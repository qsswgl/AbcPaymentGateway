using Microsoft.AspNetCore.Mvc;
using AbcPaymentGateway.Models;
using AbcPaymentGateway.Services;

namespace AbcPaymentGateway.Controllers;

/// <summary>
/// 支付宝支付控制器
/// </summary>
[ApiController]
[Route("api/payment/alipay")]
[Produces("application/json")]
public class AlipayController : ControllerBase
{
    private readonly ILogger<AlipayController> _logger;
    private readonly AbcPaymentService _paymentService;
    private readonly IAbcCertificateService _certificateService;

    public AlipayController(
        ILogger<AlipayController> logger, 
        AbcPaymentService paymentService,
        IAbcCertificateService certificateService)
    {
        _logger = logger;
        _paymentService = paymentService;
        _certificateService = certificateService;
    }

    /// <summary>
    /// 支付宝扫码支付（被扫）
    /// </summary>
    /// <param name="request">支付请求参数</param>
    /// <returns>返回支付二维码URL</returns>
    /// <remarks>
    /// 商户主动生成支付二维码，用户使用支付宝APP扫描二维码完成支付。
    /// 适用场景：PC网站支付、线下扫码支付
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/alipay/qrcode
    ///     {
    ///       "orderNo": "ORD20260115001",
    ///       "amount": 0.01,
    ///       "merchantId": "103881636900016",
    ///       "goodsName": "测试商品",
    ///       "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    ///       "expiredDate": "30"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">支付订单创建成功，返回二维码URL</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("qrcode")]
    [ProducesResponseType(typeof(AlipayQRCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateQRCodePayment([FromBody] AlipayQRCodeRequest request)
    {
        try
        {
            _logger.LogInformation("支付宝扫码支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AlipayQRCodeResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new AlipayQRCodeResponse 
                { 
                    IsSuccess = false,
                    Message = "支付金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.MerchantId))
            {
                return BadRequest(new AlipayQRCodeResponse 
                { 
                    IsSuccess = false,
                    Message = "商户号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行综合收银台支付宝接口
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
                Message = paymentResponse.ResponseMessage ?? "处理完成",
                ErrorCode = paymentResponse.ResponseCode,
                ExpireTime = DateTime.Now.AddMinutes(30)
            };

            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "支付宝扫码支付异常: {Message}", ex.Message);
            return StatusCode(500, new { message = $"支付处理失败: {ex.Message}", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// 支付宝订单预创建（ALI_PRECREATE）
    /// </summary>
    /// <param name="request">订单预创建请求参数</param>
    /// <returns>返回支付二维码URL</returns>
    /// <remarks>
    /// 预先创建支付宝订单，生成二维码供用户扫码支付。
    /// 适用场景：线下扫码支付、预生成订单
    /// PayTypeID=4（ALI_PRECREATE）
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/alipay/precreate
    ///     {
    ///       "orderNo": "ORD20260116001",
    ///       "amount": 100.00,
    ///       "merchantId": "103881636900016",
    ///       "goodsName": "商品购买",
    ///       "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    ///       "expiredDate": "30",
    ///       "storeId": "STORE_001",
    ///       "terminalId": "TERM_001"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">订单预创建成功，返回二维码URL</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("precreate")]
    [ProducesResponseType(typeof(AlipayPrecreateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePrecreateOrder([FromBody] AlipayPrecreateRequest request)
    {
        try
        {
            _logger.LogInformation("支付宝订单预创建请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AlipayPrecreateResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new AlipayPrecreateResponse 
                { 
                    IsSuccess = false,
                    Message = "支付金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.MerchantId))
            {
                return BadRequest(new AlipayPrecreateResponse 
                { 
                    IsSuccess = false,
                    Message = "商户号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行支付宝订单预创建接口
            var paymentResponse = await _paymentService.ProcessAlipayPrecreateAsync(request);

            // 转换为响应格式
            var response = new AlipayPrecreateResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = request.OrderNo,
                TransactionId = paymentResponse.TrxId ?? string.Empty,
                QrCodeUrl = paymentResponse.QRCodeUrl ?? string.Empty,
                Amount = request.Amount,
                Status = paymentResponse.IsSuccess ? "SUCCESS" : "FAILED",
                Message = paymentResponse.ResponseMessage ?? "处理完成",
                ErrorCode = paymentResponse.ResponseCode,
                ExpireTime = DateTime.Now.AddMinutes(string.IsNullOrEmpty(request.ExpiredDate) ? 30 : int.Parse(request.ExpiredDate))
            };

            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "支付宝订单预创建异常: {Message}", ex.Message);
            return StatusCode(500, new { message = $"订单预创建失败: {ex.Message}", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// 支付宝WAP支付
    /// </summary>
    /// <param name="request">支付请求参数</param>
    /// <returns>返回支付页面URL</returns>
    /// <remarks>
    /// 在移动端网页应用中，跳转到支付宝APP或支付宝网页完成支付。
    /// 适用场景：手机网站支付、H5支付
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/alipay/wap
    ///     {
    ///       "orderNo": "ORD20260113002",
    ///       "amount": 88.88,
    ///       "merchantId": "103881636900016",
    ///       "goodsName": "商品购买",
    ///       "notifyUrl": "https://example.com/api/payment/notify",
    ///       "returnUrl": "https://example.com/payment/result",
    ///       "quitUrl": "https://example.com"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">支付订单创建成功，返回支付URL</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("wap")]
    [ProducesResponseType(typeof(AlipayWapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateWapPayment([FromBody] AlipayWapRequest request)
    {
        try
        {
            _logger.LogInformation("支付宝WAP支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AlipayWapResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new AlipayWapResponse 
                { 
                    IsSuccess = false,
                    Message = "支付金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行综合收银台支付宝WAP接口
            var paymentResponse = await _paymentService.ProcessAlipayWapPaymentAsync(request);

            var response = new AlipayWapResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = request.OrderNo,
                TransactionId = paymentResponse.TrxId,
                PaymentUrl = paymentResponse.QRCodeUrl ?? "",  // WAP支付返回的是跳转URL
                Amount = request.Amount,
                Status = paymentResponse.IsSuccess ? "SUCCESS" : "FAILED",
                Message = paymentResponse.ResponseMessage ?? "处理完成",
                ErrorCode = paymentResponse.ResponseCode
            };

            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "支付宝WAP支付异常: {Message}", ex.Message);
            return StatusCode(500, new { message = $"支付处理失败: {ex.Message}", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// 支付宝APP支付
    /// </summary>
    /// <param name="request">支付请求参数</param>
    /// <returns>返回APP端调用支付宝SDK所需的订单信息</returns>
    /// <remarks>
    /// 在APP中集成支付宝SDK，通过本接口获取订单字符串，然后调用支付宝SDK发起支付。
    /// 适用场景：APP支付
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/alipay/app
    ///     {
    ///       "orderNo": "ORD20260113003",
    ///       "amount": 199.99,
    ///       "merchantId": "103881636900016",
    ///       "goodsName": "商品购买",
    ///       "notifyUrl": "https://example.com/api/payment/notify"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">支付订单创建成功，返回APP调起支付宝SDK所需的订单字符串</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("app")]
    [ProducesResponseType(typeof(AlipayAppResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAppPayment([FromBody] AlipayAppRequest request)
    {
        try
        {
            _logger.LogInformation("支付宝APP支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AlipayAppResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new AlipayAppResponse 
                { 
                    IsSuccess = false,
                    Message = "支付金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行综合收银台支付宝APP接口
            var paymentResponse = await _paymentService.ProcessAlipayAppPaymentAsync(request);

            var response = new AlipayAppResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = request.OrderNo,
                TransactionId = paymentResponse.TrxId,
                OrderString = paymentResponse.QRCodeUrl ?? "",  // APP支付返回的订单字符串
                Amount = request.Amount,
                Status = paymentResponse.IsSuccess ? "SUCCESS" : "FAILED",
                Message = paymentResponse.ResponseMessage ?? "处理完成",
                ErrorCode = paymentResponse.ResponseCode
            };

            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "支付宝APP支付异常: {Message}", ex.Message);
            return StatusCode(500, new { message = $"支付处理失败: {ex.Message}", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// 支付宝订单查询
    /// </summary>
    /// <param name="orderNo">商户订单号</param>
    /// <returns>返回订单支付状态</returns>
    /// <remarks>
    /// 查询支付宝订单的支付状态，用于确认订单是否支付成功。
    /// 
    /// 请求示例：
    /// 
    ///     GET /api/payment/alipay/query/ORD20260113001
    /// 
    /// </remarks>
    /// <response code="200">查询成功</response>
    /// <response code="404">订单不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("query/{orderNo}")]
    [ProducesResponseType(typeof(AlipayQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QueryOrder(string orderNo)
    {
        try
        {
            _logger.LogInformation("查询支付宝订单: OrderNo={OrderNo}", orderNo);

            if (string.IsNullOrWhiteSpace(orderNo))
            {
                return BadRequest(new { message = "订单号不能为空", errorCode = "PARAM_ERROR" });
            }

            // 从请求头或查询参数获取商户ID（如果需要）
            var merchantId = Request.Headers["X-Merchant-Id"].FirstOrDefault() ?? "103881636900016";

            // 调用农行综合收银台订单查询接口
            var paymentResponse = await _paymentService.QueryAlipayOrderAsync(orderNo, merchantId);

            var response = new AlipayQueryResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = orderNo,
                TransactionId = paymentResponse.TrxId,
                ThirdOrderNo = paymentResponse.ThirdPartyOrderNo,
                Amount = decimal.TryParse(paymentResponse.OrderAmount, out var amt) ? amt : 0,
                Status = MapOrderStatus(paymentResponse.ResponseCode),
                PayTime = paymentResponse.PayTime,
                Message = paymentResponse.ResponseMessage ?? "查询成功",
                ErrorCode = paymentResponse.ResponseCode
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询支付宝订单异常: {Message}", ex.Message);
            return StatusCode(500, new { message = $"查询失败: {ex.Message}", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// 映射订单状态
    /// </summary>
    private string MapOrderStatus(string? responseCode)
    {
        return responseCode switch
        {
            "0000" => "SUCCESS",      // 支付成功
            "1001" => "PROCESSING",   // 处理中
            "2001" => "CLOSED",       // 已关闭
            "3001" => "REFUNDED",     // 已退款
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// 支付宝退款
    /// </summary>
    /// <param name="request">退款请求参数</param>
    /// <returns>返回退款结果</returns>
    /// <remarks>
    /// 对已支付成功的订单发起退款。
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/alipay/refund
    ///     {
    ///       "orderNo": "ORD20260113001",
    ///       "refundAmount": 50.00,
    ///       "refundReason": "用户申请退款",
    ///       "merchantId": "103881636900016"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">退款成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("refund")]
    [ProducesResponseType(typeof(AlipayRefundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refund([FromBody] AlipayRefundRequest request)
    {
        try
        {
            _logger.LogInformation("支付宝退款请求: OrderNo={OrderNo}, RefundAmount={RefundAmount}", 
                request.OrderNo, request.RefundAmount);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AlipayRefundResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.RefundAmount <= 0)
            {
                return BadRequest(new AlipayRefundResponse 
                { 
                    IsSuccess = false,
                    Message = "退款金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行综合收银台退款接口
            var paymentResponse = await _paymentService.RefundAlipayOrderAsync(request);

            var response = new AlipayRefundResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = request.OrderNo,
                RefundNo = paymentResponse.TrxId,
                RefundAmount = request.RefundAmount,
                Status = paymentResponse.IsSuccess ? "SUCCESS" : "FAILED",
                Message = paymentResponse.ResponseMessage ?? "退款处理完成",
                ErrorCode = paymentResponse.ResponseCode,
                RefundTime = paymentResponse.IsSuccess ? DateTime.Now : null
            };

            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "支付宝退款异常: {Message}", ex.Message);
            return StatusCode(500, new { message = $"退款失败: {ex.Message}", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// 支付宝PC网页支付
    /// </summary>
    /// <param name="request">支付请求参数</param>
    /// <returns>返回支付页面URL</returns>
    /// <remarks>
    /// 在PC电脑网页中，跳转到支付宝页面完成支付。
    /// 适用场景：PC网站支付、电脑端在线支付
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/alipay/pc
    ///     {
    ///       "orderNo": "ORD20260113004",
    ///       "amount": 288.00,
    ///       "merchantId": "103881636900016",
    ///       "goodsName": "电脑商品购买",
    ///       "notifyUrl": "https://example.com/api/payment/notify",
    ///       "returnUrl": "https://example.com/payment/result",
    ///       "quitUrl": "https://example.com"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">支付订单创建成功，返回支付URL</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("pc")]
    [ProducesResponseType(typeof(AlipayPCResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePCPayment([FromBody] AlipayPCRequest request)
    {
        try
        {
            _logger.LogInformation("支付宝PC支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AlipayPCResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new AlipayPCResponse 
                { 
                    IsSuccess = false,
                    Message = "支付金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行综合收银台支付宝PC接口
            var paymentResponse = await _paymentService.ProcessAlipayPCPaymentAsync(request);

            var response = new AlipayPCResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = request.OrderNo,
                TransactionId = paymentResponse.TrxId,
                PaymentUrl = paymentResponse.QRCodeUrl ?? "",
                Amount = request.Amount,
                Status = paymentResponse.IsSuccess ? "SUCCESS" : "FAILED",
                Message = paymentResponse.ResponseMessage ?? "处理完成",
                ErrorCode = paymentResponse.ResponseCode
            };

            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "支付宝PC支付异常: {Message}", ex.Message);
            return StatusCode(500, new { message = $"支付处理失败: {ex.Message}", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// 支付宝付款码支付（主扫）
    /// </summary>
    /// <param name="request">支付请求参数</param>
    /// <returns>返回支付结果</returns>
    /// <remarks>
    /// 商户使用扫码枪扫描用户支付宝付款码完成支付。
    /// 适用场景：线下收银台、POS机支付
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/alipay/barcode
    ///     {
    ///       "orderNo": "ORD20260113005",
    ///       "amount": 68.00,
    ///       "merchantId": "103881636900016",
    ///       "goodsName": "线下扫码支付",
    ///       "authCode": "280123456789012345",
    ///       "notifyUrl": "https://example.com/api/payment/notify"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">支付成功或处理中</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("barcode")]
    [ProducesResponseType(typeof(AlipayBarCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateBarCodePayment([FromBody] AlipayBarCodeRequest request)
    {
        try
        {
            _logger.LogInformation("支付宝付款码支付请求: OrderNo={OrderNo}, Amount={Amount}, AuthCode={AuthCode}", 
                request.OrderNo, request.Amount, request.AuthCode);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.OrderNo))
            {
                return BadRequest(new AlipayBarCodeResponse 
                { 
                    IsSuccess = false,
                    Message = "订单号不能为空",
                    OrderNo = request.OrderNo
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new AlipayBarCodeResponse 
                { 
                    IsSuccess = false,
                    Message = "支付金额必须大于0",
                    OrderNo = request.OrderNo
                });
            }

            if (string.IsNullOrWhiteSpace(request.AuthCode))
            {
                return BadRequest(new AlipayBarCodeResponse 
                { 
                    IsSuccess = false,
                    Message = "付款码不能为空",
                    OrderNo = request.OrderNo
                });
            }

            // 调用农行综合收银台支付宝付款码接口
            var paymentResponse = await _paymentService.ProcessAlipayBarCodePaymentAsync(request);

            var response = new AlipayBarCodeResponse
            {
                IsSuccess = paymentResponse.IsSuccess,
                OrderNo = request.OrderNo,
                TransactionId = paymentResponse.TrxId,
                ThirdOrderNo = paymentResponse.ThirdPartyOrderNo,
                Amount = request.Amount,
                Status = MapBarCodeStatus(paymentResponse.ResponseCode),
                Message = paymentResponse.ResponseMessage ?? "处理完成",
                ErrorCode = paymentResponse.ResponseCode,
                PayTime = paymentResponse.IsSuccess ? DateTime.Now : null
            };

            if (!response.IsSuccess && response.Status != "USERPAYING")
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "支付宝付款码支付异常: {Message}", ex.Message);
            return StatusCode(500, new { message = $"支付处理失败: {ex.Message}", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// 映射付款码支付状态
    /// </summary>
    private string MapBarCodeStatus(string? responseCode)
    {
        return responseCode switch
        {
            "0000" => "SUCCESS",      // 支付成功
            "1002" => "USERPAYING",   // 用户支付中（需要轮询查询）
            _ => "FAILED"
        };
    }

    /// <summary>
    /// 测试接口 - 生成支付宝APP支付订单字符串
    /// </summary>
    /// <returns>返回订单字符串及分析结果</returns>
    /// <remarks>
    /// 测试接口，用于验证支付宝APP支付订单字符串生成是否正确。
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/payment/alipay/test/orderstring
    /// 
    /// </remarks>
    [HttpPost("test/orderstring")]
    public async Task<IActionResult> TestOrderString()
    {
        try
        {
            _logger.LogInformation("开始测试订单字符串生成");

            // 调用APP支付接口生成订单
            var request = new AlipayAppRequest
            {
                OrderNo = $"TEST{DateTime.Now:yyyyMMddHHmmss}",
                Amount = 0.01m,
                MerchantId = "103881636900016",
                GoodsName = "测试商品",
                NotifyUrl = "https://payment.qsgl.net/api/payment/notify"
            };

            var paymentResponse = await _paymentService.ProcessAlipayAppPaymentAsync(request);

            // 从响应中获取订单字符串（QRCodeUrl字段存储了orderString）
            var orderString = paymentResponse.QRCodeUrl ?? "";

            // 检查证书状态
            var certStatus = _certificateService.GetCertificateStatus();

            return Ok(new
            {
                success = paymentResponse.IsSuccess,
                orderNo = request.OrderNo,
                orderStringLength = orderString.Length,
                orderString = orderString,
                containsPlaceholder = orderString.Contains("..."),
                containsSign = orderString.Contains("sign="),
                analysis = new
                {
                    hasAppId = orderString.Contains("app_id="),
                    hasBizContent = orderString.Contains("biz_content="),
                    hasCharset = orderString.Contains("charset="),
                    hasMethod = orderString.Contains("method="),
                    hasSignType = orderString.Contains("sign_type="),
                    hasTimestamp = orderString.Contains("timestamp="),
                    hasVersion = orderString.Contains("version="),
                    hasSign = orderString.Contains("sign=")
                },
                paymentResponse = new
                {
                    responseCode = paymentResponse.ResponseCode,
                    responseMessage = paymentResponse.ResponseMessage,
                    trxId = paymentResponse.TrxId,
                    isSuccess = paymentResponse.IsSuccess
                },
                certificateStatus = certStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试订单字符串生成异常");
            return Ok(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// 测试接口 - 检查证书状态
    /// </summary>
    /// <returns>返回证书详细信息</returns>
    [HttpGet("test/certificate")]
    public IActionResult TestCertificate()
    {
        try
        {
            _logger.LogInformation("开始检查证书状态");

            var certStatus = _certificateService.GetCertificateStatus();
            var basePath = AppContext.BaseDirectory;

            return Ok(new
            {
                success = true,
                basePath = basePath,
                certificateStatus = certStatus,
                environmentInfo = new
                {
                    osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    processArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查证书状态异常");
            return Ok(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// 测试接口 - 完整支付流程测试
    /// </summary>
    /// <returns>返回完整的支付测试结果</returns>
    [HttpPost("test/fullpayment")]
    public async Task<IActionResult> TestFullPayment()
    {
        try
        {
            _logger.LogInformation("开始完整支付流程测试");

            var orderNo = $"TEST{DateTime.Now:yyyyMMddHHmmss}";
            var results = new Dictionary<string, object>();

            // 1. 测试扫码支付
            try
            {
                var qrcodeRequest = new AlipayQRCodeRequest
                {
                    OrderNo = $"{orderNo}_QR",
                    Amount = 0.01m,
                    MerchantId = "103881636900016",
                    GoodsName = "测试扫码支付",
                    NotifyUrl = "https://payment.qsgl.net/api/payment/notify"
                };
                var qrcodeResult = await _paymentService.ProcessAlipayPaymentAsync(qrcodeRequest);
                results["qrcode"] = new
                {
                    success = qrcodeResult.IsSuccess,
                    responseCode = qrcodeResult.ResponseCode,
                    responseMessage = qrcodeResult.ResponseMessage,
                    qrCodeUrl = qrcodeResult.QRCodeUrl
                };
            }
            catch (Exception ex)
            {
                results["qrcode"] = new { success = false, error = ex.Message };
            }

            // 2. 测试APP支付
            try
            {
                var appRequest = new AlipayAppRequest
                {
                    OrderNo = $"{orderNo}_APP",
                    Amount = 0.01m,
                    MerchantId = "103881636900016",
                    GoodsName = "测试APP支付",
                    NotifyUrl = "https://payment.qsgl.net/api/payment/notify"
                };
                var appResult = await _paymentService.ProcessAlipayAppPaymentAsync(appRequest);
                results["app"] = new
                {
                    success = appResult.IsSuccess,
                    responseCode = appResult.ResponseCode,
                    responseMessage = appResult.ResponseMessage,
                    orderString = appResult.QRCodeUrl
                };
            }
            catch (Exception ex)
            {
                results["app"] = new { success = false, error = ex.Message };
            }

            // 3. 检查证书
            results["certificate"] = _certificateService.GetCertificateStatus();

            return Ok(new
            {
                success = true,
                testTime = DateTime.Now,
                baseOrderNo = orderNo,
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完整支付流程测试异常");
            return Ok(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}
