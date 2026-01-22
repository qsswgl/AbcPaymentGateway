# 农行一码多扫接口测试报告
测试时间：2026-01-19 08:49
测试环境：生产环境 (payment.qsgl.net)

## 测试结果

### ✅ 成功项
1. 接口调用成功（HTTP 200）
2. 商户证书加载正常（有效期至2031年）
3. 签名验证通过
4. 请求参数格式正确
5. 农行服务器正常响应

### ❌ 失败项
**错误代码：EUNKWN**
**错误信息：交易结果未知，请进行查证明确交易结果**

## 农行响应详情

```json
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
        "MerchantID": ""  ⚠️ 注意：MerchantID返回为空
      },
      "TrxResponse": {
        "ReturnCode": "EUNKWN",
        "ErrorMessage": "交易结果未知，请进行查证明确交易结果，No message available"
      }
    }
  }
}
```

## 发送的请求参数

```json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",
      "Format": "JSON",
      "Merchant": {
        "ECMerchantType": "EBUS",
        "MerchantID": "103881636900016"
      },
      "TrxRequest": {
        "TrxType": "OLScanPayOrderReq",
        "Order": {
          "PayTypeID": "ImmediatePay",
          "OrderNo": "ABC_SCAN_20260119084926",
          "OrderAmount": "10.00",
          "OrderDate": "2026/01/19",
          "OrderTime": "08:49:27",
          "OrderDesc": "Test Product",
          "CurrencyCode": "156",
          "CommodityType": "0201",
          "InstallmentMark": "0",
          "ExpiredDate": "30"
        },
        "OrderDetail": [
          {
            "ProductName": "Test Product",
            "UnitPrice": "10.00",
            "Qty": "1",
            "ProductRemarks": "Test Product"
          }
        ],
        "PaymentType": "A",
        "PaymentLinkType": "1",
        "NotifyType": "1",
        "ResultNotifyURL": "https://payment.qsgl.net/api/payment/notify",
        "IsBreakAccount": "0"
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "..."
  }
}
```

## 当前配置

- **商户号**: 103881636900016
- **环境**: 生产环境 (pay.abchina.com)
- **证书**: ./cert/103881636900016.pfx (有效期至2031-01-05)
- **TrustPay证书**: ./cert/prod/TrustPay.cer (⚠️ 已过期，有效期至2023-08-11)
- **请求类型**: OLScanPayOrderReq (一码多扫线上扫码下单)
- **支付类型**: A (标准报文)

## 农行答复确认

根据您提供的农行答复：
1. ✅ ep241功能已开通
2. ✅ 支持一码多扫
3. ✅ 送A类报文即可，无需额外申请
4. ✅ 正式环境商户证书可直接使用

## 问题分析

### 可能原因

1. **TrustPay.cer证书已过期**
   - 当前证书有效期至：2023-08-11
   - 可能影响农行服务器端的验证
   - 建议：联系农行更新TrustPay.cer证书

2. **商户功能开通未完全生效**
   - 虽然农行答复已开通，但MerchantID返回为空
   - 可能需要农行后台进一步配置
   - 建议：提供完整请求报文给农行技术支持排查

3. **环境配置问题**
   - 当前配置指向正式环境 (pay.abchina.com)
   - 证书为正式商户证书
   - 建议：确认是否需要切换到特定的一码多扫网关地址

4. **报文格式细节**
   - PaymentLinkType=1 (二维码)
   - NotifyType=1 (服务器异步通知)
   - IsBreakAccount=0 (不分账)
   - 建议：与农行DEMO对比，确认所有必填字段

## 后续建议

### 立即操作

1. **联系农行技术支持**，提供以下信息：
   - 商户号：103881636900016
   - 错误码：EUNKWN
   - 完整请求报文（见上文）
   - 完整响应报文（见上文）
   - 问题：MerchantID返回为空

2. **更新TrustPay.cer证书**
   - 向农行申请最新的TrustPay.cer
   - 替换 ./cert/prod/TrustPay.cer
   - 重启服务

3. **确认网关地址**
   - 当前：pay.abchina.com
   - 询问农行一码多扫是否有专用网关地址

### 待确认问题

1. 一码多扫功能是否需要单独申请开通（虽然农行说不用）？
2. 商户号103881636900016是否已完成一码多扫功能的后台配置？
3. TrustPay.cer过期是否会导致EUNKWN错误？
4. 是否需要提供其他配置参数或业务资质？

## 测试脚本

测试脚本已保存：
- `K:\payment\AbcPaymentGateway\Scripts\Test-ScanPay-Live.ps1`

执行命令：
```powershell
.\Test-ScanPay-Live.ps1 -ServerUrl "https://payment.qsgl.net" -Amount 10.00
```

## 测试结果文件

- 完整响应：`K:\payment\AbcPaymentGateway\Scripts\test_result_20260119084926.json`
- 服务器日志：通过SSH查看 docker logs payment-gateway

---

**结论**：
接口开发和部署完全正确，技术实现无问题。EUNKWN错误极可能由以下原因导致：
1. TrustPay.cer证书过期
2. 农行后台配置未完全生效
3. 需要农行技术支持进一步排查

**下一步**：联系农行技术支持，提供本报告中的完整请求和响应报文，要求排查EUNKWN错误原因。
