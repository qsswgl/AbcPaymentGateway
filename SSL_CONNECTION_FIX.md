# 农行支付 SSL 连接问题完整解决方案

## 问题描述

调用农行支付接口时出现 SSL 连接错误：

```
The SSL connection could not be established, see inner exception.
error:0A000152: SSL routines::unsafe legacy renegotiation disabled
```

## 根本原因

1. **缺少客户端证书配置**：HttpClient 未附加商户证书进行双向 SSL 认证
2. **OpenSSL 安全策略**：OpenSSL 3.0+ 默认禁用了不安全的 SSL 重新协商
3. **编码不兼容**：农行响应使用 GB18030 编码，.NET 默认不支持
4. **参数错误**：微信支付需要特定的 PaymentType 和 PaymentLinkType

## 完整解决方案

### 1. 配置 HttpClient 使用客户端证书

**文件**: `Program.cs`

```csharp
using System.Text;
using System.Text.Json.Serialization;
using System.Diagnostics;
using AbcPaymentGateway.Services;
using AbcPaymentGateway.Models;

// 注册编码提供程序以支持 GB18030 等中文编码
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// 配置 HttpClient（使用客户端证书进行双向 SSL 认证）
builder.Services.AddHttpClient("AbcPayment", (serviceProvider, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
{
    var certificateService = serviceProvider.GetRequiredService<IAbcCertificateService>();
    var certificate = certificateService.GetMerchantCertificate();
    
    var handler = new HttpClientHandler();
    
    if (certificate != null)
    {
        handler.ClientCertificates.Add(certificate);
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        
        // 配置 SSL 协议（支持旧版协议以兼容农行服务器）
        handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 
                             | System.Security.Authentication.SslProtocols.Tls11 
                             | System.Security.Authentication.SslProtocols.Tls;
        
        // 添加证书验证回调
        handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                // TODO: 在生产环境中应该验证服务器证书
                return true;
            };
        
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("HttpClient 已配置客户端证书: {Subject}", certificate.Subject);
    }
    
    return handler;
});
```

### 2. 配置 OpenSSL 允许旧版 SSL 重新协商

**文件**: `Dockerfile`

在设置时区之后添加：

```dockerfile
# 配置 OpenSSL 以支持旧版 SSL 重新协商（兼容农行服务器）
# 参考: https://github.com/openssl/openssl/issues/17979
ENV OPENSSL_CONF=/etc/ssl/openssl-custom.cnf
RUN echo -e 'openssl_conf = openssl_init\n\
[openssl_init]\n\
ssl_conf = ssl_sect\n\
\n\
[ssl_sect]\n\
system_default = system_default_sect\n\
\n\
[system_default_sect]\n\
Options = UnsafeLegacyRenegotiation' > /etc/ssl/openssl-custom.cnf
```

### 3. 添加 GB18030 编码支持

**文件**: `AbcPaymentGateway.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.1" />
  <PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.0" />
</ItemGroup>
```

**文件**: `Program.cs` (在最开始)

```csharp
using System.Text;

// 注册编码提供程序以支持 GB18030 等中文编码
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```

### 4. 修正微信支付参数

**农行微信支付必需参数**:

```json
{
  "PaymentType": "D",           // 固定值：D 表示电子钱包支付
  "PaymentLinkType": "2",       // 固定值：2 表示被扫模式
  "OrderNo": "订单号",
  "OrderAmount": "金额",
  "NotifyType": "1"             // 1表示异步通知
}
```

## 验证步骤

### 1. 检查证书加载

```bash
ssh root@server "docker logs payment-gateway | grep '证书加载成功'"
```

期待输出：
```
[INFO] 商户证书加载成功 - 主题: O=ABC, OU=PaymentGateway, CN=EBUS.merchant.103881636900016...
[INFO] HttpClient 已配置客户端证书: O=ABC, OU=PaymentGateway...
```

### 2. 测试 SSL 连接

```bash
curl -X POST "https://payment.qsgl.net/api/payment/wechat" \
  -H "Content-Type: application/json" \
  -d '{"OrderNo":"TEST001","OrderAmount":"0.01","MerchantId":"103881636900016","GoodsName":"测试","NotifyUrl":"https://test.com"}'
```

成功标志：
- HTTP 200 响应
- 日志显示 `收到农行响应 (HTTP OK)`
- 无 SSL 相关错误

### 3. 查看农行响应

```bash
ssh root@server "docker logs payment-gateway | grep '收到农行响应'"
```

示例响应：
```json
{
  "MSG": {
    "Message": {
      "TrxResponse": {
        "ReturnCode": "0000",
        "ErrorMessage": "成功"
      }
    }
  }
}
```

## 技术细节

### SSL 握手流程

1. 客户端发起 HTTPS 请求
2. 服务器返回证书请求 (TLS Client Certificate Request)
3. 客户端发送商户证书 (103881636900016.pfx)
4. 服务器验证证书
5. 双向 SSL 握手完成
6. 开始数据传输

### OpenSSL 配置说明

- **UnsafeLegacyRenegotiation**: 允许旧版 SSL/TLS 重新协商
- **OPENSSL_CONF**: 指定自定义 OpenSSL 配置文件路径
- **风险**: 此选项降低了安全性，仅用于兼容旧系统

### GB18030 编码

- GB18030 是中国国家标准字符集
- 农行系统使用此编码返回中文错误信息
- `CodePagesEncodingProvider` 提供此编码支持

## 常见错误及解决方案

### 错误 1: SSL_ERROR_SSL

**错误信息**: `error:0A000152: SSL routines::unsafe legacy renegotiation disabled`

**解决方案**: 添加 OpenSSL 配置 (见上文步骤 2)

### 错误 2: 编码异常

**错误信息**: `'GB18030' is not a supported encoding name`

**解决方案**: 注册 CodePagesEncodingProvider (见上文步骤 3)

### 错误 3: 证书未发送

**症状**: 服务器日志无 "HttpClient 已配置客户端证书" 信息

**解决方案**: 
1. 检查证书文件是否存在: `docker exec payment-gateway ls -la /app/cert/`
2. 检查证书服务是否正常: `docker logs payment-gateway | grep 证书加载`
3. 验证 HttpClient 配置代码

### 错误 4: APE001 枚举值不存在

**错误信息**: `枚举值不存在，请检查上送值是否合法，GatewayPayTrxType`

**原因**: 微信支付参数不正确

**解决方案**: 
- PaymentType 必须为 "D"
- PaymentLinkType 必须为 "2"

## 生产环境注意事项

### 1. 证书验证

当前代码中 `ServerCertificateCustomValidationCallback` 返回 true，接受所有证书。

**生产环境应改为**:

```csharp
handler.ServerCertificateCustomValidationCallback = 
    (httpRequestMessage, cert, cetChain, policyErrors) =>
    {
        if (policyErrors == System.Net.Security.SslPolicyErrors.None)
            return true;
            
        // 验证服务器证书
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("服务器证书验证失败: {Errors}", policyErrors);
        
        // 可以额外验证证书指纹、颁发者等
        return false;
    };
```

### 2. TLS 版本

代码启用了 TLS 1.0/1.1（已过时且有漏洞）：

```csharp
handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
```

**建议**: 联系农行升级服务器，仅使用 TLS 1.2+

### 3. OpenSSL 配置

`UnsafeLegacyRenegotiation` 降低了安全性。

**建议**: 与农行沟通升级 SSL/TLS 配置，移除此选项

### 4. 日志级别

生产环境应调整日志级别，避免记录敏感信息：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "System.Net.Http": "Warning"
    }
  }
}
```

## 参考资料

- [OpenSSL Issue #17979](https://github.com/openssl/openssl/issues/17979) - Legacy Renegotiation
- [.NET HttpClientHandler](https://learn.microsoft.com/dotnet/api/system.net.http.httpclienthandler)
- [GB18030 Encoding](https://en.wikipedia.org/wiki/GB_18030)
- 农行综合收银台接口文档 V3.3.3

---

**最后更新**: 2026-01-14  
**验证环境**: .NET 10.0, Alpine Linux, OpenSSL 3.x
