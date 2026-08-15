# =============================================================================
#  SmartRetailX - Comprehensive API Test Suite
#  Tests: PaymentService (5005), NotificationService (5004)
# =============================================================================

$ErrorActionPreference = "Continue"
$global:Pass = 0
$global:Fail = 0
$global:Results = @()

function Invoke-ApiTest {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [string]$Body = $null,
        [int[]]$ExpectedStatuses,
        [string]$ExpectBodyContains = $null,
        [string]$ExpectBodyNotContains = $null
    )

    $headers = @{ "Content-Type" = "application/json" }

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
Write-Host "   SmartRetailX API Test Suite: Payments & Notifications" -ForegroundColor Cyan
Write-Host "   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# ─── 1. HEALTH CHECKS ──────────────────────────────────────────────────────
Write-Host "`n[1] Health Endpoint Tests" -ForegroundColor Magenta
Invoke-ApiTest -Name "PaymentService Health Check" -Method GET -Url "http://localhost:5005/health" -ExpectedStatuses @(200)
Invoke-ApiTest -Name "NotificationService Health Check" -Method GET -Url "http://localhost:5004/health" -ExpectedStatuses @(200)

# ─── 2. PAYMENT SERVICE VALID & INVALID REQUESTS ────────────────────────────
Write-Host "`n[2] PaymentService Validation & Error Handling Tests" -ForegroundColor Magenta

# Valid Success Request
Invoke-ApiTest -Name "Create Payment (Valid SUCCESS)" -Method POST -Url "http://localhost:5005/api/v1/payments" `
    -Body '{ "orderId": 101, "userId": 202, "amount": 150.00, "currency": "USD", "paymentMethod": "Card" }' `
    -ExpectedStatuses @(201) -ExpectBodyContains "Success"

# Valid Failure Request
Invoke-ApiTest -Name "Create Payment (Valid FAILURE - Insufficient Funds)" -Method POST -Url "http://localhost:5005/api/v1/payments" `
    -Body '{ "orderId": 102, "userId": 202, "amount": 99.99, "currency": "USD", "paymentMethod": "Card" }' `
    -ExpectedStatuses @(201) -ExpectBodyContains "Failed"

# Invalid requests & missing fields
Invoke-ApiTest -Name "Create Payment (Invalid Request - Zero Amount)" -Method POST -Url "http://localhost:5005/api/v1/payments" `
    -Body '{ "orderId": 103, "userId": 202, "amount": 0, "currency": "USD" }' `
    -ExpectedStatuses @(400) -ExpectBodyContains "amount must be greater than zero"

# Missing required fields
Invoke-ApiTest -Name "Create Payment (Missing Fields - Missing OrderId)" -Method POST -Url "http://localhost:5005/api/v1/payments" `
    -Body '{ "userId": 202, "amount": 10.00, "currency": "USD" }' `
    -ExpectedStatuses @(400) -ExpectBodyContains "OrderId and UserId are required"

# Get by ID non-existent resource
Invoke-ApiTest -Name "Get Payment by ID (Non-existent)" -Method GET -Url "http://localhost:5005/api/v1/payments/99999" -ExpectedStatuses @(404)

# ─── 3. NOTIFICATION SERVICE REST API ───────────────────────────────────────
Write-Host "`n[3] NotificationService REST API Tests" -ForegroundColor Magenta

# Create mock notification manually
Invoke-ApiTest -Name "Create Notification (Manual)" -Method POST -Url "http://localhost:5004/api/v1/notifications" `
    -Body '{ "userId": 202, "orderId": 101, "type": "PAYMENT_SUCCESS", "message": "Manual test notification" }' `
    -ExpectedStatuses @(201) -ExpectBodyContains "Manual test notification"

# Missing required fields in notification request
Invoke-ApiTest -Name "Create Notification (Missing UserId)" -Method POST -Url "http://localhost:5004/api/v1/notifications" `
    -Body '{ "type": "ORDER_CREATED", "message": "Test" }' `
    -ExpectedStatuses @(400) -ExpectBodyContains "UserId is required"

# Get notification by ID non-existent resource
Invoke-ApiTest -Name "Get Notification by ID (Non-existent)" -Method GET -Url "http://localhost:5004/api/v1/notifications/99999" -ExpectedStatuses @(404)

# ─── 4. EVENTBRIDGE SQS SIMULATION & IDEMPOTENCY ────────────────────────────
Write-Host "`n[4] EventBridge/SQS Consumer Simulation & Idempotency Tests" -ForegroundColor Magenta

# Clear idempotency cache and notifications store for isolated simulation
Invoke-ApiTest -Name "Clear Idempotency Cache" -Method POST -Url "http://localhost:5004/api/v1/notifications/clear-cache" -ExpectedStatuses @(200)

# Simulate OrderCreated Event
$orderCreatedPayload = '{
  "id": "event-uuid-order-created",
  "detail-type": "OrderCreated",
  "source": "com.smartretailx.order-service",
  "time": "2026-08-15T14:42:00Z",
  "detail": {
    "version": "1.0",
    "orderId": 301,
    "userId": 501,
    "totalAmount": 1250.00,
    "currency": "USD",
    "status": "Pending",
    "orderDate": "2026-08-15T14:42:00Z",
    "items": []
  }
}'
Invoke-ApiTest -Name "Simulate Event: OrderCreated" -Method POST -Url "http://localhost:5004/api/v1/notifications/simulate-sqs" `
    -Body $orderCreatedPayload -ExpectedStatuses @(200)

# Verify notification was generated for OrderCreated
Invoke-ApiTest -Name "Verify Notification Created (OrderCreated)" -Method GET -Url "http://localhost:5004/api/v1/notifications" `
    -ExpectedStatuses @(200) -ExpectBodyContains "Order #301 has been created"

# Test Idempotency: Resend identical event (same Event ID "event-uuid-order-created")
Invoke-ApiTest -Name "Simulate Event: OrderCreated (Duplicate ID)" -Method POST -Url "http://localhost:5004/api/v1/notifications/simulate-sqs" `
    -Body $orderCreatedPayload -ExpectedStatuses @(200)

# Verify duplicate was skipped (check that no second notification for Order #301 is created)
$notifJson = Invoke-ApiTest -Name "Get All Notifications to verify duplicate skip" -Method GET -Url "http://localhost:5004/api/v1/notifications" -ExpectedStatuses @(200)

$count301 = ([regex]::Matches($notifJson, "Order #301")).Count
if ($count301 -eq 1) {
    $global:Pass++
    Write-Host "[PASS] Idempotency successfully prevented duplicate notification creation." -ForegroundColor Green
} else {
    $global:Fail++
    Write-Host "[FAIL] Idempotency failed! Found $count301 notifications for Order #301 (Expected: 1)." -ForegroundColor Red
}

# Clear cache and verify duplicate can now be processed
Invoke-ApiTest -Name "Clear Idempotency Cache again" -Method POST -Url "http://localhost:5004/api/v1/notifications/clear-cache" -ExpectedStatuses @(200)

Invoke-ApiTest -Name "Simulate Event: OrderCreated (Post Cache Clear)" -Method POST -Url "http://localhost:5004/api/v1/notifications/simulate-sqs" `
    -Body $orderCreatedPayload -ExpectedStatuses @(200)

$notifJsonPostClear = Invoke-ApiTest -Name "Get All Notifications to verify duplicate success" -Method GET -Url "http://localhost:5004/api/v1/notifications" -ExpectedStatuses @(200)
$count301PostClear = ([regex]::Matches($notifJsonPostClear, "Order #301")).Count
if ($count301PostClear -eq 2) {
    $global:Pass++
    Write-Host "[PASS] Duplicate successfully processed after cache clearing." -ForegroundColor Green
} else {
    $global:Fail++
    Write-Host "[FAIL] Duplicate not processed after cache clear (Expected count: 2, Got: $count301PostClear)." -ForegroundColor Red
}

# Simulate PaymentCompleted Event
$paymentCompletedPayload = '{
  "id": "event-uuid-payment-success",
  "detail-type": "PaymentCompleted",
  "source": "com.smartretailx.payment-service",
  "time": "2026-08-15T14:43:00Z",
  "detail": {
    "version": "1.0",
    "paymentId": 12,
    "orderId": 301,
    "userId": 501,
    "amount": 1250.00,
    "currency": "USD",
    "paymentMethod": "Card",
    "transactionRef": "txn_abcdef123456",
    "paidAt": "2026-08-15T14:43:00Z"
  }
}'
Invoke-ApiTest -Name "Simulate Event: PaymentCompleted" -Method POST -Url "http://localhost:5004/api/v1/notifications/simulate-sqs" `
    -Body $paymentCompletedPayload -ExpectedStatuses @(200)

Invoke-ApiTest -Name "Verify Notification Created (PaymentCompleted)" -Method GET -Url "http://localhost:5004/api/v1/notifications" `
    -ExpectedStatuses @(200) -ExpectBodyContains "Payment confirmed for Order #301"

# Simulate PaymentFailed Event
$paymentFailedPayload = '{
  "id": "event-uuid-payment-fail",
  "detail-type": "PaymentFailed",
  "source": "com.smartretailx.payment-service",
  "time": "2026-08-15T14:44:00Z",
  "detail": {
    "version": "1.0",
    "paymentId": 13,
    "orderId": 302,
    "userId": 501,
    "amount": 99.99,
    "currency": "USD",
    "failureReason": "insufficient_funds",
    "failedAt": "2026-08-15T14:44:00Z"
  }
}'
Invoke-ApiTest -Name "Simulate Event: PaymentFailed" -Method POST -Url "http://localhost:5004/api/v1/notifications/simulate-sqs" `
    -Body $paymentFailedPayload -ExpectedStatuses @(200)

Invoke-ApiTest -Name "Verify Notification Created (PaymentFailed)" -Method GET -Url "http://localhost:5004/api/v1/notifications" `
    -ExpectedStatuses @(200) -ExpectBodyContains "Payment attempt failed for Order #302"

# ─── SUMMARY ───────────────────────────────────────────────────────────────
$total = $global:Pass + $global:Fail
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   TEST SUMMARY" -ForegroundColor Cyan
Write-Host "   Total  : $total" -ForegroundColor White
Write-Host "   Pass   : $($global:Pass)" -ForegroundColor Green
$failColor = if ($global:Fail -eq 0) { "Green" } else { "Red" }
Write-Host "   Fail   : $($global:Fail)" -ForegroundColor $failColor
Write-Host "============================================================" -ForegroundColor Cyan

Write-Host "`nDetailed Results:" -ForegroundColor Cyan
$global:Results | Format-Table -AutoSize -Property `
    @{L="Status";E={ if ($_.Passed) {"PASS"} else {"FAIL"} }},
    "Method","Name","HTTP","Expected"

if ($global:Fail -gt 0) {
    Write-Host "Some tests FAILED. Please check the logs." -ForegroundColor Red
    exit 1
}
Write-Host "All $total tests PASSED successfully!" -ForegroundColor Green
exit 0
