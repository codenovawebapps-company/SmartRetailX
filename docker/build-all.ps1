# =============================================================================
#  SmartRetailX — Docker Image Build Script
# =============================================================================

$rootDir = Split-Path -Parent $PSScriptRoot

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Building SmartRetailX Docker Images" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$images = @(
    @{ Tag = "smartretailx-user:latest";         File = "$PSScriptRoot\user-service.Dockerfile" },
    @{ Tag = "smartretailx-product:latest";      File = "$PSScriptRoot\product-service.Dockerfile" },
    @{ Tag = "smartretailx-order:latest";        File = "$PSScriptRoot\order-service.Dockerfile" },
    @{ Tag = "smartretailx-inventory:latest";    File = "$PSScriptRoot\inventory-service.Dockerfile" },
    @{ Tag = "smartretailx-payment:latest";      File = "$PSScriptRoot\payment-service.Dockerfile" },
    @{ Tag = "smartretailx-notification:latest"; File = "$PSScriptRoot\notification-service.Dockerfile" },
    @{ Tag = "smartretailx-gateway:latest";      File = "$PSScriptRoot\api-gateway.Dockerfile" }
)

foreach ($img in $images) {
    Write-Host "`nBuilding $($img.Tag)..." -ForegroundColor Yellow
    docker build -t $img.Tag -f $img.File $rootDir
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Successfully built $($img.Tag)" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Failed to build $($img.Tag)" -ForegroundColor Red
    }
}

Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host "  Docker Build Completed" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
