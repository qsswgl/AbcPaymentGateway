# 农行NET Demo 最新测试配置确认报告

## 📅 报告日期
2026年1月21日

---

## ✅ Demo最后一次成功测试记录

### 测试时间
**2026年1月21日 09:20:19** (今天早上)

### 测试结果
```json
{
  "ReturnCode": "0000",
  "ErrorMessage": "交易成功",
  "TrxType": "PayReq",
  "OrderNo": "No639045836597100869",
  "OrderAmount": "0.01",
  "PaymentURL": "https://pay.abchina.com/EbusPerbankFront/PaymentModeNewAct?TOKEN=17689868991268660925"
}
```

**状态**: ✅ **成功生成PaymentURL**

---

## 🔧 Demo使用的完整配置

### 1️⃣ 商户信息
```
商户号: 103881636900016
商户类型: EBUS
```

### 2️⃣ 环境配置
```
环境类型: 正式生产环境 (Production)
服务器地址: pay.abchina.com
服务器端口: 443
连接方式: HTTPS
请求路径: /ebus/ReceiveMerchantTrxReqServlet
```

**关键发现**: ⚠️ **Demo使用的是正式生产环境，不是测试环境！**

### 3️⃣ 证书配置

#### 商户证书
```
证书文件: K:\payment\综合收银台接口包NET版\cert\prod\103881636900016.pfx
证书密码: ay365365
证书位置: prod目录（生产环境）
证书更新时间: 2026/1/5 11:02:50
```

#### 农行证书
```
农行公钥证书: K:\payment\综合收银台接口包NET版\cert\prod\TrustPay.cer
根证书: K:\payment\综合收银台接口包NET版\cert\prod\abc.truststore
根证书密码: changeit
```

### 4️⃣ 签名配置
```
签名算法: SHA1withRSA
编码方式: GB2312 (Demo配置)
消息格式: JSON (V3.0.0)
```

### 5️⃣ 支付参数
```json
{
  "TrxType": "PayReq",
  "PaymentType": "1",
  "PaymentLinkType": "1",
  "NotifyType": "0",
  "ResultNotifyURL": "http://127.0.0.1/Merchant/MerchantResult.aspx",
  "Order": {
    "PayTypeID": "PreAuthPay",
    "OrderAmount": "0.01",
    "CurrencyCode": "156",
    "ExpiredDate": "30",
    "OrderDate": "2026/01/21",
    "OrderTime": "09:01:19",
    "CommodityType": "0202"
  }
}
```

---

## 📊 Demo配置 vs 当前项目配置对比

| 配置项 | Demo配置 | 当前项目配置 | 状态 |
|--------|---------|-------------|------|
| **商户号** | 103881636900016 | 103881636900016 | ✅ 一致 |
| **环境** | pay.abchina.com (生产) | pay.abchina.com (生产) | ✅ 一致 |
| **证书文件** | 103881636900016.pfx | 103881636900016.pfx | ✅ 一致 |
| **证书密码** | ay365365 | ay365365 | ✅ 一致 |
| **签名算法** | SHA1withRSA | SHA1withRSA | ✅ 一致 |
| **编码** | GB2312 | UTF-8 | ⚠️ **不同** |
| **PaymentType** | "1" | "A" | ⚠️ **不同** |
| **消息格式** | V3.0.0 JSON | V3.0.0 JSON | ✅ 一致 |

---

## 🔍 关键差异分析

### 差异1: 编码方式
- **Demo**: GB2312
- **当前项目**: UTF-8

**影响**: 
- Demo的Web.config配置为GB2312
- 但实际Demo代码可能内部使用UTF-8签名

**建议**: 需要确认Demo实际使用的签名编码

### 差异2: PaymentType
- **Demo**: "1"
- **当前项目**: "A"

**影响**: 
- Demo成功使用的是PaymentType="1"
- 我们之前改成了"A"（参考旧版Demo）

**建议**: 应该改回"1"与最新Demo保持一致

---

## 📝 Demo完整请求报文

### 签名前报文
```json
{
  "Message": {
    "Version": "V3.0.0",
    "Format": "JSON",
    "Merchant": {
      "ECMerchantType": "EBUS",
      "MerchantID": "103881636900016"
    },
    "TrxRequest": {
      "TrxType": "PayReq",
      "PaymentType": "1",
      "PaymentLinkType": "1",
      "ReceiveAccount": "",
      "ReceiveAccName": "",
      "NotifyType": "0",
      "ResultNotifyURL": "http://127.0.0.1/Merchant/MerchantResult.aspx",
      "MerchantRemarks": "",
      "OrderFrom": "",
      "ReceiveMark": "",
      "ReceiveMerchantType": "",
      "IsBreakAccount": "0",
      "SplitAccTemplate": "",
      "VerifyFlag": "0",
      "VerifyType": "",
      "VerifyNo": "",
      "Order": {
        "PayTypeID": "PreAuthPay",
        "OrderNo": "No639045836597100869",
        "ExpiredDate": "30",
        "OrderAmount": "0.01",
        "SubsidyAmount": "",
        "Fee": "",
        "AccountNo": "",
        "CurrencyCode": "156",
        "ReceiverAddress": "北京",
        "InstallmentMark": "0",
        "BuyIP": "",
        "OrderDesc": "Game Card Order",
        "OrderURL": "http://127.0.0.1/Merchant/MerchantQueryOrder.aspx?ON=ON200412230001&DetailQuery=1",
        "OrderDate": "2026/01/21",
        "OrderTime": "09:01:19",
        "orderTimeoutDate": "20260121104901",
        "CommodityType": "0202",
        "OrderItems": [
          {
            "SubMerName": "网上自动商户1",
            "SubMerId": "12345",
            "SubMerMCC": "0000",
            "SubMerchantRemarks": "备注",
            "ProductID": "IP000001",
            "ProductName": "中国移动IP卡",
            "UnitPrice": "1.00",
            "Qty": "1",
            "ProductRemarks": "测试商品",
            "ProductType": "充值卡",
            "ProductDiscount": "0.9",
            "ProductExpiredDate": "10"
          }
        ]
      }
    }
  },
  "Signature-Algorithm": "SHA1withRSA",
  "Signature": "JypU56QRf/JQ21brPPi78wv8FoGJS8Lvonwf+FwT2vcV81SwfSDZChMZaaOSCGuCkwHbSP9LA661uxHQOIlMG3G0IE7m4X7pqKc/8WdFxD+zQAWsnPSAVX1u7Mku0YRDwtiSkc3SL5e4iucaeiBMM8R3Jgb9PzrdpLDhnIE9I755lrYrj+H7vbOoUxKeCUFa0l+MewcOJBWB/LS7BDISX5jUnbWZhPis35TBrkC9Tuc+jTIf7WEs2P6WiuegYQcwvpGZ6ytEUHOUif0C7RO2kzO5I+JpVhemACTOd9wt7HL38mCJFfqvkmAGXWnIs8kPFEKc6CNM+OagLaciUmLyaA=="
}
```

### 农行响应报文
```json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",
      "Format": "JSON",
      "Common": {
        "Channel": ""
      },
      "Merchant": {
        "ECMerchantType": "EBUS",
        "MerchantID": "103881636900016"
      },
      "ReturnCode": "0000",
      "ErrorMessage": "交易成功",
      "TrxType": "PayReq",
      "OrderNo": "No639045836597100869",
      "OrderAmount": "0.01",
      "PaymentURL": "https://pay.abchina.com/EbusPerbankFront/PaymentModeNewAct?TOKEN=17689868991268660925",
      "OneQRForAll": ""
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "XdlqwOQsdPW7j0Z0jO3w8mCF0BADU2sL2Y8farf1LQ2WgOxmAi4mNiPDBIX7D2eN/dRllxfqE8O/hCm7v9zvGC3K7sPKKH1IidvDU9KAMMIVu8NiI8fQJi3073bJMnjX1ulviblSUUGFDZ34uyJBC0Rt2YIj3yZrI4RC016shKo="
  }
}
```

---

## 🎯 重要发现

### ✅ 证书确认
1. **Demo确实成功使用了商户号 103881636900016**
2. **Demo使用的是正式生产环境 pay.abchina.com**
3. **Demo使用的证书就是 103881636900016.pfx (ay365365)**
4. **证书位于 prod 目录，不是 test 目录**

### ⚠️ 与农行反馈矛盾
农行技术支持说：
> "证书配置有误，请检查商户证书"

但Demo证明：
- ✅ 同样的商户号
- ✅ 同样的证书
- ✅ 同样的环境
- ✅ **今天早上还能成功生成PaymentURL！**

### 🔍 可能的原因

1. **证书最近被修改或重置**
   - Demo证书更新时间: 2026/1/5 11:02:50
   - 我们的测试时间: 2026/1/21
   - 可能农行在这期间修改了证书配置

2. **Demo环境与部署环境不同**
   - Demo: 本地运行 (localhost)
   - 部署: 远程服务器
   - 可能存在网络或服务器配置差异

3. **请求格式细微差异**
   - PaymentType: Demo用"1"，我们改成了"A"
   - 编码方式: Demo配置GB2312，我们用UTF-8

---

## 📋 建议的修正步骤

### 🔧 步骤1: 对齐PaymentType
将当前项目的 `PaymentType` 从 "A" 改回 "1"，与Demo完全一致。

```csharp
// AbcPaymentService.cs 第1242行左右
PaymentType = "1",  // 改回与Demo一致
```

### 🔧 步骤2: 验证编码方式
确认Demo实际使用的签名编码（可能需要反编译DLL确认）。

### 🔧 步骤3: 本地测试
1. 将项目配置完全对齐Demo
2. 在本地运行测试（localhost）
3. 确认能否成功生成PaymentURL

### 🔧 步骤4: 联系农行
如果本地测试成功但服务器失败，说明问题在：
- 服务器IP白名单
- 服务器证书配置
- 网络连接问题

---

## 📞 提供给农行的信息

### 证据材料
1. **Demo成功日志**: 2026/01/21 09:20:19 成功生成PaymentURL
2. **使用配置**: 商户103881636900016 + 证书103881636900016.pfx + 生产环境
3. **返回码**: 0000 (交易成功)

### 质询问题
1. Demo今天早上还能成功，为什么部署后返回2302？
2. 是否需要单独配置服务器环境或IP白名单？
3. 证书是否需要在不同环境（本地vs服务器）分别配置？

---

## 🎉 结论

### ✅ 确认信息
- **商户号**: 103881636900016
- **证书**: 103881636900016.pfx (密码: ay365365)
- **环境**: **正式生产环境** pay.abchina.com:443
- **最后成功时间**: 2026年1月21日 09:20:19 (今天早上)

### 🔑 关键发现
**Demo使用的是正式生产环境，不是测试环境！**

同样的配置，Demo能成功，说明：
1. ✅ 证书本身是有效的
2. ✅ 商户号是激活的
3. ✅ 签名算法是正确的
4. ❓ 问题可能在服务器部署环境

---

*报告生成时间: 2026年1月21日*
*基于: K:\payment\综合收银台接口包NET版\demo\log\abc_demo_test.log*
