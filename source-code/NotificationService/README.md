# NotificationService

## Purpose
The **NotificationService** is responsible for consuming asynchronous events from Amazon SQS, generating user notifications (mocked email/SMS), storing history in-memory, and exposing REST APIs to query notification logs.

## Architecture Role
The NotificationService acts as a consumer of EventBridge/SQS events:
1. EventBridge routes events (`OrderCreated`, `PaymentCompleted`, `PaymentFailed`, `OrderStatusChanged`) to the SQS queue (`smartretailx-notification-queue`).
2. `NotificationService` runs a background hosted worker `SqsEventConsumer` to long-poll the SQS queue.
3. It validates events, enforces idempotency, formats messages, mock-sends notifications, logs operations, and deletes messages from the queue.

## Folder Structure
```
NotificationService/
  Controllers/
    NotificationsController.cs
  Models/
    Notification.cs
    NotificationRequest.cs
    Events/
      Events.cs   (SqsEnvelope, OrderCreatedEvent, etc.)
  Services/
    NotificationStore.cs
    MockNotificationSender.cs
    SqsEventConsumer.cs
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
- `SQS_QUEUE_URL` or `AWS__QueueUrl`: URL of the SQS queue `smartretailx-notification-queue`

## API Endpoints

### GET /health
Returns status of the microservice.
**Response (200 OK):**
```json
{
  "status": "Healthy"
}
```

### GET /api/v1/notifications
Get all simulated notifications.
**Response (200 OK):**
```json
[
  {
    "id": 1,
    "userId": 2,
    "orderId": 42,
    "type": "ORDER_CREATED",
    "message": "Order #42 has been created successfully. Total: $999.99 USD.",
    "status": "Sent",
    "createdAt": "2026-08-15T14:42:00Z"
  }
]
```

### POST /api/v1/notifications
Manually trigger/create a notification (mocked execution).

**Request Example:**
```json
{
  "userId": 2,
  "orderId": 42,
  "type": "ORDER_CREATED",
  "message": "Order #42 created successfully."
}
```
**Response Example (201 Created):**
```json
{
  "id": 2,
  "userId": 2,
  "orderId": 42,
  "type": "ORDER_CREATED",
  "message": "Order #42 created successfully.",
  "status": "Sent",
  "createdAt": "2026-08-15T14:45:00Z"
}
```

## Event Consumer Handling
The service listens to the following events routed via EventBridge:
- **`OrderCreated`**: Triggers notification `ORDER_CREATED`.
- **`PaymentCompleted`**: Triggers notification `PAYMENT_SUCCESS`.
- **`PaymentFailed`**: Triggers notification `PAYMENT_FAILED`.
- **`OrderStatusChanged`**:
  - `statusTo` = `"Processing"` -> notification `ORDER_CONFIRMED`.
  - `statusTo` = `"Cancelled"` -> notification `ORDER_CANCELLED`.
  - default -> notification `ORDER_STATUS_CHANGED`.

## Local Setup & Run
Run from project directory:
```bash
dotnet run
```
Available at `http://localhost:5004` (Swagger UI at `/swagger`).

## Docker Build & Run
```bash
docker build -t smartretailx-notificationservice .
docker run -d -p 5004:8080 -e SQS_QUEUE_URL=https://sqs.us-east-1.amazonaws.com/123456789012/smartretailx-notification-queue smartretailx-notificationservice
```

## Testing Commands
```bash
dotnet test
```

## AWS ECS Deployment Considerations
Ensure that the task execution role and task role have policies allowing `sqs:ReceiveMessage`, `sqs:DeleteMessage`, and `sqs:GetQueueAttributes` permissions for the SQS queue resource.
Configure ECS Service target group routing requests to container port `8080`.
Configure the SQS queue visibility timeout to be at least `30 seconds` (visibility timeout should be greater than or equal to SQS polling timeouts).
