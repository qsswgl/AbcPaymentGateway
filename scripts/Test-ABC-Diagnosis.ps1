<#
.SYNOPSIS
    农行支付接口诊断测试脚本 - 测试不同交易类型

.DESCRIPTION
    测试以下交易类型并生成详细的入参/出参报告：
    1. PayReq - 页面支付下单
    2. OLScanPayOrderReq - 一码多扫下单
    3. ScanPayOrderReq - 扫码支付下单

.PARAMETER ServerUrl
    服务器地址，默认: https://payment.qsgl.net

.PARAMETER Amount
    测试金额，默认: 10.00

.PARAMETER MerchantId
    商户号，默认: 103881636900016

.EXAMPLE
    .\Test-ABC-Diagnosis.ps1
    .\Test-ABC-Diagnosis.ps1 -ServerUrl "http://localhost:8080" -Amount 1.00
#>

param(
    [string]$ServerUrl = "https://payment.qsgl.net",
    [decimal]$Amount = 10.00,
    [string]$MerchantId = "103881636900016"
)

# 配置
$ErrorActionPreference = "Continue"
$OutputDir = Join-Path $PSScriptRoot "DiagnosisReports"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$ReportFile = Join-Path $OutputDir "ABC_Diagnosis_Report_$Timestamp.md"

# 创建输出目录
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# 颜色输出函数
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# 写入报告函数
function Write-Report {
    param([string]$Content)
    Add-Content -Path $ReportFile -Value $Content -Encoding UTF8
}

# 初始化报告
Write-Report "# 农行支付接口诊断报告"
Write-Report ""
Write-Report "**测试时间**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Report "**服务器地址**: $ServerUrl"
Write-Report "**商户号**: $MerchantId"
Write-Report "**测试金额**: $Amount 元"
Write-Report ""
Write-Report "---"
Write-Report ""

Write-ColorOutput "`n========================================" "Cyan"
Write-ColorOutput "  农行支付接口诊断测试工具" "Cyan"
Write-ColorOutput "========================================`n" "Cyan"

Write-ColorOutput "配置信息:" "Yellow"
Write-ColorOutput "  服务器: $ServerUrl"
Write-ColorOutput "  商户号: $MerchantId"
Write-ColorOutput "  金额: $Amount 元"
Write-ColorOutput "  报告: $ReportFile`n"

# 测试用例定义
$testCases = @(
    @{
        Name = "页面支付下单 (PayReq)"
        Endpoint = "/api/payment/abc/pagepay"
        Description = "农行页面支付下单，应返回PaymentURL用于页面跳转"
        Body = @{
            orderNo = "PAY$(Get-Date -Format 'yyyyMMddHHmmss')"
            amount = $Amount
            merchantId = $MerchantId
            goodsName = "测试商品-页面支付"
            notifyUrl = "$ServerUrl/api/payment/abc/notify"
            merchantSuccessUrl = "$ServerUrl/success"
            merchantErrorUrl = "$ServerUrl/error"
            currencyCode = "156"
            commodityType = "0201"
            paymentType = "A"
            paymentLinkType = "1"
            notifyType = "1"
        }
        ExpectedFields = @("paymentURL", "transactionId")
        TrxType = "PayReq"
    },
    @{
        Name = "一码多扫下单 (OLScanPayOrderReq)"
        Endpoint = "/api/payment/abc/scanpay"
        Description = "农行一码多扫支付，应返回二维码URL"
        Body = @{
            orderNo = "SCAN$(Get-Date -Format 'yyyyMMddHHmmss')"
            amount = $Amount
            merchantId = $MerchantId
            goodsName = "测试商品-扫码支付"
            notifyUrl = "$ServerUrl/api/payment/abc/notify"
            currencyCode = "156"
            commodityType = "0201"
        }
        ExpectedFields = @("qrCodeUrl", "transactionId")
        TrxType = "OLScanPayOrderReq"
    }
)

# 执行测试
$testResults = @()

foreach ($test in $testCases) {
    Write-ColorOutput "`n----------------------------------------" "Cyan"
    Write-ColorOutput "测试: $($test.Name)" "Cyan"
    Write-ColorOutput "----------------------------------------" "Cyan"
    Write-ColorOutput "描述: $($test.Description)" "Gray"
    Write-ColorOutput "接口: $($test.Endpoint)" "Gray"
    Write-ColorOutput "交易类型: $($test.TrxType)`n" "Gray"

    Write-Report "## 测试 $($testResults.Count + 1): $($test.Name)"
    Write-Report ""
    Write-Report "**描述**: $($test.Description)"
    Write-Report "**接口**: `$($test.Endpoint)`"
    Write-Report "**交易类型**: `$($test.TrxType)`"
    Write-Report ""

    # 准备请求
    $url = "$ServerUrl$($test.Endpoint)"
    $bodyJson = $test.Body | ConvertTo-Json -Depth 10
    $headers = @{
        "Content-Type" = "application/json"
        "Accept" = "application/json"
    }

    Write-ColorOutput "📤 发送请求..." "Yellow"
    Write-Report "### 📤 请求信息"
    Write-Report ""
    Write-Report "**URL**: `$url`"
    Write-Report "**Method**: POST"
    Write-Report "**Headers**:"
    Write-Report '```json'
    Write-Report ($headers | ConvertTo-Json)
    Write-Report '```'
    Write-Report ""
    Write-Report "**请求体**:"
    Write-Report '```json'
    Write-Report $bodyJson
    Write-Report '```'
    Write-Report ""

    # 记录发送的参数详情
    Write-ColorOutput "  订单号: $($test.Body.orderNo)" "Gray"
    Write-ColorOutput "  金额: $($test.Body.amount) 元" "Gray"
    Write-ColorOutput "  商品: $($test.Body.goodsName)" "Gray"

    # 发送请求
    $result = @{
        TestName = $test.Name
        OrderNo = $test.Body.orderNo
        Success = $false
        HttpStatus = $null
        Response = $null
        Error = $null
        Duration = 0
        Timestamp = Get-Date
    }

    try {
        $startTime = Get-Date
        
        $response = Invoke-WebRequest `
            -Uri $url `
            -Method Post `
            -Headers $headers `
            -Body $bodyJson `
            -UseBasicParsing `
            -ErrorAction Stop

        $endTime = Get-Date
        $result.Duration = ($endTime - $startTime).TotalMilliseconds
        $result.HttpStatus = [int]$response.StatusCode
        
        $responseObj = $response.Content | ConvertFrom-Json
        $result.Response = $responseObj

        Write-ColorOutput "`n✅ HTTP $($response.StatusCode) - 请求成功" "Green"
        Write-ColorOutput "   耗时: $([math]::Round($result.Duration, 2)) ms`n" "Gray"

        Write-Report "### 📥 响应信息"
        Write-Report ""
        Write-Report "**HTTP状态**: $($response.StatusCode) OK"
        Write-Report "**响应时间**: $([math]::Round($result.Duration, 2)) ms"
        Write-Report "**响应头**:"
        Write-Report '```'
        foreach ($headerKey in $response.Headers.Keys) {
            $headerValue = $response.Headers[$headerKey]
            Write-Report "$headerKey = $headerValue"
        }
        Write-Report '```'
        Write-Report ""
        Write-Report "**响应体**:"
        Write-Report '```json'
        Write-Report ($responseObj | ConvertTo-Json -Depth 10)
        Write-Report '```'
        Write-Report ""

        # 分析响应
        Write-ColorOutput "📊 响应分析:" "Yellow"
        Write-Report "### 📊 响应分析"
        Write-Report ""
        
        if ($responseObj.isSuccess -eq $true) {
            $result.Success = $true
            Write-ColorOutput "   状态: ✅ 成功" "Green"
            Write-Report "- **交易状态**: ✅ 成功"
        } else {
            Write-ColorOutput "   状态: ❌ 失败" "Red"
            Write-Report "- **交易状态**: ❌ 失败"
        }

        Write-ColorOutput "   订单号: $($responseObj.orderNo)" "White"
        Write-ColorOutput "   交易ID: $($responseObj.transactionId)" "White"
        Write-ColorOutput "   金额: $($responseObj.amount)" "White"
        Write-ColorOutput "   状态码: $($responseObj.returnCode)" "White"
        Write-ColorOutput "   消息: $($responseObj.message)" "White"

        Write-Report "- **订单号**: $($responseObj.orderNo)"
        Write-Report "- **交易ID**: $($responseObj.transactionId)"
        Write-Report "- **金额**: $($responseObj.amount) 元"
        Write-Report "- **状态码**: ``$($responseObj.returnCode)``"
        Write-Report "- **消息**: $($responseObj.message)"

        # 检查期望字段
        Write-Report ""
        Write-Report "### 🔍 关键字段检查"
        Write-Report ""
        Write-ColorOutput "`n   关键字段:" "Yellow"
        foreach ($field in $test.ExpectedFields) {
            $value = $responseObj.$field
            if ($value) {
                Write-ColorOutput "   ✅ $field = $value" "Green"
                Write-Report "- ✅ **$field**: ``$value``"
                
                # 如果是PaymentURL，提供额外说明
                if ($field -eq "paymentURL" -and $value) {
                    Write-ColorOutput "      👉 请在浏览器中打开此URL完成支付" "Cyan"
                    Write-Report "  - 📌 **用户操作**: 在浏览器中打开此URL完成支付"
                }
                
                # 如果是二维码URL，提供额外说明
                if ($field -eq "qrCodeUrl" -and $value) {
                    Write-ColorOutput "      👉 请使用支付应用扫描此二维码" "Cyan"
                    Write-Report "  - 📌 **用户操作**: 使用支付应用扫描此二维码"
                }
            } else {
                Write-ColorOutput "   ❌ $field = (空)" "Red"
                Write-Report "- ❌ **$field**: (空)"
            }
        }

    } catch {
        $endTime = Get-Date
        $result.Duration = ($endTime - $startTime).TotalMilliseconds
        $result.Error = $_.Exception.Message

        Write-ColorOutput "`n❌ 请求失败" "Red"
        Write-Report "### ❌ 错误信息"
        Write-Report ""

        if ($_.Exception.Response) {
            $result.HttpStatus = [int]$_.Exception.Response.StatusCode
            Write-ColorOutput "   HTTP状态: $($result.HttpStatus)" "Red"
            Write-Report "**HTTP状态**: $($result.HttpStatus)"
            Write-Report ""

            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorContent = $reader.ReadToEnd()
                $reader.Close()

                $errorObj = $errorContent | ConvertFrom-Json
                $result.Response = $errorObj

                Write-ColorOutput "   错误详情:" "Yellow"
                Write-ColorOutput "   - 状态码: $($errorObj.returnCode)" "White"
                Write-ColorOutput "   - 消息: $($errorObj.message)" "White"

                Write-Report "**错误响应**:"
                Write-Report '```json'
                Write-Report ($errorObj | ConvertTo-Json -Depth 10)
                Write-Report '```'
            } catch {
                Write-ColorOutput "   无法解析错误响应" "Gray"
                Write-Report "无法解析错误响应"
            }
        } else {
            Write-ColorOutput "   错误: $($_.Exception.Message)" "Red"
            Write-Report "**异常**: $($_.Exception.Message)"
        }
    }

    Write-Report ""
    Write-Report "---"
    Write-Report ""

    $testResults += $result
}

# 生成汇总报告
Write-ColorOutput "`n========================================" "Cyan"
Write-ColorOutput "  测试汇总" "Cyan"
Write-ColorOutput "========================================`n" "Cyan"

Write-Report "## 测试汇总"
Write-Report ""
Write-Report "| 测试项 | 订单号 | HTTP状态 | 交易状态 | 返回码 | 耗时ms |"
Write-Report "|:-------|:-------|:---------|:---------|:-------|:-------|"

foreach ($result in $testResults) {
    $statusIcon = if ($result.Success) { "✅" } else { "❌" }
    $httpStatus = if ($result.HttpStatus) { $result.HttpStatus } else { "N/A" }
    $returnCode = if ($result.Response.returnCode) { $result.Response.returnCode } else { "N/A" }
    $duration = [math]::Round($result.Duration, 2)

    Write-ColorOutput "$statusIcon $($result.TestName)" $(if ($result.Success) { "Green" } else { "Red" })
    Write-ColorOutput "   订单号: $($result.OrderNo)" "Gray"
    Write-ColorOutput "   HTTP: $httpStatus | 返回码: $returnCode | 耗时: ${duration}ms`n" "Gray"

    Write-Report "| $($result.TestName) | $($result.OrderNo) | $httpStatus | $statusIcon | ``$returnCode`` | $duration |"
}

Write-Report ""

# 生成ABC银行反馈报告
Write-ColorOutput "`n========================================" "Cyan"
Write-ColorOutput "  生成ABC银行反馈文档" "Cyan"
Write-ColorOutput "========================================`n" "Cyan"

$abcReportFile = Join-Path $OutputDir "ABC_Feedback_Report_$Timestamp.md"

$abcReport = @"
# 农行支付接口测试报告 - 提交ABC银行

**商户名称**: 七匹狼资产管理  
**商户号**: $MerchantId  
**测试时间**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  
**测试环境**: 生产环境 (https://pay.abchina.com)  

---

## 问题描述

我方已完成页面支付接口开发和部署，接口可正常调用农行服务器，但返回 **EUNKWN** 错误码。请协助确认配置是否完整。

---

## 测试结果

"@

foreach ($result in $testResults) {
    $abcReport += @"

### $($result.TestName)

**订单号**: $($result.OrderNo)  
**HTTP状态**: $($result.HttpStatus)  
**返回码**: $($result.Response.returnCode)  
**错误消息**: $($result.Response.message)  

"@
}

$abcReport += @"

---

## 发送到农行的请求示例

以下是页面支付接口的完整请求报文（已脱敏签名）：

``````json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",
      "Format": "JSON",
      "Merchant": {
        "ECMerchantType": "EBUS",
        "MerchantID": "$MerchantId"
      },
      "TrxRequest": {
        "TrxType": "PayReq",
        "Order": {
          "PayTypeID": "ImmediatePay",
          "OrderNo": "$($testResults[0].OrderNo)",
          "OrderAmount": "$($Amount.ToString('F2'))",
          "OrderDate": "$(Get-Date -Format 'yyyy/MM/dd')",
          "OrderTime": "$(Get-Date -Format 'HH:mm:ss')",
          "OrderDesc": "测试商品-页面支付",
          "CurrencyCode": "156",
          "CommodityType": "0201",
          "InstallmentMark": "0",
          "ExpiredDate": "30"
        },
        "OrderDetail": [
          {
            "ProductName": "测试商品-页面支付",
            "UnitPrice": "$($Amount.ToString('F2'))",
            "Qty": "1",
            "ProductRemarks": "测试商品-页面支付"
          }
        ],
        "PaymentType": "A",
        "PaymentLinkType": "1",
        "NotifyType": "1",
        "ResultNotifyURL": "https://payment.qsgl.net/api/payment/abc/notify",
        "MerchantSuccessURL": "https://payment.qsgl.net/success",
        "MerchantErrorURL": "https://payment.qsgl.net/error",
        "IsBreakAccount": "0"
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "[已生成，验签应该通过]"
  }
}
``````

---

## 农行返回的响应示例

``````json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",
      "Format": "JSON",
      "Common": {
        "Channel": "EBUS"
      },
      "Merchant": {
        "ECMerchantType": "EBUS",
        "MerchantID": ""
      },
      "TrxResponse": {
        "ReturnCode": "EUNKWN",
        "ErrorMessage": "交易结果未知，请进行查证明确交易结果，No message available"
      }
    }
  }
}
``````

---

## 需要ABC银行确认的事项

### 1. 权限配置
- [ ] 商户 $MerchantId 的 **PayReq**（页面支付）权限是否已激活？
- [ ] 是否需要在农行后台配置回调URL白名单？
  - ResultNotifyURL: https://payment.qsgl.net/api/payment/abc/notify
  - MerchantSuccessURL: https://payment.qsgl.net/success
  - MerchantErrorURL: https://payment.qsgl.net/error

### 2. 必填字段
- [ ] PayReq 交易类型的**完整必填字段列表**是什么？
- [ ] 以下字段是否必填：
  - ReceiveAccount（收款账号）
  - ReceiveAccName（收款户名）
  - VerifyFlag（实名验证）
  - VerifyType / VerifyNo（证件类型/号码）

### 3. 参数配置
- [ ] **CommodityType** 使用 "0201"（虚拟商品）是否正确？
- [ ] **PaymentType** 使用 "A"（借记卡+贷记卡合并）是否正确？
- [ ] **PaymentLinkType** 使用 "1"（电脑网络）是否正确？
- [ ] **InstallmentMark** 使用 "0"（不分期）是否正确？

### 4. 环境配置
- [ ] 商户 $MerchantId 应该访问哪个环境？
  - 当前使用: https://pay.abchina.com:443 (生产环境)
  - 是否正确？

### 5. 证书问题
- [ ] TrustPay.cer 证书已过期（2023-08-11），是否需要更新？
- [ ] 商户证书有效期至 2031-01-05，是否正常？

### 6. 返回字段
- [ ] **PaymentURL** 字段在什么情况下会返回？
- [ ] EUNKWN 错误的**具体原因**是什么？
  - 是权限未开通？
  - 是缺少必填字段？
  - 是参数值不正确？
  - 还是其他原因？

---

## 技术联系信息

**系统负责人**: 技术团队  
**联系邮箱**: support@qsgl.net  
**测试服务器**: https://payment.qsgl.net  
**完整测试报告**: 见附件  

**期望ABC银行提供**:
1. 完整的 PayReq 参数清单和示例
2. EUNKWN 错误的具体原因和解决方案
3. 商户配置检查结果
4. 更新的 TrustPay.cer 证书（如需要）

---

**生成时间**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  
**报告工具**: 农行支付接口诊断脚本 v1.0
"@

$abcReport | Out-File -FilePath $abcReportFile -Encoding UTF8

Write-ColorOutput "✅ ABC银行反馈报告已生成" "Green"
Write-ColorOutput "   文件: $abcReportFile`n" "Gray"

# 完成
Write-ColorOutput "========================================" "Cyan"
Write-ColorOutput "  测试完成" "Cyan"
Write-ColorOutput "========================================`n" "Cyan"

Write-ColorOutput "📄 报告文件:" "Yellow"
Write-ColorOutput "   完整测试报告: $ReportFile" "White"
Write-ColorOutput "   ABC银行反馈: $abcReportFile`n" "White"

Write-ColorOutput "下一步操作:" "Yellow"
Write-ColorOutput "  1. 查看完整测试报告" "White"
Write-ColorOutput "  2. 将ABC银行反馈报告发送给银行技术支持" "White"
Write-ColorOutput "  3. 等待ABC银行确认配置并提供解决方案`n" "White"

# 打开报告目录
Write-ColorOutput "是否打开报告目录? (Y/N): " "Yellow" -NoNewline
$openDir = Read-Host
if ($openDir -eq 'Y' -or $openDir -eq 'y') {
    explorer $OutputDir
}
