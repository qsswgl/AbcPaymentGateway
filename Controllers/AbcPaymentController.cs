using Microsoft.AspNetCore.Mvc;
using AbcPaymentGateway.Models;
using AbcPaymentGateway.Services;

namespace AbcPaymentGateway.Controllers;

/// <summary>
/// 农行一码多扫支付控制器
/// </summary>
[ApiController]
[Route("api/payment/abc")]
[Produces("application/json")]
public class AbcPaymentController : ControllerBase
{
    private readonly ILogger<AbcPaymentController> _logger;
    private readonly AbcPaymentService _paymentService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AbcPaymentController(
        ILogger<AbcPaymentController> logger,
        AbcPaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }

    /// <summary>
    /// 农行一码多扫线上扫码下单
    /// </summary>
    /// <param name="request">扫码支付请求参数</param>
    /// <returns>返回扫码支付二维码URL</returns>
    /// <remarks>
    /// 农行综合收银台一码多扫功能，生成统一支付二维码，支持多种支付方式。
    /// 适用场景：PC网站支付、H5支付、线下扫码支付
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/abc/scanpay
    ///     {
    ///       "orderNo": "ORD20260117001",
    ///       "amount": 100.00,
    ///       "merchantId": "103881636900016",
    ///       "goodsName": "商品购买",
    ///       "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    ///       "payTypeID": "ImmediatePay",
    ///       "paymentType": "A",
    ///       "paymentLinkType": "1",
    ///       "commodityType": "0201"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">订单创建成功，返回二维码URL</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("scanpay")]
    [ProducesResponseType(typeof(AbcScanPayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateScanPayOrder([FromBody] AbcScanPayRequest request)
    {
        try
        {
            _logger.LogInformation("农行一码多扫下单请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AbcScanPayResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new AbcScanPayResponse 
                { 
                    IsSuccess = false,
                    Message = "支付金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.MerchantId))
            {
                return BadRequest(new AbcScanPayResponse 
                { 
                    IsSuccess = false,
                    Message = "商户号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.NotifyUrl))
            {
                return BadRequest(new AbcScanPayResponse 
                { 
                    IsSuccess = false,
                    Message = "回调地址不能为空",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行一码多扫接口
            var paymentResponse = await _paymentService.ProcessAbcScanPayAsync(request);

            _logger.LogInformation("农行一码多扫响应: OrderNo={OrderNo}, IsSuccess={IsSuccess}, Code={Code}, Message={Message}, QRCodeUrl={QRCodeUrl}",
                request.OrderNo, paymentResponse.IsSuccess, paymentResponse.ResponseCode, paymentResponse.ResponseMessage, paymentResponse.QRCodeUrl);

            // 转换为响应格式
            var response = new AbcScanPayResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = request.OrderNo,
                TransactionId = paymentResponse.TrxId ?? string.Empty,
                ScanPayQRURL = paymentResponse.QRCodeUrl ?? string.Empty,
                Amount = request.Amount,
                Status = paymentResponse.IsSuccess ? "SUCCESS" : "FAILED",
                Message = paymentResponse.ResponseMessage ?? "处理完成",
                ErrorCode = paymentResponse.ResponseCode,
                ReturnCode = paymentResponse.ResponseCode,
                ExpireTime = DateTime.Now.AddMinutes(30)
            };

            // ⚠️ EUNKWN特殊处理：交易结果未知，需要查询订单
            if (paymentResponse.ResponseCode == "EUNKWN")
            {
                _logger.LogWarning("交易结果未知(EUNKWN)，建议客户端查询订单状态: OrderNo={OrderNo}", request.OrderNo);
                response.Message = "交易结果未知，请稍后查询订单状态或联系客服确认 (EUNKWN)";
                response.Status = "UNKNOWN";
                // 仍然返回200，但IsSuccess=false
                return Ok(response);
            }

            if (!response.IsSuccess)
            {
                _logger.LogWarning("农行一码多扫下单失败: OrderNo={OrderNo}, ErrorCode={ErrorCode}, Message={Message}",
                    request.OrderNo, response.ErrorCode, response.Message);
                return BadRequest(response);
            }

            _logger.LogInformation("农行一码多扫下单成功: OrderNo={OrderNo}, QRCodeUrl={QRCodeUrl}",
                request.OrderNo, response.ScanPayQRURL);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "农行一码多扫下单异常: OrderNo={OrderNo}, Message={Message}", 
                request.OrderNo, ex.Message);
            return StatusCode(500, new 
            { 
                isSuccess = false,
                orderNo = request.OrderNo,
                message = $"支付处理失败: {ex.Message}", 
                errorCode = "INTERNAL_ERROR" 
            });
        }
    }

    /// <summary>
    /// 农行页面支付下单
    /// </summary>
    /// <param name="request">页面支付请求参数</param>
    /// <returns>返回支付页面URL</returns>
    /// <remarks>
    /// 农行综合收银台页面支付功能，用户将被跳转到农行支付页面完成支付。
    /// 适用场景：PC网站支付、H5支付
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/abc/pagepay
    ///     {
    ///       "orderNo": "ORD20260119001",
    ///       "amount": 10.00,
    ///       "merchantId": "103881636900016",
    ///       "goodsName": "结算单支付",
    ///       "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    ///       "merchantSuccessUrl": "https://payment.qsgl.net/success",
    ///       "merchantErrorUrl": "https://payment.qsgl.net/fail",
    ///       "payTypeID": "ImmediatePay",
    ///       "paymentType": "A",
    ///       "paymentLinkType": "1",
    ///       "commodityType": "0201"
    ///     }
    /// 
    /// 响应示例：
    /// 
    ///     {
    ///       "isSuccess": true,
    ///       "orderNo": "ORD20260119001",
    ///       "transactionId": "ABC202601190001",
    ///       "paymentURL": "https://pay.abchina.com/ebus/PaymentLink?id=xxx",
    ///       "amount": 10.00,
    ///       "status": "SUCCESS",
    ///       "message": "订单创建成功",
    ///       "expireTime": "2026-01-19T12:00:00"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">订单创建成功，返回支付URL</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("pagepay")]
    [ProducesResponseType(typeof(AbcPagePayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePagePayOrder([FromBody] AbcPagePayRequest request)
    {
        // 🔥 DEBUG: 直接输出到 stdout
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ========== 农行页面支付请求 ==========");
        Console.WriteLine($"OrderNo: {request.OrderNo}, Amount: {request.Amount}");
        
        try
        {
            _logger.LogInformation("农行页面支付下单请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AbcPagePayResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new AbcPagePayResponse 
                { 
                    IsSuccess = false,
                    Message = "支付金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.MerchantId))
            {
                return BadRequest(new AbcPagePayResponse 
                { 
                    IsSuccess = false,
                    Message = "商户号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.NotifyUrl))
            {
                return BadRequest(new AbcPagePayResponse 
                { 
                    IsSuccess = false,
                    Message = "回调地址不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.MerchantSuccessUrl))
            {
                return BadRequest(new AbcPagePayResponse 
                { 
                    IsSuccess = false,
                    Message = "成功返回地址不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.MerchantErrorUrl))
            {
                return BadRequest(new AbcPagePayResponse 
                { 
                    IsSuccess = false,
                    Message = "失败返回地址不能为空",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行页面支付接口
            var paymentResponse = await _paymentService.ProcessAbcPagePayAsync(request);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔵 农行响应 - Code: {paymentResponse.ResponseCode}, Success: {paymentResponse.IsSuccess}");
            
            _logger.LogInformation("农行页面支付响应: OrderNo={OrderNo}, IsSuccess={IsSuccess}, Code={Code}, Message={Message}, PaymentURL={PaymentURL}",
                request.OrderNo, paymentResponse.IsSuccess, paymentResponse.ResponseCode, paymentResponse.ResponseMessage, paymentResponse.PaymentURL);

            // 转换为响应格式
            var response = new AbcPagePayResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = request.OrderNo,
                TransactionId = paymentResponse.TrxId ?? string.Empty,
                PaymentURL = paymentResponse.PaymentURL ?? string.Empty,
                Amount = request.Amount,
                Status = paymentResponse.IsSuccess ? "SUCCESS" : "FAILED",
                Message = paymentResponse.ResponseMessage ?? "处理完成",
                ErrorCode = paymentResponse.ResponseCode,
                ReturnCode = paymentResponse.ResponseCode,
                ExpireTime = DateTime.Now.AddMinutes(30)
            };

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔵 构造响应完成, ErrorCode: {response.ErrorCode}");

            // ⚠️ EUNKWN特殊处理：交易结果未知，需要查询订单
            if (paymentResponse.ResponseCode == "EUNKWN")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️⚠️⚠️ 检测到EUNKWN错误！");
                
                _logger.LogWarning("交易结果未知(EUNKWN)，建议客户端查询订单状态: OrderNo={OrderNo}", request.OrderNo);
                response.Message = "交易结果未知，请稍后查询订单状态或联系客服确认 (EUNKWN)";
                response.Status = "UNKNOWN";
                // 仍然返回200，但IsSuccess=false
                return Ok(response);
            }

            if (!response.IsSuccess)
            {
                _logger.LogWarning("农行页面支付下单失败: OrderNo={OrderNo}, ErrorCode={ErrorCode}, Message={Message}",
                    request.OrderNo, response.ErrorCode, response.Message);
                return BadRequest(response);
            }

            _logger.LogInformation("农行页面支付下单成功: OrderNo={OrderNo}, PaymentURL={PaymentURL}",
                request.OrderNo, response.PaymentURL);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "农行页面支付下单异常: OrderNo={OrderNo}, Message={Message}", 
                request.OrderNo, ex.Message);
            return StatusCode(500, new 
            { 
                isSuccess = false,
                orderNo = request.OrderNo,
                message = $"支付处理失败: {ex.Message}", 
                errorCode = "INTERNAL_ERROR" 
            });
        }
    }
}
