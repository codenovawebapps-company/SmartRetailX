# SmartRetailX — Database Design

## Architecture Overview

```
                        DATA
                          |
          +---------------+---------------+
          |               |               |
          ▼               ▼               ▼
    RDS PostgreSQL     DynamoDB           S3
          |               |               |
       Users           Products     ProductImages
       Orders          Inventory       Invoices
       OrderItems                      Reports
       Payments
```

### Why this split?

| Store | Reason |
|---|---|
| **RDS PostgreSQL** | Relational, transactional data that needs ACID guarantees (users, orders, payments) |
| **DynamoDB** | High-throughput, schema-flexible catalogue and inventory data with single-digit ms reads |
| **S3** | Binary / large-object storage (images, PDFs) that must be served cheaply at scale |

---

## RDS PostgreSQL

**Instance type (recommended):** `db.t3.medium`  
**Multi-AZ:** Yes (production)  
**Schema file:** [`database/schema.sql`](schema.sql)

### Table: `users`

> Owned by **UserService**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `id` | `SERIAL` | PK | Auto-increment |
| `name` | `VARCHAR(150)` | NOT NULL | Full name |
| `email` | `VARCHAR(255)` | NOT NULL, UNIQUE | Login identifier |
| `password_hash` | `VARCHAR(255)` | NOT NULL | bcrypt hash |
| `role` | `VARCHAR(50)` | DEFAULT `'Customer'` | `Customer` \| `Admin` \| `Vendor` |
| `created_at` | `TIMESTAMPTZ` | DEFAULT NOW() | — |
| `updated_at` | `TIMESTAMPTZ` | DEFAULT NOW() | Auto-updated via trigger |

**Indexes:** `idx_users_email`

---

### Table: `orders`

> Owned by **OrderService**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `id` | `SERIAL` | PK | Auto-increment |
| `user_id` | `INT` | FK → `users.id` CASCADE | — |
| `status` | `VARCHAR(50)` | DEFAULT `'Pending'` | `Pending` \| `Processing` \| `Shipped` \| `Delivered` \| `Cancelled` |
| `total_amount` | `NUMERIC(12,2)` | DEFAULT 0.00 | Calculated from items |
| `order_date` | `TIMESTAMPTZ` | DEFAULT NOW() | — |
| `updated_at` | `TIMESTAMPTZ` | DEFAULT NOW() | Auto-updated via trigger |

**Indexes:** `idx_orders_user_id`, `idx_orders_status`

---

### Table: `order_items`

> Owned by **OrderService** (child of `orders`)

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `id` | `SERIAL` | PK | Auto-increment |
| `order_id` | `INT` | FK → `orders.id` CASCADE | — |
| `product_id` | `VARCHAR(100)` | NOT NULL | References DynamoDB `Products.productId` (logical, no DB FK) |
| `product_name` | `VARCHAR(255)` | NOT NULL | Snapshot at order time |
| `quantity` | `INT` | CHECK > 0 | — |
| `unit_price` | `NUMERIC(12,2)` | CHECK ≥ 0 | Snapshot at order time |

**Indexes:** `idx_order_items_order_id`, `idx_order_items_product_id`

> **Design note:** `product_name` and `unit_price` are denormalized snapshots. This ensures historical order accuracy even if the product is later updated or deleted in DynamoDB.

---

### Table: `payments`

> Owned by **PaymentService**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `id` | `SERIAL` | PK | Auto-increment |
| `order_id` | `INT` | FK → `orders.id` | One payment per order |
| `user_id` | `INT` | FK → `users.id` | Redundant for fast lookup |
| `amount` | `NUMERIC(12,2)` | NOT NULL | — |
| `currency` | `VARCHAR(10)` | DEFAULT `'USD'` | ISO 4217 |
| `status` | `VARCHAR(50)` | DEFAULT `'Pending'` | `Pending` \| `Success` \| `Failed` \| `Refunded` |
| `payment_method` | `VARCHAR(50)` | DEFAULT `'Card'` | `Card` \| `UPI` \| `Wallet` \| `COD` |
| `transaction_ref` | `VARCHAR(255)` | NULLABLE | Payment gateway reference |
| `paid_at` | `TIMESTAMPTZ` | NULLABLE | Set when status → Success |
| `created_at` | `TIMESTAMPTZ` | DEFAULT NOW() | — |

**Indexes:** `idx_payments_order_id`, `idx_payments_status`

---

### Entity-Relationship Diagram

```
users
 ├── id (PK)
 ├── name
 ├── email (UNIQUE)
 ├── password_hash
 └── role
       │
       │ 1 ──────────────────── N
       ▼
    orders
     ├── id (PK)
     ├── user_id (FK → users)
     ├── status
     ├── total_amount
     └── order_date
           │                         │
           │ 1 ─────── N             │ 1 ──── 1
           ▼                         ▼
       order_items               payments
        ├── id (PK)               ├── id (PK)
        ├── order_id (FK)         ├── order_id (FK)
        ├── product_id            ├── user_id (FK)
        ├── product_name          ├── amount
        ├── quantity              ├── status
        └── unit_price            └── transaction_ref
```

---

## DynamoDB

**Capacity mode:** On-demand (PAY_PER_REQUEST)  
**Table definitions:** [`database/dynamodb-tables.json`](dynamodb-tables.json)

### Table: `Products`

> Owned by **ProductService**

| Attribute | Type | Key | Notes |
|-----------|------|-----|-------|
| `productId` | String | **PK (HASH)** | UUID e.g. `prod-001` |
| `name` | String | — | Product title |
| `description` | String | — | Full description |
| `price` | Number | — | Decimal, e.g. `999.99` |
| `category` | String | GSI HASH | For category browsing |
| `imageKey` | String | — | S3 key e.g. `product-images/prod-001/main.jpg` |
| `createdAt` | String | — | ISO 8601 UTC |
| `updatedAt` | String | — | ISO 8601 UTC |

**Global Secondary Index:** `category-index` → enables `GET /api/v1/products?category=Electronics`

---

### Table: `Inventory`

> Owned by **InventoryService**

| Attribute | Type | Key | Notes |
|-----------|------|-----|-------|
| `productId` | String | **PK (HASH)** | Matches `Products.productId` |
| `warehouseId` | String | **SK (RANGE)** | e.g. `wh-us-east-1` |
| `quantityOnHand` | Number | — | Total physical stock |
| `quantityReserved` | Number | — | Allocated to open orders |
| `reorderLevel` | Number | — | Triggers restocking alert |
| `lastRestockedAt` | String | — | ISO 8601 UTC |
| `updatedAt` | String | — | ISO 8601 UTC |

**Available stock** = `quantityOnHand - quantityReserved`

---

## S3

**Bucket naming convention:** `smartretailx-{env}-{purpose}`  
e.g. `smartretailx-prod-assets`, `smartretailx-prod-documents`

### Bucket: `smartretailx-{env}-assets`

> Product images served via CloudFront CDN

| Prefix | Content | Access |
|--------|---------|--------|
| `product-images/{productId}/` | Product photos (WebP, JPEG) | Public via CloudFront |
| `product-images/{productId}/main.jpg` | Primary listing image | Public |
| `product-images/{productId}/gallery/` | Additional gallery images | Public |

---

### Bucket: `smartretailx-{env}-documents`

> Private documents — accessed only via pre-signed URLs

| Prefix | Content | Access |
|--------|---------|--------|
| `invoices/{year}/{month}/{orderId}.pdf` | Customer invoices | Private, pre-signed URL |
| `reports/{year}/{month}/sales-report.csv` | Monthly sales exports | Private, admin only |
| `reports/{year}/{month}/inventory-report.csv` | Stock level exports | Private, admin only |

---

## Cross-Store Data Flow

```
POST /api/v1/orders
        │
        ├──► Check DynamoDB Inventory  (reserve stock)
        ├──► Write to RDS orders        (create order record)
        ├──► Write to RDS order_items   (snapshot product name + price)
        └──► Trigger PaymentService
                  │
                  └──► Write to RDS payments
                  └──► On success: generate invoice → upload to S3
```

---

## Summary

| Store | Tables / Buckets | Owned By |
|---|---|---|
| **RDS PostgreSQL** | `users`, `orders`, `order_items`, `payments` | UserService, OrderService, PaymentService |
| **DynamoDB** | `Products`, `Inventory` | ProductService, InventoryService |
| **S3** | `assets` (images), `documents` (invoices, reports) | ProductService, OrderService, Admin |
