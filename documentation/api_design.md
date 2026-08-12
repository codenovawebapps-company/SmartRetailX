# SmartRetailX — API Design

> **API Version:** v1 | **Base path prefix:** `/api/v1`  
> Versioning is embedded in the URL path (`/v1/`) to allow parallel deployment of future versions without breaking existing clients.

---

## UserService  
**Base URL:** `http://localhost:5001`

### POST /api/v1/users
Create a new user account.

**Request Body**
```json
{
  "name":  "Jane Doe",
  "email": "jane@example.com",
  "role":  "Customer"
}
```

**Responses**

| Status | Description |
|--------|-------------|
| 201 Created | User created; `Location` header points to the new resource |
| 400 Bad Request | Missing or invalid fields |

**Response Body (201)**
```json
{
  "id":    1,
  "name":  "Jane Doe",
  "email": "jane@example.com",
  "role":  "Customer"
}
```

---

### GET /api/v1/users/{id}
Retrieve a user by ID.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id`  | int  | User ID |

**Responses**

| Status | Description |
|--------|-------------|
| 200 OK | User found |
| 404 Not Found | No user with that ID |

---

### POST /api/v1/auth/login
Authenticate a user and receive an access token.

**Request Body**
```json
{
  "email":    "jane@example.com",
  "password": "secret123"
}
```

**Responses**

| Status | Description |
|--------|-------------|
| 200 OK | Login successful |
| 400 Bad Request | Missing email or password |
| 401 Unauthorized | Invalid credentials |

**Response Body (200)**
```json
{
  "userId":  1,
  "email":   "jane@example.com",
  "role":    "Customer",
  "token":   "<base64-stub-token>",
  "message": "Login successful."
}
```

> **Note:** The current token is a Base64 stub for development. Replace with JWT before production.

---

## ProductService  
**Base URL:** `http://localhost:5002`

### GET /api/v1/products
List all products.

**Responses**

| Status | Description |
|--------|-------------|
| 200 OK | Array of products (may be empty) |

---

### GET /api/v1/products/{id}
Retrieve a single product by ID.

**Responses**

| Status | Description |
|--------|-------------|
| 200 OK | Product found |
| 404 Not Found | No product with that ID |

---

### POST /api/v1/products
Create a new product.

**Request Body**
```json
{
  "name":        "Wireless Mouse",
  "description": "Ergonomic wireless mouse",
  "price":       49.99,
  "category":    "Accessories"
}
```

**Responses**

| Status | Description |
|--------|-------------|
| 201 Created | Product created |
| 400 Bad Request | Invalid fields |

---

### PUT /api/v1/products/{id}
Update an existing product (full replacement).

**Request Body**
```json
{
  "name":        "Wireless Mouse Pro",
  "description": "Ergonomic wireless mouse with USB-C",
  "price":       59.99,
  "category":    "Accessories"
}
```

**Responses**

| Status | Description |
|--------|-------------|
| 200 OK | Product updated |
| 404 Not Found | No product with that ID |

---

## OrderService  
**Base URL:** `http://localhost:5003`

### POST /api/v1/orders
Place a new order.

**Request Body**
```json
{
  "userId": 1,
  "items": [
    { "productId": 1, "quantity": 2, "unitPrice": 999.99 }
  ]
}
```

> `totalAmount` is auto-calculated from items if not provided. `orderDate` defaults to UTC now. `status` defaults to `"Pending"`.

**Responses**

| Status | Description |
|--------|-------------|
| 201 Created | Order placed |
| 400 Bad Request | Invalid payload |

---

### GET /api/v1/orders/{id}
Retrieve a single order by ID.

**Responses**

| Status | Description |
|--------|-------------|
| 200 OK | Order found |
| 404 Not Found | No order with that ID |

---

### GET /api/v1/users/{id}/orders
Get all orders placed by a specific user.

**Responses**

| Status | Description |
|--------|-------------|
| 200 OK | Array of orders (may be empty) |

---

### PUT /api/v1/orders/{id}/status
Update the status of an order.

**Request Body**
```json
{
  "status": "Shipped"
}
```

**Allowed status values**

| Value | Description |
|-------|-------------|
| `Pending`    | Order placed, not yet processed |
| `Processing` | Payment confirmed, being prepared |
| `Shipped`    | Dispatched to courier |
| `Delivered`  | Received by customer |
| `Cancelled`  | Order cancelled |

**Responses**

| Status | Description |
|--------|-------------|
| 200 OK | Status updated, full order returned |
| 400 Bad Request | Invalid status value |
| 404 Not Found | No order with that ID |

---

## Summary Table

| Method | Endpoint | Service | Description |
|--------|----------|---------|-------------|
| POST | `/api/v1/users` | UserService | Create user |
| GET  | `/api/v1/users/{id}` | UserService | Get user by ID |
| POST | `/api/v1/auth/login` | UserService | Login and get token |
| GET  | `/api/v1/products` | ProductService | List all products |
| GET  | `/api/v1/products/{id}` | ProductService | Get product by ID |
| POST | `/api/v1/products` | ProductService | Create product |
| PUT  | `/api/v1/products/{id}` | ProductService | Update product |
| POST | `/api/v1/orders` | OrderService | Place order |
| GET  | `/api/v1/orders/{id}` | OrderService | Get order by ID |
| GET  | `/api/v1/users/{id}/orders` | OrderService | Get orders by user |
| PUT  | `/api/v1/orders/{id}/status` | OrderService | Update order status |