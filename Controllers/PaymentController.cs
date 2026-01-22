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
    private readonly IAbcCertificateService _certificateService;

    public PaymentController(
        AbcPaymentService paymentService,
        ILogger<PaymentController> logger,
        IAbcCertificateService certificateService)
    {
        _paymentService = paymentService;
        _logger = logger;
        _certificateService = certificateService;
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
    /// 农行一码多扫线上扫码下单（官方V3.0 MSG格式）
    /// </summary>
    /// <param name="request">扫码支付请求</param>
    /// <returns>支付响应</returns>
    [HttpPost("abc-scan")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    public async Task<IActionResult> CreateAbcScanPayment([FromBody] AbcScanPayRequest request)
    {
        _logger.LogInformation("收到农行一码多扫请求: OrderNo={OrderNo}, Amount={Amount}, MerchantId={MerchantId}, GoodsName={GoodsName}, NotifyUrl={NotifyUrl}", 
            request.OrderNo, request.Amount, request.MerchantId, request.GoodsName, request.NotifyUrl);

        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            _logger.LogWarning("模型验证失败: {Errors}", errors);
            return BadRequest(ModelState);
        }

        // 默认使用第一个商户ID（如果未指定）
        if (string.IsNullOrEmpty(request.MerchantId))
        {
            // 从配置中获取默认商户ID
            var config = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AbcPaymentConfig>>();
            request.MerchantId = config.Value.MerchantIds.FirstOrDefault() ?? "";
        }

        var response = await _paymentService.ProcessAbcScanPayAsync(request);
        
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
    /// 农行页面支付（PayReq - 与Demo相同的TrxType，用于验证证书）
    /// </summary>
    /// <param name="request">页面支付请求</param>
    /// <returns>支付响应</returns>
    [HttpPost("abc-page")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    public async Task<IActionResult> CreateAbcPagePayment([FromBody] AbcScanPayRequest request)
    {
        _logger.LogInformation("收到农行页面支付请求: OrderNo={OrderNo}, Amount={Amount}", 
            request.OrderNo, request.Amount);

        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            _logger.LogWarning("模型验证失败: {Errors}", errors);
            return BadRequest(ModelState);
        }

        // 默认使用第一个商户ID
        if (string.IsNullOrEmpty(request.MerchantId))
        {
            var config = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AbcPaymentConfig>>();
            request.MerchantId = config.Value.MerchantIds.FirstOrDefault() ?? "";
        }

        // 使用 PayReq（页面支付）- 与Demo相同
        var pageRequest = new AbcPagePayRequest
        {
            OrderNo = request.OrderNo,
            Amount = request.Amount,
            GoodsName = request.GoodsName ?? "测试商品",
            NotifyUrl = request.NotifyUrl ?? "http://127.0.0.1/Merchant/MerchantResult.aspx",
            PayTypeID = "ImmediatePay",  // 与Demo一致
            MerchantId = request.MerchantId  // 传递商户ID
        };

        var response = await _paymentService.ProcessAbcPagePayAsync(pageRequest);
        
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

    /// <summary>
    /// 创建支付订单 - 微信支付（APP 原生 SDK 支付）
    /// </summary>
    /// <param name="request">支付请求</param>
    /// <returns>微信支付 SDK 所需参数</returns>
    /// <remarks>
    /// 此接口用于 APP 端调用原生微信 SDK 进行支付。
    /// 流程：APP → 农行综合收银台 API → 微信支付 → 农行 → 商户银行账户
    /// 
    /// 使用说明：
    /// 1. APP 调用此接口创建支付订单
    /// 2. 接口返回微信 SDK 所需的参数（appId、timeStamp、nonceStr、package、signType、paySign）
    /// 3. APP 使用这些参数调用微信原生 SDK 发起支付
    /// 4. 用户在微信客户端完成支付
    /// 5. 微信通过回调接口 /api/payment/notify 返回支付结果
    /// </remarks>
    [HttpPost("wechat")]
    [ProducesResponseType(typeof(WeChatPaymentSDKResponse), 200)]
    public async Task<IActionResult> CreateWeChatPayment([FromBody] PaymentRequest request)
    {
        _logger.LogInformation("收到微信支付请求: OrderNo={OrderNo}, Amount={Amount}, ClientIP={ClientIP}", 
            request.OrderNo, request.OrderAmount, request.ClientIP);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 验证必要参数
        if (string.IsNullOrEmpty(request.OrderNo) || string.IsNullOrEmpty(request.OrderAmount))
        {
            return BadRequest(new WeChatPaymentSDKResponse
            {
                IsSuccess = false,
                ErrorCode = "PARAM_ERROR",
                ErrorMessage = "订单号和金额不能为空"
            });
        }

        try
        {
            // 设置为微信支付类型
            request.TrxType = "WeChatAppPayReq";

            // 调用支付服务处理微信支付
            var paymentResponse = await _paymentService.ProcessWeChatPaymentAsync(request);

            if (!paymentResponse.IsSuccess)
            {
                return BadRequest(new WeChatPaymentSDKResponse
                {
                    IsSuccess = false,
                    ErrorCode = paymentResponse.ResponseCode,
                    ErrorMessage = paymentResponse.ResponseMessage,
                    OrderNo = request.OrderNo
                });
            }

            // 生成微信 SDK 所需的参数
            var sdkResponse = GenerateWeChatSDKSignature(paymentResponse, request);
            
            return Ok(sdkResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理微信支付请求失败: OrderNo={OrderNo}", request.OrderNo);
            return StatusCode(500, new WeChatPaymentSDKResponse
            {
                IsSuccess = false,
                ErrorCode = "SYSTEM_ERROR",
                ErrorMessage = "系统处理失败，请稍后重试",
                OrderNo = request.OrderNo
            });
        }
    }

    /// <summary>
    /// 生成微信 SDK 签名
    /// </summary>
    /// <param name="paymentResponse">支付响应</param>
    /// <param name="request">支付请求</param>
    /// <returns>微信 SDK 响应</returns>
    private WeChatPaymentSDKResponse GenerateWeChatSDKSignature(PaymentResponse paymentResponse, PaymentRequest request)
    {
        // 从支付响应中提取 prepay_id（农行返回）
        // 这里需要根据农行实际返回的字段名进行映射
        var prepayId = ExtractPrepayId(paymentResponse);

        if (string.IsNullOrEmpty(prepayId))
        {
            _logger.LogError("无法从支付响应中提取 prepay_id");
            return new WeChatPaymentSDKResponse
            {
                IsSuccess = false,
                ErrorCode = "PREPAY_ID_ERROR",
                ErrorMessage = "支付系统返回无效的支付标识"
            };
        }

        // 生成微信 SDK 所需的参数
        var appId = GetWeChatAppId(); // 农行聚合收银台的 AppID
        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonceStr = GenerateNonceString();
        var package = $"prepay_id={prepayId}";
        var signType = "MD5"; // 或 SHA256，根据农行配置

        // 生成签名
        var paySign = GenerateWeChatPaySignature(appId, timeStamp, nonceStr, package, signType);

        return new WeChatPaymentSDKResponse
        {
            AppId = appId,
            TimeStamp = timeStamp,
            NonceStr = nonceStr,
            Package = package,
            SignType = signType,
            PaySign = paySign,
            OrderNo = request.OrderNo,
            TrxId = paymentResponse.TrxId,
            IsSuccess = true,
            Amount = request.OrderAmount,
            GoodsDescription = request.OrderDesc ?? request.ProductName ?? "商品购买"
        };
    }

    /// <summary>
    /// 从支付响应中提取 prepay_id
    /// </summary>
    private string? ExtractPrepayId(PaymentResponse response)
    {
        // TODO: 根据农行实际返回的格式进行解析
        // 通常农行会在 RawResponse 中返回 prepay_id
        if (!string.IsNullOrEmpty(response.RawResponse))
        {
            try
            {
                // 如果是 JSON 格式，解析获取 prepay_id
                using var doc = System.Text.Json.JsonDocument.Parse(response.RawResponse);
                if (doc.RootElement.TryGetProperty("prepay_id", out var prepayId))
                {
                    return prepayId.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 prepay_id 失败");
            }
        }

        return response.TrxId; // 默认使用交易流水号作为 prepay_id
    }

    /// <summary>
    /// 获取微信 AppID（农行聚合收银台的 AppID）
    /// </summary>
    private string GetWeChatAppId()
    {
        // TODO: 从配置文件读取农行聚合收银台的 AppID
        return Environment.GetEnvironmentVariable("WECHAT_APP_ID") ?? "wxdefault"; // 默认值
    }

    /// <summary>
    /// 生成随机字符串
    /// </summary>
    private string GenerateNonceString()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new System.Random();
        return new string(Enumerable.Range(0, 32)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    /// <summary>
    /// 生成微信支付签名
    /// </summary>
    private string GenerateWeChatPaySignature(string appId, string timeStamp, string nonceStr, string package, string signType)
    {
        // 按照微信要求的顺序排序参数
        var signData = $"appId={appId}&nonceStr={nonceStr}&package={package}&signType={signType}&timeStamp={timeStamp}";

        // TODO: 根据农行综合收银台的要求，可能需要使用商户证书进行签名
        // 示例：使用证书签名
        // var dataBytes = System.Text.Encoding.UTF8.GetBytes(signData);
        // var signatureBytes = _certificateService.SignData(dataBytes);
        // return Convert.ToBase64String(signatureBytes);
        // _logger.LogDebug("微信支付签名完成，使用商户证书: 103881636900016.pfx");

        // TODO: 从配置读取 API 密钥
        var apiKey = Environment.GetEnvironmentVariable("WECHAT_API_KEY") ?? "default_key";

        if (signType.ToUpper() == "SHA256")
        {
            // HMAC-SHA256 签名
            using (var hmac = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(apiKey)))
            {
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signData));
                return Convert.ToHexString(hash).ToLower();
            }
        }
        else
        {
            // MD5 签名（默认）
            var md5 = System.Security.Cryptography.MD5.Create();
            var md5Data = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signData));
            return Convert.ToHexString(md5Data).ToLower();
        }
    }
}
