# =============================================================================
#  SmartRetailX — Master Automated Test & Report Runner
# =============================================================================

$ErrorActionPreference = "Continue"
$rootDir = "e:\smartretailX"
$global:Pass = 0
$global:Fail = 0
$global:TestResults = @()
$global:AuthToken = ""

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Starting SmartRetailX Microservices Mesh for Testing" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Terminate any previous instances
taskkill /F /IM UserService.exe /IM ProductService.exe /IM OrderService.exe /IM InventoryService.exe /IM PaymentService.exe /IM NotificationService.exe /IM ApiGateway.exe 2>$null

# 2. Launch Services
$services = @(
    @{ Name = "UserService";         Path = "$rootDir\source-code\UserService";         Port = 5001 },
    @{ Name = "ProductService";      Path = "$rootDir\source-code\ProductService";      Port = 5002 },
    @{ Name = "OrderService";        Path = "$rootDir\source-code\OrderService";        Port = 5003 },
    @{ Name = "InventoryService";    Path = "$rootDir\source-code\InventoryService";    Port = 5004 },
    @{ Name = "PaymentService";      Path = "$rootDir\source-code\PaymentService";      Port = 5005 },
    @{ Name = "NotificationService";  Path = "$rootDir\source-code\NotificationService"; Port = 5006 },
    @{ Name = "ApiGateway";          Path = "$rootDir\api-gateway";                     Port = 5000 }
)

foreach ($svc in $services) {
    Write-Host "Starting $($svc.Name) on port $($svc.Port)..." -ForegroundColor Yellow
    Start-Process -FilePath "dotnet" -ArgumentList "run --no-build --urls http://localhost:$($svc.Port)" -WorkingDirectory $svc.Path -WindowStyle Hidden
}

# 3. Wait for all services to be healthy
Write-Host "`nWaiting for all microservices to report healthy..." -ForegroundColor Yellow
foreach ($svc in $services) {
    $ready = $false
    for ($i = 0; $i -lt 15; $i++) {
        try {
            $h = Invoke-WebRequest -Uri "http://localhost:$($svc.Port)/health" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
            if ($h.StatusCode -eq 200) { $ready = $true; break }
        } catch {
            Start-Sleep -Seconds 1
        }
    }
    if ($ready) {
        Write-Host "  [OK] $($svc.Name) (Port $($svc.Port)) is ready" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] $($svc.Name) (Port $($svc.Port)) took longer to start" -ForegroundColor Yellow
    }
}

# Helper Function
function Assert-Endpoint {
    param(
        [string]$Category,
        [string]$TestName,
        [string]$Method,
        [string]$Url,
        [string]$Body = $null,
        [int[]]$ExpectedStatuses,
        [string]$ExpectContains = $null,
        [hashtable]$Headers = @{}
    )

    $reqHeaders = @{ "Content-Type" = "application/json" }
    if ($global:AuthToken -and -not $Headers.ContainsKey("Authorization")) {
        $reqHeaders["Authorization"] = "Bearer $($global:AuthToken)"
    }
    foreach ($k in $Headers.Keys) { $reqHeaders[$k] = $Headers[$k] }

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $status = 0
    $content = ""

    try {
        $params = @{
            Uri             = $Url
            Method          = $Method
            Headers         = $reqHeaders
            TimeoutSec      = 8
            UseBasicParsing = $true
            ErrorAction     = "Stop"
        }
        if ($Body) { $params.Body = $Body }
        $resp = Invoke-WebRequest @params
        $status = [int]$resp.StatusCode
        $content = $resp.Content
    } catch [System.Net.WebException] {
        $r = $_.Exception.Response
        if ($r) {
            $status = [int]$r.StatusCode
            $stream = $r.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $content = $reader.ReadToEnd()
        } else {
            $content = $_.Exception.Message
        }
    } catch {
        $content = $_.Exception.Message
    }
    $sw.Stop()

    $passedStatus = $ExpectedStatuses -contains $status
    $passedBody = (-not $ExpectContains) -or ($content -match [regex]::Escape($ExpectContains))
    $isPassed = $passedStatus -and $passedBody

    if ($isPassed) { $global:Pass++ } else { $global:Fail++ }

    $global:TestResults += [PSCustomObject]@{
        Category       = $Category
        TestName       = $TestName
        Method         = $Method
        Url            = $Url
        ExpectedStatus = ($ExpectedStatuses -join ",")
        ActualStatus   = $status
        LatencyMs      = [math]::Round($sw.Elapsed.TotalMilliseconds, 1)
        Passed         = $isPassed
        Details        = if ($content.Length -gt 80) { $content.Substring(0, 80) + "..." } else { $content }
    }

    $icon = if ($isPassed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($isPassed) { "Green" } else { "Red" }
    Write-Host "$icon [$Method] $Category - $TestName ($status in $($sw.ElapsedMilliseconds)ms)" -ForegroundColor $color

    return $content
}

Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host "  EXECUTING API TEST MATRIX" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Health Checks
Write-Host "`n[1] Health & Discovery" -ForegroundColor Magenta
Assert-Endpoint -Category "Health" -TestName "ApiGateway Health" -Method GET -Url "http://localhost:5000/health" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Health" -TestName "UserService Health" -Method GET -Url "http://localhost:5001/health" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Health" -TestName "ProductService Health" -Method GET -Url "http://localhost:5002/health" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Health" -TestName "OrderService Health" -Method GET -Url "http://localhost:5003/health" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Health" -TestName "InventoryService Health" -Method GET -Url "http://localhost:5004/health" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Health" -TestName "PaymentService Health" -Method GET -Url "http://localhost:5005/health" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Health" -TestName "NotificationService Health" -Method GET -Url "http://localhost:5006/health" -ExpectedStatuses @(200)

# 2. Authentication & JWT
Write-Host "`n[2] Authentication & Security (JWT / RBAC)" -ForegroundColor Magenta
$loginRes = Assert-Endpoint -Category "Auth" -TestName "Customer Login (Valid JWT)" -Method POST -Url "http://localhost:5000/api/v1/auth/login" `
    -Body '{ "email": "jane@example.com", "password": "secret123" }' -ExpectedStatuses @(200) -ExpectContains "token"

if ($loginRes -match '"token"\s*:\s*"([^"]+)"') {
    $global:AuthToken = $matches[1]
}

Assert-Endpoint -Category "Auth" -TestName "Invalid Login (401 Unauthorized)" -Method POST -Url "http://localhost:5000/api/v1/auth/login" `
    -Body '{ "email": "jane@example.com", "password": "badpassword" }' -ExpectedStatuses @(401)

Assert-Endpoint -Category "Auth" -TestName "Register New User (POST)" -Method POST -Url "http://localhost:5000/api/v1/auth/register" `
    -Body "{ `"name`": `"Auto Test`", `"email`": `"autouser_$(Get-Random)@example.com`", `"password`": `"test1234`", `"role`": `"Customer`" }" `
    -ExpectedStatuses @(201)

Assert-Endpoint -Category "Auth" -TestName "Get Current Profile (JWT Protected /me)" -Method GET -Url "http://localhost:5000/api/v1/auth/me" `
    -ExpectedStatuses @(200) -ExpectContains "jane@example.com"

# 3. User Service CRUD
Write-Host "`n[3] UserService CRUD" -ForegroundColor Magenta
Assert-Endpoint -Category "User" -TestName "List All Users (GET)" -Method GET -Url "http://localhost:5000/api/v1/users" -ExpectedStatuses @(200)
Assert-Endpoint -Category "User" -TestName "Get User By ID (GET)" -Method GET -Url "http://localhost:5000/api/v1/users/2" -ExpectedStatuses @(200) -ExpectContains "jane@example.com"
Assert-Endpoint -Category "User" -TestName "Update User (PUT)" -Method PUT -Url "http://localhost:5000/api/v1/users/2" `
    -Body '{ "name": "Jane Doe Updated", "role": "Customer" }' -ExpectedStatuses @(200) -ExpectContains "Jane Doe Updated"

# 4. Product Service CRUD
Write-Host "`n[4] ProductService CRUD" -ForegroundColor Magenta
Assert-Endpoint -Category "Product" -TestName "List Products (GET)" -Method GET -Url "http://localhost:5000/api/v1/products" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Product" -TestName "Get Product by ID (GET)" -Method GET -Url "http://localhost:5000/api/v1/products/1" -ExpectedStatuses @(200) -ExpectContains "Laptop"

$newProdRes = Assert-Endpoint -Category "Product" -TestName "Create Product (POST)" -Method POST -Url "http://localhost:5000/api/v1/products" `
    -Body '{ "name": "4K Ultra Webcam", "description": "Crisp 4K HDR", "price": 129.99, "category": "Accessories", "stock": 40 }' `
    -ExpectedStatuses @(201)

$newProdId = 1
if ($newProdRes -match '"id"\s*:\s*(\d+)') { $newProdId = [int]$matches[1] }

Assert-Endpoint -Category "Product" -TestName "Update Product (PUT)" -Method PUT -Url "http://localhost:5000/api/v1/products/$newProdId" `
    -Body '{ "name": "4K Ultra Webcam Pro", "price": 139.99, "category": "Accessories", "stock": 35 }' -ExpectedStatuses @(200)

Assert-Endpoint -Category "Product" -TestName "Delete Product (DELETE)" -Method DELETE -Url "http://localhost:5000/api/v1/products/$newProdId" -ExpectedStatuses @(200)

# 5. Inventory Service
Write-Host "`n[5] InventoryService Control" -ForegroundColor Magenta
Assert-Endpoint -Category "Inventory" -TestName "Get Stock Level (GET)" -Method GET -Url "http://localhost:5000/api/v1/inventory/1" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Inventory" -TestName "Update Stock Directly (PUT)" -Method PUT -Url "http://localhost:5000/api/v1/inventory/1" `
    -Body '{ "stock": 120 }' -ExpectedStatuses @(200) -ExpectContains "120"
Assert-Endpoint -Category "Inventory" -TestName "Check Stock Availability (GET)" -Method GET -Url "http://localhost:5000/api/v1/inventory/check/1?quantity=2" -ExpectedStatuses @(200) -ExpectContains '"available":true'
Assert-Endpoint -Category "Inventory" -TestName "Reduce Stock (POST)" -Method POST -Url "http://localhost:5000/api/v1/inventory/reduce" `
    -Body '{ "productId": "1", "quantity": 1 }' -ExpectedStatuses @(200)

# 6. Order Service
Write-Host "`n[6] OrderService & Event Pipeline" -ForegroundColor Magenta
$createOrderRes = Assert-Endpoint -Category "Order" -TestName "Place New Order (POST)" -Method POST -Url "http://localhost:5000/api/v1/orders" `
    -Body '{ "userId": 2, "customerName": "Jane Doe", "items": [{ "productId": 1, "productName": "Dell XPS 15 Laptop", "quantity": 1, "unitPrice": 1499.99 }] }' `
    -ExpectedStatuses @(201)

$newOrderId = 1
if ($createOrderRes -match '"id"\s*:\s*(\d+)') { $newOrderId = [int]$matches[1] }

Assert-Endpoint -Category "Order" -TestName "Get Order by ID (GET)" -Method GET -Url "http://localhost:5000/api/v1/orders/$newOrderId" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Order" -TestName "Get Orders by User ID (GET)" -Method GET -Url "http://localhost:5000/api/v1/orders/user/2" -ExpectedStatuses @(200)
Assert-Endpoint -Category "Order" -TestName "Update Order Status (PUT)" -Method PUT -Url "http://localhost:5000/api/v1/orders/$newOrderId/status" `
    -Body '{ "status": "Shipped" }' -ExpectedStatuses @(200) -ExpectContains "Shipped"

# 7. Payment & Notification Services
Write-Host "`n[7] Payment & Notification Services" -ForegroundColor Magenta
Assert-Endpoint -Category "Payment" -TestName "Process Payment (POST)" -Method POST -Url "http://localhost:5000/api/v1/payments" `
    -Body '{ "orderId": 1, "userId": 2, "amount": 1499.99, "currency": "USD", "paymentMethod": "Card" }' -ExpectedStatuses @(201)

Assert-Endpoint -Category "Notification" -TestName "Send Order Notification (POST)" -Method POST -Url "http://localhost:5000/api/v1/notifications" `
    -Body '{ "eventType": "OrderShipped", "userId": "2", "orderId": "1", "message": "Your order #1 has shipped!" }' -ExpectedStatuses @(200, 201)

# Cleanup Background Processes
Write-Host "`nCleaning up microservice background processes..." -ForegroundColor Yellow
taskkill /F /IM UserService.exe /IM ProductService.exe /IM OrderService.exe /IM InventoryService.exe /IM PaymentService.exe /IM NotificationService.exe /IM ApiGateway.exe 2>$null

# Print Summary Table
Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host "                TEST EXECUTION SUMMARY" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
$global:TestResults | Format-Table Category, Method, TestName, ActualStatus, LatencyMs, Passed -AutoSize

Write-Host "TOTAL: $($global:Pass + $global:Fail) | PASSED: $global:Pass | FAILED: $global:Fail" -ForegroundColor $(if ($global:Fail -eq 0) { "Green" } else { "Red" })

# Export JSON report
$reportPath = "$rootDir\tests\api\test_report.json"
$global:TestResults | ConvertTo-Json -Depth 4 | Set-Content -Path $reportPath
Write-Host "Detailed report exported to: $reportPath" -ForegroundColor Cyan
