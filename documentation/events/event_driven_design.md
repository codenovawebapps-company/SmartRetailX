# SmartRetailX — Event-Driven Communication Design

## Overview

SmartRetailX uses **Amazon EventBridge** as the central event bus for asynchronous inter-service communication, with **Amazon SQS** queues acting as durable buffers between EventBridge and each consumer service.

This design satisfies:
- ✅ **Asynchronous processing** — services don't block waiting for downstream responses
- ✅ **Event publishing / subscription** — EventBridge fan-out to multiple consumers per event
- ✅ **Data consistency** — SQS + DLQ guarantee at-least-once delivery with retry

---

## Architecture

```
                    ┌─────────────────────────────────────────────┐
                    │           EventBridge                        │
                    │       (smartretailx-event-bus)               │
                    └──────────────────┬──────────────────────────┘
                                       │
              ┌─────────┬─────────────┬┴─────────────┬─────────────┐
              │         │             │               │             │
              ▼         ▼             ▼               ▼             ▼
         payment-   inventory-   notification-   order-update-  (filtered)
          queue       queue         queue           queue
              │         │             │               │
              ▼         ▼             ▼               ▼
         Payment    Inventory   Notification      Order
         Service    Service      Service         Service
```

### Why EventBridge + SQS?

| Concern | Solution |
|---|---|
| Fan-out (1 event → N consumers) | EventBridge rule → multiple SQS targets |
| Durability | SQS stores messages for 24 h if consumer is down |
| Retry on failure | SQS visibility timeout + `MaxReceiveCount = 3` |
| Poison-message isolation | Dead Letter Queue (DLQ) per queue |
| Filtering | EventBridge content-based routing (e.g. `belowReorderLevel: true`) |

---

## Events

### 1. `OrderCreated`
**Source:** `com.smartretailx.order-service`  
**Producer:** OrderService  
**Consumers:** PaymentService, InventoryService, NotificationService

**Trigger:** Immediately after a new order row is committed to RDS.

**Payload fields:**

| Field | Type | Description |
|---|---|---|
| `version` | string | Schema version (`"1.0"`) |
| `orderId` | int | RDS `orders.id` |
| `userId` | int | Placing customer |
| `totalAmount` | decimal | Sum of all items |
| `currency` | string | ISO 4217 (`"USD"`) |
| `status` | string | Always `"Pending"` |
| `orderDate` | string | ISO 8601 UTC |
| `items[]` | array | `productId`, `productName`, `quantity`, `unitPrice` |

**Consumer reactions:**
- **PaymentService** → initiates charge against saved payment method
- **InventoryService** → reserves stock for each line item in DynamoDB
- **NotificationService** → sends "Order Received" email/SMS to customer

---

### 2. `PaymentCompleted`
**Source:** `com.smartretailx.payment-service`  
**Producer:** PaymentService  
**Consumers:** OrderService, NotificationService

**Trigger:** Gateway confirms successful charge; `payments.status` set to `Success`.

**Payload fields:**

| Field | Type | Description |
|---|---|---|
| `version` | string | Schema version |
| `paymentId` | int | RDS `payments.id` |
| `orderId` | int | Associated order |
| `userId` | int | Paying customer |
| `amount` | decimal | Charged amount |
| `currency` | string | ISO 4217 |
| `paymentMethod` | string | `Card` \| `UPI` \| `Wallet` \| `COD` |
| `transactionRef` | string | Payment gateway reference |
| `paidAt` | string | ISO 8601 UTC |

**Consumer reactions:**
- **OrderService** → transitions order status `Pending → Processing`
- **NotificationService** → sends "Payment Confirmed" receipt to customer

---

### 3. `PaymentFailed`
**Source:** `com.smartretailx.payment-service`  
**Producer:** PaymentService  
**Consumers:** OrderService, InventoryService, NotificationService

**Trigger:** Gateway declines charge or times out; `payments.status` set to `Failed`.

**Payload fields:**

| Field | Type | Description |
|---|---|---|
| `version` | string | Schema version |
| `paymentId` | int | RDS `payments.id` |
| `orderId` | int | Associated order |
| `userId` | int | Customer |
| `amount` | decimal | Attempted amount |
| `currency` | string | ISO 4217 |
| `failureReason` | string | `insufficient_funds` \| `card_expired` \| `gateway_timeout` |
| `failedAt` | string | ISO 8601 UTC |

**Consumer reactions:**
- **OrderService** → transitions order status `Pending → Cancelled`
- **InventoryService** → releases the reserved stock back to available
- **NotificationService** → sends "Payment Failed" alert to customer

---

### 4. `InventoryUpdated`
**Source:** `com.smartretailx.inventory-service`  
**Producer:** InventoryService  
**Consumers:** ProductService, NotificationService *(filtered: `belowReorderLevel = true`)*

**Trigger:** Any stock level mutation — reserve, release, restock, or manual adjustment.

**Payload fields:**

| Field | Type | Description |
|---|---|---|
| `version` | string | Schema version |
| `productId` | string | DynamoDB `Products.productId` |
| `warehouseId` | string | DynamoDB `Inventory.warehouseId` |
| `changeType` | string | `Reserved` \| `Released` \| `Restocked` \| `Adjusted` |
| `quantityBefore` | int | Stock before change |
| `quantityAfter` | int | Stock after change |
| `quantityReserved` | int | Current reserved units |
| `belowReorderLevel` | bool | `true` triggers reorder alert |
| `updatedAt` | string | ISO 8601 UTC |

**Consumer reactions:**
- **ProductService** → optionally marks product as low stock / out of stock
- **NotificationService** *(only when `belowReorderLevel = true`)* → alerts admin to reorder

---

### 5. `OrderStatusChanged`
**Source:** `com.smartretailx.order-service`  
**Producer:** OrderService  
**Consumers:** NotificationService, InventoryService

**Trigger:** Any order status transition (`Pending → Processing`, `Processing → Shipped`, etc.).

**Payload fields:**

| Field | Type | Description |
|---|---|---|
| `version` | string | Schema version |
| `orderId` | int | RDS `orders.id` |
| `userId` | int | Customer |
| `statusFrom` | string | Previous status |
| `statusTo` | string | New status |
| `reason` | string | Optional — e.g. `PaymentCompleted`, `ManualUpdate` |
| `changedAt` | string | ISO 8601 UTC |

**Consumer reactions:**
- **NotificationService** → sends status update push/email to customer
- **InventoryService** → on `Delivered`, permanently deducts reserved stock

---

## Full Event Flow

### Happy Path — Successful Order

```
Customer places order
        │
        ▼
  OrderService ──[OrderCreated]──────────────────────────────────────────┐
        │                                                                 │
        │              ┌──────────────────┐   ┌──────────────────┐      │
        │              │  PaymentService  │   │ InventoryService │      │
        │              │ (charges card)   │   │ (reserves stock) │      │
        │              └────────┬─────────┘   └────────┬─────────┘      │
        │                       │                       │                │
        │              [PaymentCompleted]    [InventoryUpdated]          │
        │                       │                                        │
        │              ┌────────▼─────────┐                             │
        │              │   OrderService   │                             │
        │              │ Pending→Processing│                            │
        │              └────────┬─────────┘                            │
        │                       │                                       │
        │              [OrderStatusChanged]                             │
        │                       │                                       │
        └──────────────────────►└──────────────────────────────────────►│
                                                            NotificationService
                                                         (3 notifications sent)
```

### Sad Path — Payment Failed

```
Customer places order
        │
  OrderService ──[OrderCreated]──► PaymentService (charge declined)
                                          │
                                   [PaymentFailed]
                                          │
                     ┌────────────────────┼────────────────────┐
                     ▼                    ▼                    ▼
               OrderService        InventoryService    NotificationService
            (Pending→Cancelled)   (release reserved)   ("Payment Failed"
                     │                                    alert to user)
                     │
              [OrderStatusChanged]
                     │
                     ▼
            NotificationService
          ("Order Cancelled" email)
```

---

## SQS Queue Configuration

| Queue | Consumer | Visibility Timeout | DLQ | Max Retries |
|---|---|---|---|---|
| `smartretailx-payment-queue` | PaymentService | 60 s | ✅ | 3 |
| `smartretailx-inventory-queue` | InventoryService | 30 s | ✅ | 3 |
| `smartretailx-notification-queue` | NotificationService | 30 s | ✅ | 3 |
| `smartretailx-order-update-queue` | OrderService | 60 s | ✅ | 3 |

> **Dead Letter Queues:** Failed messages (exceeding `MaxReceiveCount`) land in the DLQ for manual inspection via CloudWatch or re-processing via Lambda.

---

## EventBridge Routing Rules Summary

| Rule | Event | Targets |
|---|---|---|
| `route-order-created-to-payment` | `OrderCreated` | payment-queue |
| `route-order-created-to-inventory` | `OrderCreated` | inventory-queue |
| `route-order-created-to-notification` | `OrderCreated` | notification-queue |
| `route-payment-completed-to-order` | `PaymentCompleted` | order-update-queue, notification-queue |
| `route-payment-failed-to-order-inventory-notification` | `PaymentFailed` | order-update-queue, inventory-queue, notification-queue |
| `route-inventory-updated-to-notification` | `InventoryUpdated` *(filtered: belowReorderLevel=true)* | notification-queue |
| `route-order-status-changed-to-notification` | `OrderStatusChanged` | notification-queue |

---

## Data Consistency Mechanisms

| Mechanism | Where Used | Purpose |
|---|---|---|
| **At-least-once delivery** | SQS | Guarantees no event is lost |
| **Idempotency keys** | All consumers | Prevent duplicate processing (use `orderId` / `paymentId` as idempotency key) |
| **Outbox pattern** (future) | OrderService, PaymentService | Atomically persist DB row + event in same transaction |
| **DLQ + CloudWatch alarm** | All queues | Alert on-call when messages fail repeatedly |
| **Inventory snapshot in order_items** | RDS | Price/name accuracy even if product changes after order |
| **Status transition validation** | OrderService | Reject illegal status jumps (e.g. `Pending → Delivered`) |

---

## Supporting Files

| File | Description |
|---|---|
| [`events/event-schemas.json`](event-schemas.json) | Full schema + sample payload per event |
| [`events/eventbridge-rules.json`](eventbridge-rules.json) | EventBridge rules + SQS ARN references |
| [`database/database_design.md`](../database/database_design.md) | RDS / DynamoDB / S3 schema |
| [`api_design.md`](../api_design.md) | REST API endpoints |
