using AbcPaymentGateway.Models;
using AbcPaymentGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace AbcPaymentGateway.Controllers;

/// <summary>
/// 支付控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly AbcPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        AbcPaymentService paymentService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// 创建支付订单 - 扫码支付
    /// </summary>
    /// <param name="request">支付请求</param>
    /// <returns>支付响应</returns>
    [HttpPost("qrcode")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    public async Task<IActionResult> CreateQRCodePayment([FromBody] PaymentRequest request)
    {
        _logger.LogInformation("收到扫码支付请求: OrderNo={OrderNo}", request.OrderNo);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 设置为扫码支付类型
        request.TrxType = "UDCAppQRCodePayReq";

        var response = await _paymentService.ProcessPaymentAsync(request);
        
        if (response.IsSuccess)
        {
            return Ok(response);
        }
        else
        {
            return BadRequest(response);
        }
    }

    /// <summary>
    /// 创建支付订单 - 电子钱包支付
    /// </summary>
    /// <param name="request">支付请求</param>
    /// <returns>支付响应</returns>
    [HttpPost("ewallet")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    public async Task<IActionResult> CreateEWalletPayment([FromBody] PaymentRequest request)
    {
        _logger.LogInformation("收到电子钱包支付请求: OrderNo={OrderNo}", request.OrderNo);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 设置为电子钱包支付类型
        request.TrxType = "EWalletPayReq";

        var response = await _paymentService.ProcessPaymentAsync(request);
        
        if (response.IsSuccess)
        {
            return Ok(response);
        }
        else
        {
            return BadRequest(response);
        }
    }

    /// <summary>
    /// 查询订单状态
    /// </summary>
    /// <param name="orderNo">订单号</param>
    /// <returns>订单状态</returns>
    [HttpGet("query/{orderNo}")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    public async Task<IActionResult> QueryOrder(string orderNo)
    {
        _logger.LogInformation("查询订单: OrderNo={OrderNo}", orderNo);

        if (string.IsNullOrEmpty(orderNo))
        {
            return BadRequest("订单号不能为空");
        }

        var response = await _paymentService.QueryOrderAsync(orderNo);
        return Ok(response);
    }

    /// <summary>
    /// 支付回调接口
    /// </summary>
    /// <returns>处理结果</returns>
    [HttpPost("notify")]
    public async Task<IActionResult> PaymentNotify()
    {
        _logger.LogInformation("收到支付回调通知");

        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            _logger.LogInformation("回调数据: {Body}", body);

            // TODO: 验证签名并处理回调数据
            // 这里需要根据农行的回调规范进行处理

            return Ok(new { success = true, message = "SUCCESS" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理支付回调失败");
            return StatusCode(500, new { success = false, message = "FAIL" });
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "ABC Payment Gateway"
        });
    }
}
