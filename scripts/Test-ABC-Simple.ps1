<#
.SYNOPSIS
    ABC Payment Gateway Diagnosis Test Script
.DESCRIPTION
    Test different ABC payment transaction types
.EXAMPLE
    .\Test-ABC-Simple.ps1
#>

param(
    [string]$ServerUrl = "https://payment.qsgl.net",
    [decimal]$Amount = 10.00,
    [string]$MerchantId = "103881636900016"
)

$OutputDir = Join-Path $PSScriptRoot "DiagnosisReports"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$ReportFile = Join-Path $OutputDir "ABC_Test_Report_$Timestamp.md"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

function Write-Report { param([string]$Content) Add-Content -Path $ReportFile -Value $Content -Encoding UTF8 }

Write-Host "`n========================================"  -ForegroundColor Cyan
Write-Host "  ABC Payment Diagnosis Tool" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
Write-Host "Server: $ServerUrl" -ForegroundColor Yellow
Write-Host "Merchant: $MerchantId" -ForegroundColor Yellow
Write-Host "Amount: $Amount CNY`n" -ForegroundColor Yellow

Write-Report "# ABC Payment Test Report"
Write-Report ""
Write-Report "**Test Time**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Report "**Server**: $ServerUrl"
Write-Report "**Merchant**: $MerchantId"
Write-Report "**Amount**: $Amount CNY"
Write-Report ""

# Test Case 1: Page Payment
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host "Test 1: Page Payment (PayReq)" -ForegroundColor Cyan
Write-Host "----------------------------------------`n" -ForegroundColor Cyan

$orderNo1 = "PAY$(Get-Date -Format 'yyyyMMddHHmmss')"
$url1 = "$ServerUrl/api/payment/abc/pagepay"
$body1 = @{
    orderNo = $orderNo1
    amount = $Amount
    merchantId = $MerchantId
    goodsName = "Test Product - Page Payment"
    notifyUrl = "$ServerUrl/api/payment/abc/notify"
    merchantSuccessUrl = "$ServerUrl/success"
    merchantErrorUrl = "$ServerUrl/error"
} | ConvertTo-Json

Write-Host "Order No: $orderNo1" -ForegroundColor Gray
Write-Host "Endpoint: /api/payment/abc/pagepay`n" -ForegroundColor Gray

Write-Report "## Test 1: Page Payment (PayReq)"
Write-Report ""
Write-Report "**Order No**: $orderNo1"
Write-Report "**Endpoint**: /api/payment/abc/pagepay"
Write-Report ""
Write-Report "### Request"
Write-Report '```json'
Write-Report $body1
Write-Report '```'
Write-Report ""

try {
    $startTime = Get-Date
    $response = Invoke-WebRequest -Uri $url1 -Method Post -ContentType "application/json" -Body $body1 -UseBasicParsing
    $duration = ((Get-Date) - $startTime).TotalMilliseconds
    $result = $response.Content | ConvertFrom-Json
    
    Write-Host "HTTP Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Duration: $([math]::Round($duration, 2)) ms`n" -ForegroundColor Gray
    
    Write-Report "### Response"
    Write-Report ""
    Write-Report "**HTTP Status**: $($response.StatusCode)"
    Write-Report "**Duration**: $([math]::Round($duration, 2)) ms"
    Write-Report ""
    Write-Report '```json'
    Write-Report ($result | ConvertTo-Json -Depth 10)
    Write-Report '```'
    Write-Report ""
    
    Write-Host "Response Analysis:" -ForegroundColor Yellow
    Write-Host "  Success: $($result.isSuccess)" -ForegroundColor $(if ($result.isSuccess) { "Green" } else { "Red" })
    Write-Host "  Order No: $($result.orderNo)" -ForegroundColor White
    Write-Host "  Transaction ID: $($result.transactionId)" -ForegroundColor White
    Write-Host "  Return Code: $($result.returnCode)" -ForegroundColor White
    Write-Host "  Message: $($result.message)" -ForegroundColor White
    Write-Host "  Payment URL: $($result.paymentURL)`n" -ForegroundColor $(if ($result.paymentURL) { "Green" } else { "Red" })
    
    Write-Report "### Analysis"
    Write-Report ""
    Write-Report "- **Success**: $($result.isSuccess)"
    Write-Report "- **Order No**: $($result.orderNo)"
    Write-Report "- **Transaction ID**: $($result.transactionId)"
    Write-Report "- **Return Code**: $($result.returnCode)"
    Write-Report "- **Message**: $($result.message)"
    Write-Report "- **Payment URL**: $($result.paymentURL)"
    Write-Report ""
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            $reader.Close()
            $errorObj = $errorBody | ConvertFrom-Json
            
            Write-Host "Error Code: $($errorObj.returnCode)" -ForegroundColor Red
            Write-Host "Error Message: $($errorObj.message)`n" -ForegroundColor Red
            
            Write-Report "### Error"
            Write-Report ""
            Write-Report '```json'
            Write-Report ($errorObj | ConvertTo-Json -Depth 10)
            Write-Report '```'
            Write-Report ""
        } catch {
            Write-Report "### Error"
            Write-Report ""
            Write-Report $_.Exception.Message
            Write-Report ""
        }
    }
}

# Test Case 2: Scan Payment
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host "Test 2: Scan Payment (OLScanPayOrderReq)" -ForegroundColor Cyan
Write-Host "----------------------------------------`n" -ForegroundColor Cyan

$orderNo2 = "SCAN$(Get-Date -Format 'yyyyMMddHHmmss')"
$url2 = "$ServerUrl/api/payment/abc/scanpay"
$body2 = @{
    orderNo = $orderNo2
    amount = $Amount
    merchantId = $MerchantId
    goodsName = "Test Product - Scan Payment"
    notifyUrl = "$ServerUrl/api/payment/abc/notify"
} | ConvertTo-Json

Write-Host "Order No: $orderNo2" -ForegroundColor Gray
Write-Host "Endpoint: /api/payment/abc/scanpay`n" -ForegroundColor Gray

Write-Report "## Test 2: Scan Payment (OLScanPayOrderReq)"
Write-Report ""
Write-Report "**Order No**: $orderNo2"
Write-Report "**Endpoint**: /api/payment/abc/scanpay"
Write-Report ""
Write-Report "### Request"
Write-Report '```json'
Write-Report $body2
Write-Report '```'
Write-Report ""

try {
    $startTime = Get-Date
    $response = Invoke-WebRequest -Uri $url2 -Method Post -ContentType "application/json" -Body $body2 -UseBasicParsing
    $duration = ((Get-Date) - $startTime).TotalMilliseconds
    $result = $response.Content | ConvertFrom-Json
    
    Write-Host "HTTP Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Duration: $([math]::Round($duration, 2)) ms`n" -ForegroundColor Gray
    
    Write-Report "### Response"
    Write-Report ""
    Write-Report "**HTTP Status**: $($response.StatusCode)"
    Write-Report "**Duration**: $([math]::Round($duration, 2)) ms"
    Write-Report ""
    Write-Report '```json'
    Write-Report ($result | ConvertTo-Json -Depth 10)
    Write-Report '```'
    Write-Report ""
    
    Write-Host "Response Analysis:" -ForegroundColor Yellow
    Write-Host "  Success: $($result.isSuccess)" -ForegroundColor $(if ($result.isSuccess) { "Green" } else { "Red" })
    Write-Host "  Order No: $($result.orderNo)" -ForegroundColor White
    Write-Host "  Transaction ID: $($result.transactionId)" -ForegroundColor White
    Write-Host "  Return Code: $($result.returnCode)" -ForegroundColor White
    Write-Host "  Message: $($result.message)" -ForegroundColor White
    Write-Host "  QR Code URL: $($result.qrCodeUrl)`n" -ForegroundColor $(if ($result.qrCodeUrl) { "Green" } else { "Red" })
    
    Write-Report "### Analysis"
    Write-Report ""
    Write-Report "- **Success**: $($result.isSuccess)"
    Write-Report "- **Order No**: $($result.orderNo)"
    Write-Report "- **Transaction ID**: $($result.transactionId)"
    Write-Report "- **Return Code**: $($result.returnCode)"
    Write-Report "- **Message**: $($result.message)"
    Write-Report "- **QR Code URL**: $($result.qrCodeUrl)"
    Write-Report ""
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            $reader.Close()
            $errorObj = $errorBody | ConvertFrom-Json
            
            Write-Host "Error Code: $($errorObj.returnCode)" -ForegroundColor Red
            Write-Host "Error Message: $($errorObj.message)`n" -ForegroundColor Red
            
            Write-Report "### Error"
            Write-Report ""
            Write-Report '```json'
            Write-Report ($errorObj | ConvertTo-Json -Depth 10)
            Write-Report '```'
            Write-Report ""
        } catch {
            Write-Report "### Error"
            Write-Report ""
            Write-Report $_.Exception.Message
            Write-Report ""
        }
    }
}

Write-Report "---"
Write-Report ""
Write-Report "**Report Generated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Completed" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Report saved to:" -ForegroundColor Yellow
Write-Host "  $ReportFile`n" -ForegroundColor White

Write-Host "Press any key to open report directory..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
explorer $OutputDir
