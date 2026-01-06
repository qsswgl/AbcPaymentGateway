using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AbcPaymentGateway.Services;

/// <summary>
/// 健康检查服务接口
/// </summary>
public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckHealthAsync();
}

/// <summary>
/// 健康检查服务实现
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(ILogger<HealthCheckService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 检查应用健康状态
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        try
        {
            _logger.LogInformation("执行健康检查");
            
            // 可以添加更多检查项
            // 例如: 数据库连接, 外部服务可用性等
            
            return HealthCheckResult.Healthy("应用运行正常");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return HealthCheckResult.Unhealthy("应用检查失败: " + ex.Message);
        }
    }
}
