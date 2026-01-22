# Test ABC PagePay API - Production
$ErrorActionPreference = "Stop"

Write-Host "`n========================================"
Write-Host "  Testing ABC PagePay API (Production)"
Write-Host "========================================`n"

$apiUrl = "https://payment.qsgl.net/api/payment/abc/pagepay"

# 正确的属性名称 (与 AbcPagePayRequest 模型匹配)
$testData = @{
    OrderNo = "TEST" + (Get-Date -Format "yyyyMMddHHmmss")
    Amount = 0.01
    MerchantId = "103881636900016"
    GoodsName = "Test Product"
    NotifyUrl = "https://payment.qsgl.net/api/payment/abc/notify"
    MerchantSuccessUrl = "https://payment.qsgl.net/success"
    MerchantErrorUrl = "https://payment.qsgl.net/fail"
}

Write-Host "Test Data:"
Write-Host "  OrderNo: $($testData.OrderNo)"
Write-Host "  Amount: $($testData.Amount)"
Write-Host "  MerchantId: $($testData.MerchantId)"
Write-Host "  GoodsName: $($testData.GoodsName)`n"

Write-Host "API Endpoint: $apiUrl`n"

try {
    Write-Host "Sending request..." -ForegroundColor Yellow
    
    $jsonBody = $testData | ConvertTo-Json
    Write-Host "Request Body:" -ForegroundColor Gray
    Write-Host $jsonBody -ForegroundColor Gray
    Write-Host ""
    
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -ContentType "application/json" -Body $jsonBody -TimeoutSec 30
    
    Write-Host "========================================"
    Write-Host "  API Response Success!"
    Write-Host "========================================`n"
    
    Write-Host "Response:" -ForegroundColor Yellow
    $response | ConvertTo-Json | Write-Host -ForegroundColor White
    
    if ($response.paymentURL) {
        Write-Host "`n========================================"
        Write-Host "  PaymentURL Generated!"
        Write-Host "========================================`n"
        
        Write-Host "PaymentURL:" -ForegroundColor Yellow
        Write-Host $response.paymentURL -ForegroundColor Green
        
        if ($response.paymentURL -match "pay\.abchina\.com.*TOKEN=") {
            Write-Host "`nURL format is correct!" -ForegroundColor Green
            
            if ($response.paymentURL -match "TOKEN=([^&]+)") {
                $token = $Matches[1]
                Write-Host "TOKEN extracted: $token" -ForegroundColor Green
            }
        }
        
        Write-Host "`nOpen in browser? (Y/N): " -NoNewline
        $open = Read-Host
        
        if ($open -eq "Y" -or $open -eq "y") {
            Write-Host "Opening browser..." -ForegroundColor Yellow
            Start-Process $response.paymentURL
        }
    } else {
        Write-Host "`nWarning: No paymentURL in response" -ForegroundColor Yellow
    }
    
    Write-Host "`nOther Info:" -ForegroundColor Yellow
    if ($response.isSuccess) {
        Write-Host "  Status: SUCCESS" -ForegroundColor Green
    } else {
        Write-Host "  Status: FAILED" -ForegroundColor Red
    }
    
    if ($response.message) {
        Write-Host "  Message: $($response.message)"
    }
    
} catch {
    Write-Host "`n========================================"
    Write-Host "  API Call Failed!"
    Write-Host "========================================`n"
    
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)"
        
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "`nResponse Body:" -ForegroundColor Yellow
            Write-Host $responseBody -ForegroundColor Red
        } catch {}
    }
    
    Write-Host "`nTroubleshooting:" -ForegroundColor Cyan
    Write-Host "  1. Check service: docker-compose ps"
    Write-Host "  2. Check logs: docker-compose logs -f payment"
    Write-Host "  3. Check Swagger: https://payment.qsgl.net/swagger"
    
    exit 1
}

Write-Host "`nTest completed!`n" -ForegroundColor Green
