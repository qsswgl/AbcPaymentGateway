# 农行支付接口集成成功记录

## 📅 日期
2026年1月14日

## ✅ 成功突破

### 1. SSL 连接问题 - **已解决**
**问题**: `error:0A000152: SSL routines::unsafe legacy renegotiation disabled`

**解决方案**:
```dockerfile
# Dockerfile 添加 OpenSSL 配置
ENV OPENSSL_CONF=/etc/ssl/openssl-custom.cnf
RUN echo -e 'openssl_conf = openssl_init\n[openssl_init]\nssl_conf = ssl_sect\n\n[ssl_sect]\nsystem_default = system_default_sect\n\n[system_default_sect]\nOptions = UnsafeLegacyRenegotiation' > /etc/ssl/openssl-custom.cnf
```

```csharp
// Program.cs 配置客户端证书
handler.ClientCertificates.Add(certificate);
handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
```

### 2. GB18030 编码问题 - **已解决**
**问题**: `'GB18030' is not a supported encoding name`

**解决方案**:
```xml
<!-- AbcPaymentGateway.csproj -->
<PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.0" />
```

```csharp
// Program.cs
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```

### 3. 响应解析问题 - **已解决**
**问题**: 农行返回 V3.0.0 嵌套 JSON 格式无法解析

**农行响应格式**:
```json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",
      "Format": "JSON",
      "TrxResponse": {
        "ReturnCode": "0000",
        "ErrorMessage": "..."
      }
    }
  }
}
```

**解决方案**:
```csharp
if (root.TryGetProperty("MSG", out var msgElement) &&
    msgElement.TryGetProperty("Message", out var messageElement) &&
    messageElement.TryGetProperty("TrxResponse", out var trxResponse))
{
    // 解析 ReturnCode, ErrorMessage 等
}
```

### 4. 请求格式问题 - **已解决** 🎉

**错误演进历史**:
1. ❌ HTTP 302 重定向 → 发送了 JSON 格式，农行不接受
2. ❌ AP5324 "请求要素不存在，TrxType" → 扁平表单缺少嵌套结构
3. ✅ 2308 "商户无可用的支付方式" → **格式正确，业务配置问题**

**最终正确格式**:

```csharp
// 构建嵌套的请求结构
var message = new Dictionary<string, object>
{
    ["Version"] = "V3.0.0",
    ["Format"] = "JSON",
    ["Merchant"] = new Dictionary<string, string>
    {
        ["ECMerchantType"] = "EBUS",
        ["MerchantID"] = "103881636900016"
    },
    ["TrxRequest"] = new Dictionary<string, object>
    {
        ["TrxType"] = "EWalletPayReq",
        ["PaymentType"] = "D",
        ["PaymentLinkType"] = "2",
        ["OrderNo"] = "TEST20260114005",
        ["OrderAmount"] = "0.01",
        // ...
    }
};

var msg = new Dictionary<string, object>
{
    ["Message"] = message
};

// 序列化为JSON字符串
var jsonString = JsonSerializer.Serialize(msg);

// 使用 GB18030 编码发送
var encoding = Encoding.GetEncoding("GB18030");
var bytes = encoding.GetBytes(jsonString);
var content = new ByteArrayContent(bytes);
content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
{
    CharSet = "GB18030"
};
```

**发送的实际内容**:
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
      "TrxType": "EWalletPayReq",
      "PaymentType": "D",
      "PaymentLinkType": "2",
      "OrderNo": "TEST20260114005",
      "OrderAmount": "0.01",
      "OrderTime": "20260114090620",
      "NotifyType": "1",
      "OrderDesc": "测试订单",
      "ProductName": "测试商品",
      "ClientIP": "127.0.0.1"
    }
  }
}
```

**Content-Type**: `application/json; charset=GB18030`

## 📊 当前状态

### 成功指标
- ✅ SSL 双向认证通过
- ✅ 请求格式正确（农行成功解析）
- ✅ HTTP 200 响应
- ✅ 农行返回业务错误码（2308）

### 待解决问题
- ❌ **错误码 2308**: "商户无可用的支付方式，PMMNo=EP226"
  - **原因**: 商户可能未开通电子钱包支付功能
  - **解决**: 联系农行开通微信支付权限
  
- ⏳ **签名逻辑未实现**
  - 当前请求未包含数字签名
  - 可能导致农行拒绝交易
  - 需要根据农行SDK实现签名算法

## 🔧 下一步操作

### 1. 联系农行开通支付方式
- 确认商户号 `103881636900016` 是否已开通微信支付
- 确认 PMMNo=EP226 的具体含义
- 可能需要签订微信支付协议

### 2. 实现签名逻辑
参考农行 SDK 中的签名实现：
- 将请求参数按键排序
- 拼接为字符串
- 使用商户证书私钥签名
- 添加 Signature 字段到请求中

### 3. 测试其他接口
- 支付宝支付
- 扫码支付
- 订单查询
- 退款

## 📝 技术总结

### 关键发现
1. **农行使用 V3.0.0 嵌套 JSON 格式**，而非简单的键值对
2. **必须使用 GB18030 编码**，UTF-8 不被接受
3. **需要包含 Version, Format, Merchant 等元数据**
4. **TrxRequest 包含实际的交易数据**
5. **SSL 必须启用旧版重新协商（UnsafeLegacyRenegotiation）**

### 核心参数
- `TrxType`: "EWalletPayReq" （电子钱包支付请求）
- `PaymentType`: "D" （电子钱包，包括微信/支付宝）
- `PaymentLinkType`: "2" （被扫模式）
- `NotifyType`: "1" （异步通知）

### 文件修改清单
1. ✅ `Dockerfile` - 添加 OpenSSL 配置
2. ✅ `Program.cs` - 配置证书、SSL协议、GB18030
3. ✅ `Services/AbcPaymentService.cs` - 实现V3.0.0格式
4. ✅ `AbcPaymentGateway.csproj` - 添加编码包
5. ✅ `SSL_CONNECTION_FIX.md` - SSL问题文档

## 🎯 成功案例

### 测试请求
```json
POST https://payment.qsgl.net/api/payment/wechat
Content-Type: application/json; charset=utf-8

{
  "TrxType": "WeChatAppPayReq",
  "OrderNo": "TEST20260114005",
  "OrderAmount": "0.01",
  "ProductName": "测试商品",
  "OrderDesc": "测试订单",
  "ClientIP": "127.0.0.1"
}
```

### 农行响应
```json
{
  "appId": "",
  "timeStamp": "",
  "nonceStr": "",
  "package": "",
  "signType": "MD5",
  "paySign": "",
  "orderNo": "TEST20260114005",
  "trxId": null,
  "isSuccess": false,
  "errorMessage": "商户无可用的支付方式，PMMNo=EP226",
  "errorCode": "2308",
  "amount": null,
  "goodsDescription": null
}
```

**说明**: 虽然返回业务错误，但说明格式完全正确！

## 🏆 里程碑
- ✅ **2026-01-14 09:00** - SSL 连接成功
- ✅ **2026-01-14 09:03** - 响应解析成功
- ✅ **2026-01-14 09:06** - **格式验证成功！农行成功解析请求！**

---

**结论**: 技术集成已完成，剩余为商户配置和签名实现。
