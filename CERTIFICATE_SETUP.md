# 农行商户证书配置说明

## 证书信息

### 商户证书
- **文件路径**: `K:\payment\AbcPaymentGateway\cert\103881636900016.pfx`
- **证书密码**: `ay365365`
- **商户ID**: `103881636900016`
- **用途**: 用于支付宝支付和微信支付的请求签名

### TrustPay平台证书
- **文件路径**: `K:\payment\AbcPaymentGateway\cert\prod\TrustPay.cer`
- **用途**: 用于验证农行支付平台返回数据的签名

## 配置文件

### appsettings.json 配置
```json
{
  "AbcPayment": {
    "MerchantIds": [
      "103881636900016"
    ],
    "CertificatePaths": [
      "./cert/103881636900016.pfx"
    ],
    "CertificatePasswords": [
      "ay365365"
    ],
    "TrustPayCertPath": "./cert/prod/TrustPay.cer",
    "PrintLog": true,
    "LogPath": "./logs",
    "Timeout": 30000,
    "ServerName": "pay.abchina.com",
    "ServerPort": "443",
    "ConnectMethod": "https",
    "TrxUrlPath": "/ebus/ReceiveMerchantTrxReqServlet",
    "MerchantErrorUrl": "https://payment.qsgl.net/error",
    "IsTestEnvironment": false
  }
}
```

## 证书管理服务

### AbcCertificateService
新增的证书管理服务，负责：

1. **证书加载**
   - 启动时自动加载商户证书（PFX格式）
   - 加载TrustPay平台证书（CER格式）
   - 验证证书有效期
   - 记录证书信息到日志

2. **签名功能**
   - 使用商户证书私钥对数据进行RSA-SHA256签名
   - 用于支付宝支付请求签名
   - 用于微信支付请求签名

3. **验签功能**
   - 使用TrustPay证书公钥验证农行返回数据的签名
   - 确保返回数据的真实性和完整性

### 服务注册
在 `Program.cs` 中注册为单例服务：
```csharp
builder.Services.Configure<AbcPaymentConfig>(
    builder.Configuration.GetSection("AbcPayment")
);

builder.Services.AddSingleton<IAbcCertificateService, AbcCertificateService>();
```

## 证书使用示例

### 1. 支付宝支付中使用证书
在 `AlipayController.cs` 中：
```csharp
// 构建支付宝请求数据
var requestData = BuildAlipayRequestData(request);
var dataBytes = System.Text.Encoding.UTF8.GetBytes(requestData);

// 使用商户证书签名
var signature = _certificateService.SignData(dataBytes);
var signatureBase64 = Convert.ToBase64String(signature);

_logger.LogDebug("支付宝请求签名完成，使用商户证书: 103881636900016.pfx");
```

### 2. 微信支付中使用证书
在 `PaymentController.cs` 中：
```csharp
// 构建微信签名数据
var signData = $"appId={appId}&nonceStr={nonceStr}&package={package}&signType={signType}&timeStamp={timeStamp}";
var dataBytes = System.Text.Encoding.UTF8.GetBytes(signData);

// 使用商户证书签名
var signatureBytes = _certificateService.SignData(dataBytes);
var paySign = Convert.ToBase64String(signatureBytes);

_logger.LogDebug("微信支付签名完成，使用商户证书: 103881636900016.pfx");
```

### 3. 验证农行返回数据签名
在 `AbcPaymentService.cs` 中：
```csharp
// 验证农行返回的签名
var responseData = GetResponseData(response);
var signature = GetSignatureFromResponse(response);

var isValid = _certificateService.VerifySignature(responseData, signature);
if (!isValid)
{
    _logger.LogWarning("农行返回数据签名验证失败");
    throw new InvalidOperationException("支付结果签名验证失败");
}
```

## 证书部署

### Docker 容器部署
证书文件已通过 Dockerfile 复制到容器：
```dockerfile
# 复制证书文件
RUN mkdir -p /app/cert/prod /app/cert/test
COPY cert/ /app/cert/
```

### 证书文件结构
```
cert/
├── 103881636900016.pfx          # 商户证书（密码: ay365365）
├── prod/
│   ├── TrustPay.cer             # 生产环境平台证书
│   └── abc.truststore           # Java truststore（如需要）
└── test/
    ├── 103881636900016.pfx      # 测试环境商户证书
    ├── TrustPay.cer             # 测试环境平台证书
    └── ...                      # 其他测试证书
```

## 安全注意事项

### 1. 证书密码保护
- ✅ 证书密码已配置在 `appsettings.json`
- ⚠️ 生产环境建议使用环境变量或密钥管理服务
- ⚠️ 不要将证书密码提交到公共代码仓库

### 2. 证书权限
- 证书文件权限应设置为仅应用程序可读
- Docker 容器中证书路径：`/app/cert/`

### 3. 证书有效期
- 服务启动时自动检查证书有效期
- 证书过期会记录警告日志
- 建议提前30天更新证书

### 4. 证书备份
- 定期备份商户证书文件
- 保存证书密码到安全位置
- 记录证书更新历史

## 日志示例

### 启动日志
```
[2026-01-13 16:45:00] [INFO] 加载商户证书: /app/cert/103881636900016.pfx
[2026-01-13 16:45:00] [INFO] 商户证书加载成功 - 主题: CN=103881636900016, 序列号: XXXX, 有效期至: 2027-12-31
[2026-01-13 16:45:00] [INFO] 加载TrustPay证书: /app/cert/prod/TrustPay.cer
[2026-01-13 16:45:00] [INFO] TrustPay证书加载成功 - 主题: CN=ABC TrustPay, 有效期至: 2028-12-31
```

### 签名日志
```
[2026-01-13 16:45:10] [DEBUG] 支付宝请求签名完成，使用商户证书: 103881636900016.pfx
[2026-01-13 16:45:10] [DEBUG] 数据签名成功，签名长度: 256 字节
```

### 验签日志
```
[2026-01-13 16:45:12] [DEBUG] 签名验证结果: True
```

## 故障排查

### 证书加载失败
**错误**: "商户证书文件不存在: /app/cert/103881636900016.pfx"
**解决**: 
1. 检查证书文件是否存在
2. 检查 Dockerfile 是否正确复制证书
3. 检查路径配置是否正确

### 密码错误
**错误**: "加载商户证书失败: The specified network password is not correct"
**解决**:
1. 验证证书密码是否正确（当前密码: ay365365）
2. 检查 appsettings.json 配置

### 签名失败
**错误**: "证书没有私钥，无法签名"
**解决**:
1. 确认使用的是 PFX 格式证书（包含私钥）
2. 检查证书导出时是否包含私钥
3. 验证证书权限

### 验签失败
**错误**: "TrustPay证书未加载，无法验证签名"
**解决**:
1. 检查 TrustPayCertPath 配置
2. 确认 TrustPay.cer 文件存在
3. 检查证书格式（应为 CER/DER 或 PEM 格式）

## 相关文件

- **证书服务**: `Services/AbcCertificateService.cs`
- **配置模型**: `Models/AbcPaymentConfig.cs`
- **支付服务**: `Services/AbcPaymentService.cs`
- **支付宝控制器**: `Controllers/AlipayController.cs`
- **微信支付控制器**: `Controllers/PaymentController.cs`
- **配置文件**: `appsettings.json`

## 下一步工作

1. **集成农行SDK**
   - 根据农行API文档实现完整的签名算法
   - 实现支付请求的数据加密（如需要）
   - 实现回调数据的验签逻辑

2. **测试验证**
   - 使用测试环境证书测试支付流程
   - 验证签名和验签功能
   - 测试证书过期场景

3. **监控告警**
   - 添加证书即将过期的告警
   - 监控签名失败率
   - 记录证书使用统计

---

**最后更新**: 2026-01-13  
**证书配置人**: GitHub Copilot  
**证书有效期**: 需要确认实际证书的有效期
