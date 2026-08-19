# SmartRetailX — Docker Containerisation

This directory contains container definitions and orchestration configurations for all SmartRetailX microservices.

---

## 📁 Directory Structure

```
docker/
├── .dockerignore
├── docker-compose.yml              # Multi-container orchestration
├── user-service.Dockerfile         # UserService container definition
├── product-service.Dockerfile      # ProductService container definition
├── order-service.Dockerfile        # OrderService container definition
├── inventory-service.Dockerfile    # InventoryService container definition
├── payment-service.Dockerfile      # PaymentService container definition
├── notification-service.Dockerfile # NotificationService container definition
├── api-gateway.Dockerfile          # ApiGateway container definition
├── build-all.ps1                   # Script to build all container images
└── README.md                       # Documentation
```

---

## 🚀 Running Containers

### Option 1: Using Docker Compose (Recommended)
From the `docker/` directory:
```bash
docker compose up --build
```
Or to run in detached (background) mode:
```bash
docker compose up -d
```

To stop all containers:
```bash
docker compose down
```

---

### Option 2: Building and Running Individual Images

#### 1. Build all images:
```powershell
pwsh docker/build-all.ps1
```

#### 2. Run an individual container:
```bash
# Run User Service on port 5001
docker run -d -p 5001:8080 --name smartretailx-user smartretailx-user:latest

# Run Product Service on port 5002
docker run -d -p 5002:8080 --name smartretailx-product smartretailx-product:latest

# Run Order Service on port 5003
docker run -d -p 5003:8080 --name smartretailx-order smartretailx-order:latest

# Run Inventory Service on port 5004
docker run -d -p 5004:8080 --name smartretailx-inventory smartretailx-inventory:latest

# Run Payment Service on port 5005
docker run -d -p 5005:8080 --name smartretailx-payment smartretailx-payment:latest

# Run Notification Service on port 5006
docker run -d -p 5006:8080 --name smartretailx-notification smartretailx-notification:latest

# Run API Gateway on port 5000
docker run -d -p 5000:8080 --name smartretailx-gateway smartretailx-gateway:latest
```

---

## 🌐 Container Port Mapping

| Container | Image Tag | Host Port | Container Port |
| :--- | :--- | :---: | :---: |
| **API Gateway** | `smartretailx-gateway:latest` | `5000` | `8080` |
| **User Service** | `smartretailx-user:latest` | `5001` | `8080` |
| **Product Service** | `smartretailx-product:latest` | `5002` | `8080` |
| **Order Service** | `smartretailx-order:latest` | `5003` | `8080` |
| **Inventory Service** | `smartretailx-inventory:latest` | `5004` | `8080` |
| **Payment Service** | `smartretailx-payment:latest` | `5005` | `8080` |
| **Notification Service** | `smartretailx-notification:latest` | `5006` | `8080` |
