# 农行支付 2302 错误排查 - UTF-8编码修复后

## 当前状态
- ✅ 签名算法：SHA1withRSA
- ✅ 签名编码：UTF-8
- ✅ 发送编码：UTF-8（已修复）
- ✅ 证书加载：成功
- ✅ 通信成功：能收到农行响应
- ❌ 错误代码：**2302 - 商户服务器证书配置有误**

## 2302错误的可能原因

根据Demo日志分析，2302错误 = "商户提交的解密和数字签名验证失败"

### 1. 签名数据不一致
**问题**：签名的数据和发送的数据不一致
- 签名对象：`{"Message":{...}}`
- 发送对象：整个MSG包括Signature

**检查点**：
- JSON序列化格式是否一致？
- 字段顺序是否影响？
- 空值字段处理？

### 2. 证书问题
**可能性**：
- ❓ 证书未在农行商户服务系统正确配置
- ❓ 证书序列号格式不对
- ❓ 证书主题DN格式不符合要求

**当前证书信息**：
```
Subject: O=ABC, OU=PaymentGateway, CN=EBUS.merchant.103881636900016.103881636900016.0000
SerialNumber: 7B97CA10275A16B1CEF3
Thumbprint: F59ABE04B450A0FECED81F9D68F4B1DDFF7AB973
NotAfter: 01/05/2031
```

### 3. 请求格式细节差异

**需要对比**：
1. JSON字段顺序
2. 数值格式 (1.00 vs "1.00")
3. 布尔值格式
4. 空字符串 vs null
5. 数组格式

### 4. HTTP请求差异

**检查点**：
- Content-Type header
- 其他HTTP headers
- POST body格式

## Demo成功案例分析

从 `logExample.log` 看到的成功请求：

```json
{
  "Message": {
    "Version": "V3.0.0",
    "Format": "JSON",
    "Merchant": {
      "ECMerchantType": "EBUS",
      "MerchantID": "103882200000958"
    },
    "TrxRequest": {
      "TrxType": "PayReq",
      "PaymentType": "A",  // ⚠️ Demo用"A"不是"1"
      "PaymentLinkType": "1",
      "ReceiveAccount": "",
      "ReceiveAccName": "",
      "NotifyType": "0",
      "ResultNotifyURL": "http://yourwebsite/appname/MerchantResult.jsp",
      "MerchantRemarks": "",
      "ReceiveMark": "",
      "ReceiveMerchantType": "",
      "IsBreakAccount": "0",
      "SplitAccTemplate": "",
      "Order": {
        "PayTypeID": "ImmediatePay",
        "OrderDate": "2014/09/23",
        "OrderTime": "11:55:30",
        "orderTimeoutDate": "20141019104901",
        "OrderNo": "ON20140924003",
        "CurrencyCode": "156",
        "OrderAmount": "2.00",
        "Fee": "",
        "AccountNo": "",
        "OrderDesc": "",
        "OrderURL": "",
        "ReceiverAddress": "����",
        "InstallmentMark": "0",
        "CommodityType": "0101",
        "BuyIP": "",
        "ExpiredDate": "30",
        "OrderItems": [...]
      }
    }
  },
  "Signature-Algorithm": "SHA1withRSA",
  "Signature": "..."
}
```

## 下一步调查

### 立即检查
1. ✅ 将PaymentType改回"A"（Demo用的）
2. ✅ 检查JSON序列化的字段顺序
3. ✅ 确认所有空字段都发送空字符串而非null
4. ✅ 验证数值都转换为字符串格式

### 如果仍失败
1. 联系农行确认证书是否真的已配置
2. 要求农行提供证书配置的具体要求
3. 检查是否需要特殊的证书属性或元数据

## 关键发现

**PaymentType字段**：
- 我们当前：`"1"`
- Demo使用：`"A"`

这可能是关键差异！Demo文档说明：
- 1 = 农行借记卡
- 3 = 农行贷记卡  
- **A = 支付方式合并**

让我们先改回"A"测试！
