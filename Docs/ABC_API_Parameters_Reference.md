# 农行支付接口入参/出参详细文档

**文档版本**: v1.0  
**生成时间**: 2026年1月19日  
**适用范围**: 提供给ABC银行技术支持  
**商户号**: 103881636900016

---

## 📋 目录

1. [页面支付接口 (PayReq)](#1-页面支付接口-payreq)
2. [一码多扫接口 (OLScanPayOrderReq)](#2-一码多扫接口-olscanpayorderreq)
3. [通用说明](#3-通用说明)
4. [错误码对照表](#4-错误码对照表)

---

## 1. 页面支付接口 (PayReq)

### 🔵 接口说明

**功能**: 创建页面支付订单，返回支付URL供用户在浏览器中完成支付  
**交易类型**: PayReq  
**请求URL**: https://pay.abchina.com:443/ebus/ReceiveMerchantTrxReqServlet  
**请求方法**: POST  
**数据格式**: JSON (MSG封装)

---

### 📤 入参详情

#### HTTP请求头
```
Content-Type: text/plain; charset=GB18030
Accept: */*
```

#### 请求体结构
```json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",              // 协议版本，固定值
      "Format": "JSON",                  // 数据格式，固定值
      "Merchant": {
        "ECMerchantType": "EBUS",        // 商户类型，固定值
        "MerchantID": "103881636900016"  // 商户号
      },
      "TrxRequest": {
        "TrxType": "PayReq",             // 交易类型：页面支付
        "Order": { ... },                // 订单信息，见下表
        "OrderDetail": [ ... ],          // 订单明细，见下表
        "PaymentType": "A",              // 支付方式
        "PaymentLinkType": "1",          // 支付渠道
        "NotifyType": "1",               // 通知方式
        "ResultNotifyURL": "...",        // 后台通知URL
        "MerchantSuccessURL": "...",     // 成功跳转URL
        "MerchantErrorURL": "...",       // 失败跳转URL
        "IsBreakAccount": "0"            // 是否分账
      }
    },
    "Signature-Algorithm": "SHA1withRSA", // 签名算法
    "Signature": "..."                    // 签名值
  }
}
```

#### Order 对象字段详情

| 字段名 | 类型 | 必填 | 说明 | 示例值 | 备注 |
|--------|------|------|------|--------|------|
| PayTypeID | String | 是 | 支付类型 | "ImmediatePay" | 固定值：即时支付 |
| OrderNo | String | 是 | 商户订单号 | "PAY20260119095001" | 商户系统唯一订单号，建议格式：前缀+日期时间+序号 |
| OrderAmount | String | 是 | 订单金额 | "10.00" | 格式：保留两位小数，单位：元 |
| OrderDate | String | 是 | 订单日期 | "2026/01/19" | 格式：YYYY/MM/DD |
| OrderTime | String | 是 | 订单时间 | "09:50:30" | 格式：HH:mm:ss |
| OrderDesc | String | 是 | 订单描述 | "测试商品" | 订单说明，用户可见 |
| CurrencyCode | String | 是 | 货币代码 | "156" | 156=人民币（固定值） |
| CommodityType | String | 是 | 商品类型 | "0201" | 0201=虚拟商品, 0101=实物商品 |
| InstallmentMark | String | 是 | 分期标识 | "0" | 0=不分期, 1=分期 |
| ExpiredDate | String | 是 | 订单有效期 | "30" | 单位：天，订单保存时间 |
| ReceiverAddress | String | 否 | 收货地址 | "福建省福州市..." | 实物商品时建议填写 |
| BuyIP | String | 否 | 买家IP | "123.123.123.123" | 用户下单时的IP地址 |
| AccountNo | String | 否 | 支付账号 | "" | 一般为空 |
| OrderURL | String | 否 | 订单URL | "" | 订单详情页URL |
| Fee | String | 否 | 手续费 | "0.00" | 手续费金额 |
| SubsidyAmount | String | 否 | 补贴金额 | "0.00" | 补贴金额 |

#### OrderDetail 数组字段详情

OrderDetail 是一个数组，包含订单中的商品明细：

```json
"OrderDetail": [
  {
    "ProductName": "测试商品",        // 商品名称（必填）
    "UnitPrice": "10.00",            // 单价（必填）
    "Qty": "1",                      // 数量（必填）
    "ProductRemarks": "测试商品购买"  // 商品备注（可选）
  }
]
```

| 字段名 | 类型 | 必填 | 说明 | 示例值 |
|--------|------|------|------|--------|
| ProductName | String | 是 | 商品名称 | "测试商品" |
| UnitPrice | String | 是 | 商品单价 | "10.00" |
| Qty | String | 是 | 商品数量 | "1" |
| ProductRemarks | String | 否 | 商品备注 | "测试商品购买" |

#### TrxRequest 其他字段详情

| 字段名 | 类型 | 必填 | 说明 | 可选值 | 当前使用值 |
|--------|------|------|------|--------|-----------|
| PaymentType | String | 是 | 支付方式 | 1=借记卡, 3=贷记卡, A=借贷记卡合并, 6=银联跨行 | "A" |
| PaymentLinkType | String | 是 | 支付渠道 | 1=电脑网络, 2=手机网络, 3=数字电视, 4=智能客户端 | "1" |
| NotifyType | String | 是 | 通知方式 | 0=仅页面通知, 1=页面+服务器通知 | "1" |
| ResultNotifyURL | String | 条件必填 | 后台通知URL | 当NotifyType=1时必填 | "https://payment.qsgl.net/api/payment/abc/notify" |
| MerchantSuccessURL | String | 是 | 成功跳转URL | 支付成功后的页面跳转地址 | "https://payment.qsgl.net/success" |
| MerchantErrorURL | String | 是 | 失败跳转URL | 支付失败后的页面跳转地址 | "https://payment.qsgl.net/error" |
| IsBreakAccount | String | 是 | 是否分账 | 0=不分账, 1=分账 | "0" |
| ReceiveAccount | String | ? | 收款账号 | 农行账号 | **待确认是否必填** |
| ReceiveAccName | String | ? | 收款户名 | 账户名称 | **待确认是否必填** |
| VerifyFlag | String | ? | 实名验证标识 | 0=不验证, 1=验证 | **待确认是否必填** |
| VerifyType | String | 否 | 证件类型 | 01=身份证, 02=护照等 | 当VerifyFlag=1时必填 |
| VerifyNo | String | 否 | 证件号码 | 证件号码 | 当VerifyFlag=1时必填 |

#### 完整入参示例

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
        "TrxType": "PayReq",
        "Order": {
          "PayTypeID": "ImmediatePay",
          "OrderNo": "PAY20260119095001",
          "OrderAmount": "10.00",
          "OrderDate": "2026/01/19",
          "OrderTime": "09:50:30",
          "OrderDesc": "测试商品-页面支付",
          "CurrencyCode": "156",
          "CommodityType": "0201",
          "InstallmentMark": "0",
          "ExpiredDate": "30"
        },
        "OrderDetail": [
          {
            "ProductName": "测试商品",
            "UnitPrice": "10.00",
            "Qty": "1",
            "ProductRemarks": "测试商品购买"
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
    "Signature": "ER0jRmvKC7QwI1eK7r2U0+ukEhA5j2SKVsl+vJrvXKaBOEwdPqTLK8uTLsC8m1AypUTpL7D7CCQSAS/5BPS0+cWTpuNVG93JqlhFSQ4kmDRmHdKaMkvmkXlimGzFOXZk5GYqzIjQVuHSTei+yNiLFUfyEWuQwXkpzBxQ2HGPTMLTZ4EnovJQgbAMvagwIMH/13jjD7zOhaQx2rAWQEPB/V5lYs7Zf0jx6x0kAEoN0hgdLHdzsgqp7fecpFbDC4jEok82IGOtdhzb7rtRT4WHeQHxSkVcRfq6ovfxBVrTQZ+RZmqWYhZDKIuvldziUO0DLgDyaJWNW55DE6uWgT+ekQ=="
  }
}
```

---

### 📥 出参详情

#### 成功响应示例（预期）

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
        "MerchantID": "103881636900016"
      },
      "TrxResponse": {
        "ReturnCode": "0000",
        "ErrorMessage": "交易成功",
        "PaymentURL": "https://pay.abchina.com/payment/redirect?token=xxxxx",
        "TrxID": "ABC202601190950001234567890",
        "OrderNo": "PAY20260119095001",
        "OrderAmount": "10.00",
        "TrxDate": "2026/01/19",
        "TrxTime": "09:50:30"
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "..."
  }
}
```

#### 当前实际响应（EUNKWN错误）

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
        "MerchantID": ""
      },
      "TrxResponse": {
        "ReturnCode": "EUNKWN",
        "ErrorMessage": "交易结果未知，请进行查证明确交易结果，No message available"
      }
    }
  }
}
```

#### TrxResponse 字段说明

| 字段名 | 类型 | 说明 | 示例值（成功） | 示例值（当前错误） |
|--------|------|------|---------------|-------------------|
| ReturnCode | String | 返回码 | "0000" | "EUNKWN" |
| ErrorMessage | String | 返回消息 | "交易成功" | "交易结果未知，请进行查证明确交易结果，No message available" |
| PaymentURL | String | 支付URL | "https://pay.abchina.com/payment/redirect?token=xxxxx" | **缺失** ⚠️ |
| TrxID | String | 农行交易流水号 | "ABC202601190950001234567890" | **缺失** ⚠️ |
| OrderNo | String | 商户订单号 | "PAY20260119095001" | **缺失** ⚠️ |
| OrderAmount | String | 订单金额 | "10.00" | **缺失** ⚠️ |
| TrxDate | String | 交易日期 | "2026/01/19" | **缺失** ⚠️ |
| TrxTime | String | 交易时间 | "09:50:30" | **缺失** ⚠️ |

---

## 2. 一码多扫接口 (OLScanPayOrderReq)

### 🔵 接口说明

**功能**: 创建一码多扫订单，返回二维码URL供用户扫码支付  
**交易类型**: OLScanPayOrderReq  
**请求URL**: https://pay.abchina.com:443/ebus/ReceiveMerchantTrxReqServlet  
**请求方法**: POST  
**数据格式**: JSON (MSG封装)

---

### 📤 入参详情

#### 请求体结构
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
        "TrxType": "OLScanPayOrderReq",  // 交易类型：一码多扫
        "Order": { ... },                 // 订单信息
        "OrderDetail": [ ... ],           // 订单明细
        "PaymentType": "A",               // 支付方式
        "PaymentLinkType": "1",           // 支付渠道
        "NotifyType": "1",                // 通知方式
        "ResultNotifyURL": "...",         // 后台通知URL
        "IsBreakAccount": "0"             // 是否分账
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "..."
  }
}
```

#### Order 对象字段（与PayReq基本相同）

| 字段名 | 类型 | 必填 | 说明 | 示例值 |
|--------|------|------|------|--------|
| PayTypeID | String | 是 | 支付类型 | "ImmediatePay" |
| OrderNo | String | 是 | 商户订单号 | "SCAN20260119095001" |
| OrderAmount | String | 是 | 订单金额 | "10.00" |
| OrderDate | String | 是 | 订单日期 | "2026/01/19" |
| OrderTime | String | 是 | 订单时间 | "09:50:30" |
| OrderDesc | String | 是 | 订单描述 | "测试商品-扫码支付" |
| CurrencyCode | String | 是 | 货币代码 | "156" |
| CommodityType | String | 是 | 商品类型 | "0201" |
| InstallmentMark | String | 是 | 分期标识 | "0" |
| ExpiredDate | String | 是 | 订单有效期 | "30" |

#### TrxRequest 字段差异

与 PayReq 的主要区别：
- ❌ **不需要** MerchantSuccessURL（扫码支付无页面跳转）
- ❌ **不需要** MerchantErrorURL（扫码支付无页面跳转）
- ✅ **需要** ResultNotifyURL（后台通知必填）

#### 完整入参示例

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
          "OrderNo": "SCAN20260119095001",
          "OrderAmount": "10.00",
          "OrderDate": "2026/01/19",
          "OrderTime": "09:50:30",
          "OrderDesc": "测试商品-扫码支付",
          "CurrencyCode": "156",
          "CommodityType": "0201",
          "InstallmentMark": "0",
          "ExpiredDate": "30"
        },
        "OrderDetail": [
          {
            "ProductName": "测试商品",
            "UnitPrice": "10.00",
            "Qty": "1",
            "ProductRemarks": "测试商品购买"
          }
        ],
        "PaymentType": "A",
        "PaymentLinkType": "1",
        "NotifyType": "1",
        "ResultNotifyURL": "https://payment.qsgl.net/api/payment/abc/notify",
        "IsBreakAccount": "0"
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "..."
  }
}
```

---

### 📥 出参详情

#### 成功响应示例（预期）

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
        "MerchantID": "103881636900016"
      },
      "TrxResponse": {
        "ReturnCode": "0000",
        "ErrorMessage": "交易成功",
        "QRCode": "https://qr.abchina.com/scan?code=xxxxx",
        "TrxID": "ABC202601190950001234567890",
        "OrderNo": "SCAN20260119095001",
        "OrderAmount": "10.00",
        "TrxDate": "2026/01/19",
        "TrxTime": "09:50:30"
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "..."
  }
}
```

#### 当前实际响应（EUNKWN错误）

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
        "MerchantID": ""
      },
      "TrxResponse": {
        "ReturnCode": "EUNKWN",
        "ErrorMessage": "交易结果未知，请进行查证明确交易结果，No message available"
      }
    }
  }
}
```

---

## 3. 通用说明

### 签名算法

**算法**: SHA1withRSA  
**私钥**: 商户证书 103881636900016.pfx  
**验签公钥**: TrustPay.cer（**已过期，2023-08-11**）⚠️

#### 签名流程
1. 提取 Message 对象的 JSON 字符串
2. 使用 GB18030 编码转换为字节数组
3. 使用商户私钥进行 SHA1withRSA 签名
4. Base64 编码签名结果

#### 验签流程
1. 提取响应中的 Message 对象
2. 使用 TrustPay.cer 公钥验证 Signature
3. 验签通过后解析响应内容

### 字符编码

**统一编码**: GB18030  
**Content-Type**: text/plain; charset=GB18030

### 证书信息

#### 商户证书
- **文件**: 103881636900016.pfx
- **密码**: [已配置]
- **主题**: CN=EBUS.merchant.103881636900016.103881636900016.0000
- **序列号**: 7B97CA10275A16B1CEF3
- **有效期**: 2031-01-05 10:56:49 ✅

#### TrustPay证书
- **文件**: TrustPay.cer
- **主题**: CN=MainServer.0001
- **有效期**: 2023-08-11 13:38:49 ❌ **已过期**

---

## 4. 错误码对照表

| 错误码 | 说明 | 可能原因 | 建议处理 |
|--------|------|----------|----------|
| 0000 | 交易成功 | - | 正常业务流程 |
| APE001 | 系统错误 | 请求格式错误、枚举值不存在 | 检查 TrxType 和其他枚举字段 |
| APE002 | 商户信息不存在 | 商户号错误或未开通 | 检查商户号配置 |
| APE003 | 商户未开通此功能 | 权限未开通 | 联系ABC银行开通权限 |
| APE004 | 商户已停用 | 商户状态异常 | 联系ABC银行确认状态 |
| APE009 | 请求报文格式错误 | 缺少必填字段 | 检查必填字段完整性 |
| APE400 | 签名验证失败 | 证书错误或签名算法错误 | 检查证书配置和签名逻辑 |
| **EUNKWN** | **交易结果未知** | **配置问题或权限问题** | **当前问题，待ABC银行确认** |
| E001 | 订单不存在 | 查询的订单号不存在 | 检查订单号是否正确 |
| E002 | 订单已支付 | 重复支付 | 提示用户订单已支付 |
| E003 | 订单已关闭 | 订单已取消或过期 | 提示用户订单已关闭 |
| E004 | 订单已退款 | 订单已退款 | 提示用户订单已退款 |

---

## 5. 当前问题汇总

### 🔴 核心问题

**错误码**: EUNKWN  
**错误消息**: "交易结果未知，请进行查证明确交易结果，No message available"

### 📊 问题分析

✅ **已确认正常的部分**:
- HTTP 连接正常（200 OK）
- 签名生成正常（未返回 APE400）
- 商户识别正常（未返回 APE002）
- 请求格式正常（未返回 APE009）
- TrxType 已修正为 PayReq

⚠️ **待确认的部分**:
- PayReq 权限是否真正激活
- 是否缺少必填字段（ReceiveAccount, ReceiveAccName, VerifyFlag等）
- CommodityType "0201" 是否允许
- 回调URL是否需要预先登记
- TrustPay.cer 证书过期是否影响

### 🎯 需要ABC银行提供的信息

1. **完整的必填字段列表**（PayReq 和 OLScanPayOrderReq）
2. **EUNKWN 错误的具体原因**
3. **商户配置检查结果**（是否真正开通了 PayReq）
4. **示例请求报文**（成功的 PayReq 示例）
5. **更新的 TrustPay.cer 证书**

---

## 📞 联系方式

**商户名称**: 七匹狼资产管理  
**商户号**: 103881636900016  
**技术联系**: support@qsgl.net  
**测试服务器**: https://payment.qsgl.net

**期望响应时间**: 1-2个工作日  
**紧急程度**: 高（已完成开发，待上线）

---

**文档生成**: 2026年1月19日  
**文档版本**: v1.0  
**最后更新**: 2026年1月19日
