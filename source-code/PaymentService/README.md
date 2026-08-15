# PaymentService

## Purpose
The **PaymentService** is responsible for processing, simulating, and documenting customer payments for orders placed on the SmartRetailX platform. It publishes events to Amazon EventBridge so that downstream services like NotificationService and InventoryService can react accordingly.

## Architecture Role
The PaymentService sits between `OrderService` and `EventBridge`.
1. Customer requests an order or payment transaction.
2. `PaymentService` processes the request, updates status (`Success` or `Failed`), and stores details in an in-memory db.
3. `PaymentService` fires `PaymentCompleted` or `PaymentFailed` event to EventBridge.

## Folder Structure
```
PaymentService/
  Controllers/
    PaymentsController.cs
  Models/
    Payment.cs
    PaymentRequest.cs
    Events/
      PaymentCompletedEvent.cs
      PaymentFailedEvent.cs
  Services/
    EventPublisher.cs
  Properties/
    launchSettings.json
  appsettings.json
  Dockerfile
  Program.cs
```

## Environment Variables
- `ASPNETCORE_ENVIRONMENT`: E.g., `Development`, `Production`
- `AWS_REGION`: The AWS region (e.g., `us-east-1`)
- `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` (development only)
- `EventBridge__EventBusName`: Name of the custom EventBridge Event Bus (default: `smartretailx-event-bus`)

## API Endpoints

### GET /health
Returns status of the microservice.
**Response (200 OK):**
```json
{
  "status": "Healthy"
}
```

### GET /api/v1/payments
Get all simulated payments.
**Response (200 OK):**
```json
[
  {
    "id": 1,
    "orderId": 1,
    "userId": 2,
    "amount": 999.99,
    "currency": "USD",
    "status": "Success",
    "paymentMethod": "Card",
    "transactionRef": "txn_abcdef123456",
    "paidAt": "2026-08-15T10:00:00Z",
    "createdAt": "2026-08-15T10:00:00Z"
  }
]
```

### POST /api/v1/payments
Simulate processing a payment.
- If the `amount` is `99.99` or the `paymentMethod` is `"FAIL"`, it triggers a failure.
- Otherwise, it succeeds.

**Request Example:**
```json
{
  "orderId": 1,
  "userId": 2,
  "amount": 999.99,
  "currency": "USD",
  "paymentMethod": "Card"
}
```
**Response Example (201 Created):**
```json
{
  "id": 1,
  "orderId": 1,
  "userId": 2,
  "amount": 999.99,
  "currency": "USD",
  "status": "Success",
  "paymentMethod": "Card",
  "transactionRef": "txn_6122ac99d45a",
  "paidAt": "2026-08-15T14:42:00Z",
  "createdAt": "2026-08-15T14:42:00Z"
}
```

## Event Schemas

### 1. PaymentCompleted (com.smartretailx.payment-service)
```json
{
  "version": "1.0",
  "paymentId": 1,
  "orderId": 1,
  "userId": 2,
  "amount": 999.99,
  "currency": "USD",
  "paymentMethod": "Card",
  "transactionRef": "txn_6122ac99d45a",
  "paidAt": "2026-08-15T14:42:00Z"
}
```

### 2. PaymentFailed (com.smartretailx.payment-service)
```json
{
  "version": "1.0",
  "paymentId": 2,
  "orderId": 2,
  "userId": 2,
  "amount": 99.99,
  "currency": "USD",
  "failureReason": "insufficient_funds",
  "failedAt": "2026-08-15T14:43:00Z"
}
```

## Local Setup & Run
Run from project directory:
```bash
dotnet run
```
Available at `http://localhost:5005` (Swagger UI at `/swagger`).

## Docker Build & Run
```bash
docker build -t smartretailx-paymentservice .
docker run -d -p 5005:8080 -e EventBridge__EventBusName=smartretailx-event-bus smartretailx-paymentservice
```

## Testing Commands
```bash
dotnet test
```

## AWS ECS Deployment Considerations
Ensure that the task execution role has policies allowing `events:PutEvents` permission for resource `arn:aws:events:*:*:event-bus/smartretailx-event-bus`.
Configure ECS Service target group routing requests to container port `8080`.
