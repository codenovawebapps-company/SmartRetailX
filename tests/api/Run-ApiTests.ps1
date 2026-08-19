# =============================================================================
#  SmartRetailX - Comprehensive End-to-End API Test Suite
#  Covers: User, Product, Order, Inventory, Payment, Notification Services
# =============================================================================

$ErrorActionPreference = "Continue"
$global:Pass = 0
$global:Fail = 0
$global:Results = @()
$global:AuthToken = ""

function Invoke-ApiTest {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [string]$Body = $null,
        [int[]]$ExpectedStatuses,
        [string]$ExpectBodyContains = $null,
        [string]$ExpectBodyNotContains = $null,
        [hashtable]$CustomHeaders = @{}
    )

    $headers = @{ "Content-Type" = "application/json" }
    if ($global:AuthToken -and -not $CustomHeaders.ContainsKey("Authorization")) {
        $headers["Authorization"] = "Bearer $($global:AuthToken)"
    }
    foreach ($key in $CustomHeaders.Keys) {
        $headers[$key] = $CustomHeaders[$key]
    }

    try {
        $params = @{
            Uri              = $Url
            Method           = $Method
            Headers          = $headers
            TimeoutSec       = 10
            UseBasicParsing  = $true
            ErrorAction      = "Stop"
        }
        if ($Body) { $params.Body = $Body }
        $response = Invoke-WebRequest @params
        $status   = [int]$response.StatusCode
        $content  = $response.Content
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp) {
            $status  = [int]$resp.StatusCode
            $reader  = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $content = $reader.ReadToEnd()
        } else {
            $status  = 0
            $content = $_.Exception.Message
        }
    } catch {
        $status  = 0
        $content = $_.Exception.Message
    }

    $statusPass = $ExpectedStatuses -contains $status
    $bodyContainsPass = (-not $ExpectBodyContains) -or ($content -match [regex]::Escape($ExpectBodyContains))
    $bodyNotContainsPass = (-not $ExpectBodyNotContains) -or (-not ($content -match [regex]::Escape($ExpectBodyNotContains)))
    
    $passed = $statusPass -and $bodyContainsPass -and $bodyNotContainsPass

    if ($passed) { $global:Pass++ } else { $global:Fail++ }

    $snippet = ($content -replace "`r`n"," " -replace "`n"," ")
    if ($snippet.Length -gt 120) { $snippet = $snippet.Substring(0,120) + "..." }

    $global:Results += [PSCustomObject]@{
        Passed   = $passed
        Method   = $Method
        Name     = $Name
        HTTP     = $status
        Expected = ($ExpectedStatuses -join " or ")
        Body     = $snippet
    }

    $icon  = if ($passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($passed) { "Green"  } else { "Red"   }
    Write-Host "$icon [$Method] $Name  =>  HTTP $status" -ForegroundColor $color
    if (-not $passed) {
        Write-Host "       Expected: $($ExpectedStatuses -join ' or ') | Body: $snippet" -ForegroundColor Yellow
    }

    return $content
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   SmartRetailX Comprehensive API Test Suite" -ForegroundColor Cyan
Write-Host "   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# ─── 1. HEALTH CHECKS ──────────────────────────────────────────────────────
Write-Host "`n[1] Microservice Mesh Health Checks" -ForegroundColor Magenta
Invoke-ApiTest -Name "UserService Health" -Method GET -Url "http://localhost:5001/health" -ExpectedStatuses @(200)
Invoke-ApiTest -Name "ProductService Health" -Method GET -Url "http://localhost:5002/health" -ExpectedStatuses @(200)
Invoke-ApiTest -Name "OrderService Health" -Method GET -Url "http://localhost:5003/health" -ExpectedStatuses @(200)
Invoke-ApiTest -Name "InventoryService Health" -Method GET -Url "http://localhost:5004/health" -ExpectedStatuses @(200)

# ─── 2. AUTHENTICATION & JWT ────────────────────────────────────────────────
Write-Host "`n[2] Authentication & JWT Verification Tests" -ForegroundColor Magenta

# Valid Login
$loginResp = Invoke-ApiTest -Name "JWT Login (Valid User)" -Method POST -Url "http://localhost:5001/api/v1/auth/login" `
    -Body '{ "email": "jane@example.com", "password": "secret123" }' `
    -ExpectedStatuses @(200) -ExpectBodyContains "token"

if ($loginResp -match '"token"\s*:\s*"([^"]+)"') {
    $global:AuthToken = $matches[1]
    Write-Host "       [INFO] Captured JWT Bearer Token for subsequent requests." -ForegroundColor DarkCyan
}

# Invalid Login (401)
Invoke-ApiTest -Name "JWT Login (Invalid Password)" -Method POST -Url "http://localhost:5001/api/v1/auth/login" `
    -Body '{ "email": "jane@example.com", "password": "wrongpassword" }' `
    -ExpectedStatuses @(401)

# Authenticated Profile (200 with JWT)
Invoke-ApiTest -Name "Get Current User Profile (JWT Protected)" -Method GET -Url "http://localhost:5001/api/v1/auth/me" `
    -ExpectedStatuses @(200) -ExpectBodyContains "jane@example.com"

# ─── 3. USER SERVICE CRUD ──────────────────────────────────────────────────
Write-Host "`n[3] UserService CRUD Tests" -ForegroundColor Magenta
$testEmail = "testuser_$(Get-Random)@example.com"

# Create User (POST)
$createUserResp = Invoke-ApiTest -Name "Create User (POST)" -Method POST -Url "http://localhost:5001/api/v1/users" `
    -Body "{ `"name`": `"Test Runner`", `"email`": `"$testEmail`", `"role`": `"Customer`" }" `
    -ExpectedStatuses @(201)

$newUserId = 1
if ($createUserResp -match '"id"\s*:\s*(\d+)') {
    $newUserId = [int]$matches[1]
}

# Get User by ID (GET)
Invoke-ApiTest -Name "Get User By ID (GET)" -Method GET -Url "http://localhost:5001/api/v1/users/$newUserId" `
    -ExpectedStatuses @(200) -ExpectBodyContains $testEmail

# Update User (PUT)
Invoke-ApiTest -Name "Update User (PUT)" -Method PUT -Url "http://localhost:5001/api/v1/users/$newUserId" `
    -Body "{ `"name`": `"Test Runner Updated`", `"role`": `"Customer`" }" `
    -ExpectedStatuses @(200) -ExpectBodyContains "Test Runner Updated"

# Delete User (DELETE)
Invoke-ApiTest -Name "Delete User (DELETE)" -Method DELETE -Url "http://localhost:5001/api/v1/users/$newUserId" `
    -ExpectedStatuses @(200)

# ─── 4. PRODUCT SERVICE CRUD ───────────────────────────────────────────────
Write-Host "`n[4] ProductService CRUD Tests" -ForegroundColor Magenta

# Get All Products
Invoke-ApiTest -Name "Get All Products (GET)" -Method GET -Url "http://localhost:5002/api/v1/products" `
    -ExpectedStatuses @(200)

# Create Product (POST)
$createProdResp = Invoke-ApiTest -Name "Create Product (POST)" -Method POST -Url "http://localhost:5002/api/v1/products" `
    -Body '{ "name": "Test Wireless Mouse", "description": "High precision", "price": 49.99, "category": "Accessories", "stock": 100 }' `
    -ExpectedStatuses @(201)

$newProdId = 1
if ($createProdResp -match '"id"\s*:\s*(\d+)') {
    $newProdId = [int]$matches[1]
}

# Update Product (PUT)
Invoke-ApiTest -Name "Update Product (PUT)" -Method PUT -Url "http://localhost:5002/api/v1/products/$newProdId" `
    -Body '{ "name": "Test Wireless Mouse Pro", "price": 59.99, "category": "Accessories", "stock": 90 }' `
    -ExpectedStatuses @(200) -ExpectBodyContains "59.99"

# Delete Product (DELETE)
Invoke-ApiTest -Name "Delete Product (DELETE)" -Method DELETE -Url "http://localhost:5002/api/v1/products/$newProdId" `
    -ExpectedStatuses @(200)

# ─── 5. INVENTORY SERVICE ──────────────────────────────────────────────────
Write-Host "`n[5] InventoryService Tests" -ForegroundColor Magenta

# Get Stock
Invoke-ApiTest -Name "Get Product Stock (GET)" -Method GET -Url "http://localhost:5004/api/v1/inventory/1" `
    -ExpectedStatuses @(200)

# Update Stock (PUT)
Invoke-ApiTest -Name "Update Product Stock (PUT)" -Method PUT -Url "http://localhost:5004/api/v1/inventory/1" `
    -Body '{ "stock": 150 }' `
    -ExpectedStatuses @(200) -ExpectBodyContains "150"

# Check Stock (GET)
Invoke-ApiTest -Name "Check Stock Availability (GET)" -Method GET -Url "http://localhost:5004/api/v1/inventory/check/1?quantity=5" `
    -ExpectedStatuses @(200) -ExpectBodyContains '"available":true'

# Reduce Stock (POST)
Invoke-ApiTest -Name "Reduce Stock (POST)" -Method POST -Url "http://localhost:5004/api/v1/inventory/reduce" `
    -Body '{ "productId": "1", "quantity": 2 }' `
    -ExpectedStatuses @(200) -ExpectBodyContains '"success":true'

# ─── 6. ORDER SERVICE ──────────────────────────────────────────────────────
Write-Host "`n[6] OrderService Tests" -ForegroundColor Magenta

# Create Order (POST)
$createOrderResp = Invoke-ApiTest -Name "Create Order (POST)" -Method POST -Url "http://localhost:5003/api/v1/orders" `
    -Body '{ "userId": 2, "customerName": "Jane Doe", "items": [{ "productId": 1, "productName": "Dell XPS 15", "quantity": 1, "unitPrice": 1499.99 }] }' `
    -ExpectedStatuses @(201)

$newOrderId = 1
if ($createOrderResp -match '"id"\s*:\s*(\d+)') {
    $newOrderId = [int]$matches[1]
}

# Get Order By ID (GET)
Invoke-ApiTest -Name "Get Order by ID (GET)" -Method GET -Url "http://localhost:5003/api/v1/orders/$newOrderId" `
    -ExpectedStatuses @(200)

# Get Orders By User ID (GET)
Invoke-ApiTest -Name "Get Orders by User ID (GET)" -Method GET -Url "http://localhost:5003/api/v1/orders/user/2" `
    -ExpectedStatuses @(200)

# Update Order Status (PUT)
Invoke-ApiTest -Name "Update Order Status (PUT)" -Method PUT -Url "http://localhost:5003/api/v1/orders/$newOrderId/status" `
    -Body '{ "status": "Shipped" }' `
    -ExpectedStatuses @(200) -ExpectBodyContains "Shipped"

# ─── SUMMARY ───────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "   Total: $($global:Pass + $global:Fail)  |  Passed: $global:Pass  |  Failed: $global:Fail" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

if ($global:Fail -eq 0) {
    Write-Host "`n>>> ALL API TESTS PASSED! SmartRetailX is fully compliant! <<<" -ForegroundColor Green
} else {
    Write-Host "`n>>> SOME TESTS FAILED. Please review the output above. <<<" -ForegroundColor Red
}
