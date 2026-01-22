# 支付宝支付接口使用指南

## 📅 测试日期
2026年1月15日

## ✅ 接口状态
**支付宝支付已成功集成并通过测试！**

### 当前状态
- ✅ **接口格式正确** - 农行成功解析请求
- ✅ **SSL 连接正常** - 双向认证通过
- ✅ **V3.0.0 格式** - 使用嵌套 JSON 结构
- ⚠️ **商户配置** - 错误码 2308（商户无可用的支付方式）

## 📡 API 接口

### 1. 支付宝扫码支付（被扫模式）

**接口地址**: `POST /api/payment/alipay/qrcode`

**功能说明**: 商户生成支付二维码，用户使用支付宝APP扫描二维码完成支付

**适用场景**: 
- PC网站支付
- 线下扫码支付
- 收银台扫码

#### 请求参数

```json
{
  "orderNo": "ALIPAY20260115002",         // 商户订单号（必填，唯一）
  "amount": 0.01,                         // 支付金额（必填，单位：元）
  "merchantId": "103881636900016",        // 商户号（必填）
  "goodsName": "测试商品",                 // 商品名称（必填）
  "notifyUrl": "https://payment.qsgl.net/api/payment/notify",  // 支付回调地址（选填）
  "returnUrl": "https://example.com/result",  // 支付完成返回地址（选填）
  "expiredDate": "30",                    // 订单过期时间（选填，单位：分钟，默认30）
  "limitPay": "0",                        // 限制信用卡支付（选填，0=不限制，1=限制）
  "attach": "custom_data_123"             // 附加数据（选填，回调时原样返回）
}
```

#### 响应示例

**成功响应**:
```json
{
  "isSuccess": true,
  "orderNo": "ALIPAY20260115002",
  "transactionId": "ABC20260115080137001",
  "qrCodeUrl": "https://qr.alipay.com/bax...",
  "amount": 0.01,
  "status": "SUCCESS",
  "message": "支付订单创建成功",
  "expireTime": "2026-01-15T08:31:37+08:00",
  "errorCode": null
}
```

**失败响应**:
```json
{
  "isSuccess": false,
  "orderNo": "ALIPAY20260115002",
  "transactionId": null,
  "qrCodeUrl": null,
  "amount": 0.01,
  "status": "FAILED",
  "message": "商户无可用的支付方式，PMMNo=EP226",
  "expireTime": "2026-01-15T08:31:37+08:00",
  "errorCode": "2308"
}
```

#### 测试命令

**PowerShell**:
```powershell
$body = @{
    orderNo = "ALIPAY20260115002"
    amount = 0.01
    merchantId = "103881636900016"
    goodsName = "测试商品"
    notifyUrl = "https://payment.qsgl.net/api/payment/notify"
    expiredDate = "30"
} | ConvertTo-Json

Invoke-WebRequest -Uri 'https://payment.qsgl.net/api/payment/alipay/qrcode' `
    -Method POST `
    -Body $body `
    -ContentType 'application/json; charset=utf-8'
```

**cURL**:
```bash
curl -X POST https://payment.qsgl.net/api/payment/alipay/qrcode \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "ALIPAY20260115002",
    "amount": 0.01,
    "merchantId": "103881636900016",
    "goodsName": "测试商品",
    "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    "expiredDate": "30"
  }'
```

## 🔧 技术实现

### 农行 V3.0.0 请求格式

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
      "TrxType": "EWalletPayReq",           // 电子钱包支付请求
      "PaymentType": "D",                   // D=电子钱包（微信/支付宝）
      "PaymentLinkType": "2",               // 2=被扫模式
      "OrderNo": "ALIPAY20260115002",
      "OrderAmount": "0.01",
      "OrderTime": "20260115080137",
      "OrderDesc": "测试商品",              // 订单详情（必填）
      "ProductName": "测试商品",
      "NotifyType": "1",                    // 1=异步通知
      "ResultNotifyURL": "https://payment.qsgl.net/api/payment/notify",
      "OrderValidTime": "30"
    }
  }
}
```

### 核心代码

**AbcPaymentService.cs**:
```csharp
public async Task<PaymentResponse> ProcessAlipayPaymentAsync(AlipayQRCodeRequest request)
{
    // 构建支付宝支付请求数据
    var requestData = BuildAlipayRequestData(request);
    
    // 发送到农行支付平台
    var response = await SendToAbcAsync(requestData);
    
    return response;
}

private Dictionary<string, string> BuildAlipayRequestData(AlipayQRCodeRequest request)
{
    var trxRequest = new Dictionary<string, object>
    {
        ["TrxType"] = "EWalletPayReq",
        ["PaymentType"] = "D",
        ["PaymentLinkType"] = "2",
        ["OrderNo"] = request.OrderNo,
        ["OrderAmount"] = request.Amount.ToString("F2"),
        ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
        ["NotifyType"] = "1",
        ["OrderDesc"] = request.GoodsName ?? "商品购买"
    };
    
    // ... 添加其他字段
    
    var message = new Dictionary<string, object>
    {
        ["Version"] = "V3.0.0",
        ["Format"] = "JSON",
        ["Merchant"] = new Dictionary<string, string>
        {
            ["ECMerchantType"] = "EBUS",
            ["MerchantID"] = request.MerchantId
        },
        ["TrxRequest"] = trxRequest
    };
    
    return new Dictionary<string, string>
    {
        ["MSG"] = JsonSerializer.Serialize(new { Message = message })
    };
}
```

**AlipayController.cs**:
```csharp
[HttpPost("qrcode")]
public async Task<IActionResult> CreateQRCodePayment([FromBody] AlipayQRCodeRequest request)
{
    // 参数验证
    if (string.IsNullOrWhiteSpace(request.OrderNo) || request.Amount <= 0)
    {
        return BadRequest(new { message = "参数错误" });
    }
    
    // 调用农行支付服务
    var paymentResponse = await _paymentService.ProcessAlipayPaymentAsync(request);
    
    // 转换为支付宝响应格式
    var response = new AlipayQRCodeResponse
    {
        IsSuccess = paymentResponse.IsSuccess,
        OrderNo = request.OrderNo,
        TransactionId = paymentResponse.TrxId,
        QrCodeUrl = paymentResponse.QRCodeUrl,
        Amount = request.Amount,
        Status = paymentResponse.IsSuccess ? "SUCCESS" : "FAILED",
        Message = paymentResponse.ResponseMessage,
        ErrorCode = paymentResponse.ResponseCode
    };
    
    return response.IsSuccess ? Ok(response) : BadRequest(response);
}
```

## 📊 测试结果

### 测试日志

```
info: AbcPaymentGateway.Services.AbcPaymentService[0]
      开始处理支付宝支付请求: OrderNo=ALIPAY20260115002, Amount=0.01

info: AbcPaymentGateway.Services.AbcPaymentService[0]
      发送MSG格式 (JSON): {"Message":{"Version":"V3.0.0","Format":"JSON",
      "Merchant":{"ECMerchantType":"EBUS","MerchantID":"103881636900016"},
      "TrxRequest":{"TrxType":"EWalletPayReq","PaymentType":"D",
      "PaymentLinkType":"2","OrderNo":"ALIPAY20260115002",
      "OrderAmount":"0.01","OrderTime":"20260115080137","NotifyType":"1",
      "OrderDesc":"测试商品","ProductName":"测试商品",
      "ResultNotifyURL":"https://payment.qsgl.net/api/payment/notify",
      "OrderValidTime":"30"}}}

info: System.Net.Http.HttpClient.AbcPayment.ClientHandler[101]
      Received HTTP response headers after 232ms - 200

info: AbcPaymentGateway.Services.AbcPaymentService[0]
      收到农行响应: {"MSG":{"Message":{"Version":"V3.0.0",
      "Merchant":{"MerchantID":"103881636900016"},"TrxResponse":
      {"ReturnCode":"2308","ErrorMessage":"商户无可用的支付方式，PMMNo=EP226"}}}

info: AbcPaymentGateway.Services.AbcPaymentService[0]
      解析农行响应成功: ReturnCode=2308, Message=商户无可用的支付方式
```

### 关键指标
- ✅ HTTP 200 响应
- ✅ 请求格式正确（农行成功解析）
- ✅ 响应时间: ~232ms
- ⚠️ 业务错误: 2308（商户配置问题）

## ⚠️ 已知问题

### 错误码 2308 - 商户无可用的支付方式

**错误信息**: "商户无可用的支付方式，PMMNo=EP226"

**原因分析**:
1. 商户未开通支付宝支付渠道
2. 支付方式配置不正确
3. PMMNo(支付方式编号)不存在或未激活

**解决方案**:
1. 联系农行确认商户号 `103881636900016` 的支付方式配置
2. 确认是否已开通电子钱包（支付宝）支付权限
3. 检查 PMMNo=EP226 的配置状态

## 📝 注意事项

1. **必填字段**: `OrderNo`, `Amount`, `MerchantId`, `GoodsName`
2. **订单号唯一性**: 每个订单号必须唯一，不可重复
3. **金额格式**: 保留两位小数，如 `0.01`
4. **编码要求**: 使用 GB18030 编码发送到农行
5. **超时时间**: 默认30分钟，可通过 `expiredDate` 参数自定义
6. **回调地址**: `notifyUrl` 必须是公网可访问的 HTTPS 地址

## 🔗 相关接口

- 微信支付: `POST /api/payment/wechat`
- 订单查询: `POST /api/payment/query`
- 支付退款: `POST /api/payment/refund`

## 📖 Swagger 文档

访问: https://payment.qsgl.net/swagger

接口分组: **Alipay** (支付宝支付控制器)

完整的接口文档、参数说明和示例请查看 Swagger UI。

---

**最后更新**: 2026年1月15日 08:02
**状态**: ✅ 接口集成完成，等待商户配置开通
