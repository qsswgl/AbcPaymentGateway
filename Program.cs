using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Text;
using AbcPaymentGateway.Services;
using AbcPaymentGateway.Models;
using AbcPaymentGateway.Logging;

// 注册编码提供程序以支持 GB18030 等中文编码
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// 🔧 启用 OpenSSL legacy renegotiation（必须在程序最开始设置）
// 解决错误: error:0A000152:SSL routines::unsafe legacy renegotiation disabled
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false); // 回退到旧的HttpClientHandler

var builder = WebApplication.CreateBuilder(args);

// 🔍 配置日志：同时输出到 Console 和文件
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 📁 添加文件日志
var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);
var logFilePath = Path.Combine(logDirectory, $"payment_{DateTime.Now:yyyyMMdd}.log");

// 创建简单的文件日志记录器
builder.Logging.AddProvider(new FileLoggerProvider(logFilePath));
builder.Logging.SetMinimumLevel(LogLevel.Information);

// 添加控制器支持
builder.Services.AddControllers();

// 配置农行支付配置
builder.Services.Configure<AbcPaymentConfig>(
    builder.Configuration.GetSection("AbcPayment")
);

// 添加证书管理服务（单例，启动时加载证书）
builder.Services.AddSingleton<IAbcCertificateService, AbcCertificateService>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AbcPaymentConfig>>();
    var logger = serviceProvider.GetRequiredService<ILogger<AbcCertificateService>>();
    var certService = new AbcCertificateService(config, logger);
    
    // 立即加载truststore根证书到系统受信任存储
    certService.LoadTrustStoreCertificates();
    logger.LogInformation("🔐 系统启动时已加载truststore根证书");
    
    return certService;
});

// 配置 HttpClient（使用客户端证书进行双向 SSL 认证）
builder.Services.AddHttpClient("AbcPayment", (serviceProvider, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
{
    var certificateService = serviceProvider.GetRequiredService<IAbcCertificateService>();
    var merchantCertificate = certificateService.GetMerchantCertificate();
    var trustPayCertificate = certificateService.GetTrustPayCertificate();
    
    var handler = new HttpClientHandler();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    // 🔑 重要：TrustPay.cer 是农行服务器的公钥证书，用于验证农行响应签名
    // 不应该添加到 ClientCertificates（客户端证书是用于双向SSL认证的）
    if (trustPayCertificate != null)
    {
        logger.LogInformation("📋 农行公钥证书 (TrustPay) 已加载 - 用于验签: {Subject}", trustPayCertificate.Subject);
    }
    else
    {
        logger.LogWarning("⚠️ 农行公钥证书 (TrustPay) 未加载");
    }
    
    // 🔑 只添加商户证书作为客户端证书（双向SSL认证）
    if (merchantCertificate != null)
    {
        handler.ClientCertificates.Add(merchantCertificate);
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        
        // 🔧 启用 OpenSSL legacy renegotiation（农行服务器需要）
        // 解决错误: error:0A000152:SSL routines::unsafe legacy renegotiation disabled
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.AllowLegacyRenegotiation", true);
        logger.LogInformation("🔓 已启用 OpenSSL Legacy Renegotiation 支持");
        
        // 配置 SSL 协议（支持旧版协议以兼容农行服务器）
        handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 
                             | System.Security.Authentication.SslProtocols.Tls11 
                             | System.Security.Authentication.SslProtocols.Tls;
        
        // 添加证书验证回调（生产环境应该验证服务器证书）
        handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                // TODO: 在生产环境中应该验证服务器证书
                // 当前为测试环境，接受所有证书
                return true;
            };
        
        logger.LogInformation("✅ HttpClient 已配置商户证书 (双向SSL): {Subject}", merchantCertificate.Subject);
    }
    else
    {
        logger.LogError("❌ 商户证书未加载，双向SSL认证将失败！");
    }
    
    logger.LogInformation("📋 客户端证书配置完成 - 共 {Count} 个证书", handler.ClientCertificates.Count);
    
    return handler;
});

// 添加默认 HttpClient（用于其他服务）
builder.Services.AddHttpClient();

// 添加支付服务
builder.Services.AddScoped<AbcPaymentService>();

// 添加 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 开发环境下启用详细异常页面
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// 启用 CORS
app.UseCors("AllowAll");

// 映射控制器路由
app.MapControllers();

// 添加基础路由（必须在静态文件之前）
app.MapGet("/", GetRootInfo)
    .WithName("Root");

app.MapGet("/health", GetHealth)
    .WithName("Health");

app.MapGet("/ping", GetPing)
    .WithName("Ping");

// Swagger 文档端点
app.MapGet("/swagger.json", GetSwaggerJson)
    .WithName("SwaggerJson");

app.MapGet("/swagger", GetSwaggerUI)
    .WithName("Swagger");

app.MapGet("/docs", GetSwaggerUI)
    .WithName("SwaggerUI");

app.Run();

// 端点处理函数
static IResult GetRootInfo()
{
    try
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var json = $@"{{""name"":""农行支付网关 API"",""version"":""1.0"",""status"":""running"",""timestamp"":""{DateTime.UtcNow:O}"",""environment"":""{env}""}}";
        return Results.Text(json, "application/json");
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
}

static IResult GetHealth()
{
    try
    {
        var uptime = (int)(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds;
        var json = $@"{{""status"":""healthy"",""timestamp"":""{DateTime.UtcNow:O}"",""uptime"":{uptime}}}";
        return Results.Text(json, "application/json");
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
}

static IResult GetPing()
{
    return Results.Text("pong");
}

static IResult GetSwaggerJson()
{
    try
    {
        var swaggerPath = Path.Combine(AppContext.BaseDirectory, "Web", "swagger.json");
        if (File.Exists(swaggerPath))
        {
            var json = File.ReadAllText(swaggerPath);
            return Results.Text(json, "application/json");
        }
        return Results.NotFound();
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
}

static IResult GetSwaggerUI()
{
    try
    {
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "Web", "swagger-ui.html");
        if (File.Exists(htmlPath))
        {
            var html = File.ReadAllText(htmlPath);
            return Results.Text(html, "text/html");
        }
        return Results.NotFound();
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
}
