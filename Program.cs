using System.Text.Json.Serialization;
using System.Diagnostics;
using AbcPaymentGateway.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器支持
builder.Services.AddControllers();

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
