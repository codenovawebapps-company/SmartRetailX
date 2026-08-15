Current Problems

-Limited scalability
-Single points of failure
-High deployment complexity
-Slow feature delivery
-Lack of real-time analytics
-Poor resilience during peak sales

Objective 

Design and implement a secure, scalable and highly available cloud-native distributed web application for SmartRetailX using AWS microservices and event-driven architecture.

1. Replace the monolithic architecture with microservices.
2. Containerise services using Docker.
3. Deploy services using ECS Fargate.
4. Provide REST APIs through API Gateway.
5. Use managed AWS databases.
6. Implement event-driven communication.
7. Support high availability and scalability.
8. Implement serverless processing with Lambda.
9. Provide monitoring and observability.
10. Evaluate the system through testing.


Technology Stack 
| Area               | Technology            |
| ------------------ | --------------------- |
| Language           | C#                    |
| Framework          | ASP.NET Core          |
| API                | REST                  |
| API Documentation  | Swagger/OpenAPI       |
| Containers         | Docker                |
| Container Platform | ECS Fargate           |
| Container Registry | Amazon ECR            |
| API Gateway        | Amazon API Gateway    |
| Relational DB      | Amazon RDS PostgreSQL |
| NoSQL DB           | DynamoDB              |
| File Storage       | S3                    |
| Messaging          | SQS                   |
| Event Bus          | EventBridge           |
| Serverless         | Lambda                |
| DNS                | Route 53              |
| CDN                | CloudFront            |
| Monitoring         | CloudWatch            |
| Tracing            | X-Ray                 |
| Testing            | Postman + xUnit + k6  |

Selected Microservices 
                 SmartRetailX
                      │
       ┌──────────────┼──────────────┐
       │              │              │
      User          Product         Order
    Service         Service        Service
                                      │
                         ┌────────────┼────────────┐
                         │            │            │
                      Payment      Inventory   Notification
                      Service       Service      Service


what each service does

| Service              | Responsibility                  |
| -------------------- | ------------------------------- |
| User Service         | Users, profiles, authentication |
| Product Service      | Products, categories, prices    |
| Order Service        | Orders and order status         |
| Payment Service      | Payment processing              |
| Inventory Service    | Stock management                |
| Notification Service | Customer notifications          |
