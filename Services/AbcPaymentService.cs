using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using AbcPaymentGateway.Models;
using Microsoft.Extensions.Options;

namespace AbcPaymentGateway.Services;

/// <summary>
/// 农行支付服务
/// </summary>
public class AbcPaymentService
{
    private readonly AbcPaymentConfig _config;
    private readonly ILogger<AbcPaymentService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IAbcCertificateService _certificateService;

    public AbcPaymentService(
        IOptions<AbcPaymentConfig> config,
        ILogger<AbcPaymentService> logger,
        IHttpClientFactory httpClientFactory,
        IAbcCertificateService certificateService)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("AbcPayment");
        _certificateService = certificateService;
    }

    /// <summary>
    /// 处理支付请求
    /// </summary>
    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            _logger.LogInformation("开始处理支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.OrderAmount);

            // 构建请求数据
            var requestData = BuildRequestData(request);

            // 发送到农行支付平台
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("支付请求完成: OrderNo={OrderNo}, Response={Response}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理支付请求失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"系统错误: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建请求数据
    /// </summary>
    private Dictionary<string, string> BuildRequestData(PaymentRequest request)
    {
        var data = new Dictionary<string, string>
        {
            ["TrxType"] = request.TrxType,
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.OrderAmount,
            ["MerchantID"] = _config.MerchantIds.FirstOrDefault() ?? ""
        };

        // 添加可选字段
        if (!string.IsNullOrEmpty(request.OrderDesc))
            data["OrderDesc"] = request.OrderDesc;
        
        if (!string.IsNullOrEmpty(request.OrderValidTime))
            data["OrderValidTime"] = request.OrderValidTime;
        
        if (!string.IsNullOrEmpty(request.PayQRCode))
            data["PayQRCode"] = request.PayQRCode;
        
        if (!string.IsNullOrEmpty(request.OrderTime))
            data["OrderTime"] = request.OrderTime;
        else
            data["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss");
        
        if (!string.IsNullOrEmpty(request.OrderAbstract))
            data["OrderAbstract"] = request.OrderAbstract;
        
        if (!string.IsNullOrEmpty(request.ResultNotifyURL))
            data["ResultNotifyURL"] = request.ResultNotifyURL;
        
        if (!string.IsNullOrEmpty(request.ProductName))
            data["ProductName"] = request.ProductName;
        
        if (!string.IsNullOrEmpty(request.PaymentType))
            data["PaymentType"] = request.PaymentType;
        
        if (!string.IsNullOrEmpty(request.PaymentLinkType))
            data["PaymentLinkType"] = request.PaymentLinkType;
        
        if (!string.IsNullOrEmpty(request.MerchantRemarks))
            data["MerchantRemarks"] = request.MerchantRemarks;
        
        if (!string.IsNullOrEmpty(request.NotifyType))
            data["NotifyType"] = request.NotifyType;
        
        if (!string.IsNullOrEmpty(request.Token))
            data["Token"] = request.Token;

        return data;
    }

    /// <summary>
    /// 签名请求数据
    /// </summary>
    private string SignRequestData(Dictionary<string, string> data)
    {
        // 使用商户证书对数据进行签名
        // 具体实现需要根据农行的签名算法

        try
        {
            // 加载商户证书（现在通过证书服务）
            if (_config.CertificatePaths.Count == 0 || _config.CertificatePasswords.Count == 0)
            {
                _logger.LogWarning("未配置商户证书");
                return JsonSerializer.Serialize(data, AppJsonSerializerContext.Default.DictionaryStringString);
            }

            // 注意：实际使用时需要根据农行SDK的签名要求进行签名
            // 示例：使用证书服务签名
            var jsonData = JsonSerializer.Serialize(data, AppJsonSerializerContext.Default.DictionaryStringString);
            var dataBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
            
            // 使用商户证书签名（证书已在服务启动时加载）
            var signature = _certificateService.SignData(dataBytes);
            var signatureBase64 = Convert.ToBase64String(signature);
            
            _logger.LogDebug("请求数据签名完成，签名长度: {Length} 字节", signature.Length);
            
            if (_config.PrintLog)
            {
                _logger.LogDebug("请求数据: {Data}", jsonData);
                _logger.LogDebug("签名值: {Signature}", signatureBase64);
            }

            // TODO: 根据农行API要求，将签名附加到请求数据中
            // 例如：data["Signature"] = signatureBase64;

            return jsonData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "签名数据失败");
            throw;
        }
    }

    /// <summary>
    /// 发送请求到农行支付平台
    /// </summary>
    /// <param name="requestData">请求数据</param>
    /// <param name="useIEUrl">是否使用IE提交地址（页面支付专用）</param>
    private async Task<PaymentResponse> SendToAbcAsync(Dictionary<string, string> requestData, bool useIEUrl = false)
    {
        try
        {
            // 根据交易类型选择URL
            var urlPath = useIEUrl ? _config.IETrxUrlPath : _config.TrxUrlPath;
            var url = $"{_config.ConnectMethod}://{_config.ServerName}:{_config.ServerPort}{urlPath}";
            
            _logger.LogInformation("=== 农行支付请求开始 ===");
            _logger.LogInformation("目标URL: {Url}", url);
            _logger.LogInformation("IE模式: {UseIEUrl}", useIEUrl);
            _logger.LogInformation("服务器: {ServerName}:{ServerPort}", _config.ServerName, _config.ServerPort);
            
            HttpContent content;
            
            // 检查是否是嵌套的MSG格式（包含MSG键）
            if (requestData.ContainsKey("MSG"))
            {
                // MSG格式：直接发送JSON（农行V3.0.0格式）
                var msgJson = requestData["MSG"];
                _logger.LogInformation("📤 发送MSG格式 (JSON长度={Length})", msgJson.Length);
                _logger.LogDebug("MSG内容: {MSG}", msgJson.Length > 500 ? msgJson.Substring(0, 500) + "..." : msgJson);
                
                // 🔑 关键修复：使用 UTF-8 编码发送（与签名时的编码一致！）
                // 根据Demo反编译和测试，签名和发送都必须使用相同编码
                var encoding = Encoding.UTF8;
                var bytes = encoding.GetBytes(msgJson);
                _logger.LogInformation("请求体大小: {Size} 字节", bytes.Length);
                content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                {
                    CharSet = "UTF-8"
                };
            }
            else
            {
                // 扁平格式：使用Form表单
                _logger.LogInformation("请求参数: {Data}", JsonSerializer.Serialize(requestData));
                
                var logContent = new FormUrlEncodedContent(requestData);
                var formData = await logContent.ReadAsStringAsync();
                _logger.LogInformation("Form 表单数据: {FormData}", formData);
                
                content = new FormUrlEncodedContent(requestData);
            }
            
            _logger.LogInformation("🌐 发送HTTP POST请求...");
            var response = await _httpClient.PostAsync(url, content);
            _logger.LogInformation("📥 收到HTTP响应: {StatusCode}", response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("响应长度: {Length} 字符", responseContent.Length);
            _logger.LogInformation("响应内容: {Response}", responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent);
            _logger.LogInformation("=== 农行支付请求结束 ===");

            // 解析响应
            return ParseResponse(responseContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ HTTP请求失败: {Message}", ex.Message);
            _logger.LogError("完整异常信息: {Exception}", ex.ToString());
            
            // 特别记录SSL错误
            if (ex.InnerException != null)
            {
                _logger.LogError("内部异常: {InnerException}", ex.InnerException.ToString());
                
                // 检查是否是SSL相关错误
                if (ex.InnerException.Message.Contains("SSL") || 
                    ex.InnerException.Message.Contains("certificate") ||
                    ex.InnerException.Message.Contains("证书"))
                {
                    _logger.LogError("🔒 SSL证书错误详情:");
                    _logger.LogError("  - 服务器: {ServerName}:{ServerPort}", _config.ServerName, _config.ServerPort);
                    _logger.LogError("  - 协议: {ConnectMethod}", _config.ConnectMethod);
                    _logger.LogError("  - 客户端证书数量: {CertCount}", _config.CertificatePaths.Count);
                    _logger.LogError("  - TrustPay证书: {TrustPay}", _config.TrustPayCertPath);
                }
            }
            
            return new PaymentResponse
            {
                ResponseCode = "9998",
                ResponseMessage = $"网络错误: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送请求异常: {Message}", ex.Message);
            _logger.LogError("完整异常堆栈: {Exception}", ex.ToString());
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"系统错误: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 农行错误码映射表
    /// </summary>
    private static readonly Dictionary<string, string> AbcErrorCodeMapping = new()
    {
        ["0000"] = "交易成功",
        ["APE001"] = "系统错误，请稍后重试",
        ["APE002"] = "商户信息不存在，请检查商户号配置",
        ["APE003"] = "商户未开通此功能，请联系农行开通",
        ["APE004"] = "商户已停用，请联系农行",
        ["APE009"] = "请求报文格式错误，请检查必填字段",
        ["APE400"] = "签名验证失败，请检查证书配置",
        ["EUNKWN"] = "交易结果未知，请查询订单状态确认",
        ["E001"] = "订单不存在",
        ["E002"] = "订单已支付",
        ["E003"] = "订单已关闭",
        ["E004"] = "订单已退款",
        ["E005"] = "订单金额不符",
        ["E100"] = "支付方式不支持",
        ["E101"] = "支付渠道异常",
        ["E102"] = "支付超时",
        ["E200"] = "余额不足",
        ["E201"] = "超过限额"
    };

    /// <summary>
    /// 获取友好的错误消息
    /// </summary>
    private string GetFriendlyErrorMessage(string errorCode, string originalMessage)
    {
        if (AbcErrorCodeMapping.TryGetValue(errorCode, out var friendlyMsg))
        {
            return $"{friendlyMsg} ({errorCode})";
        }
        return $"{originalMessage} ({errorCode})";
    }

    /// <summary>
    /// 解析农行支付平台响应
    /// </summary>
    private PaymentResponse ParseResponse(string responseContent)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            var response = new PaymentResponse
            {
                RawResponse = responseContent
            };

            // 农行 V3.0.0 格式: {"MSG":{"Message":{"TrxResponse":{...}}}}
            // 或错误格式: {"MSG":{"Message":{"ReturnCode":"2302","ErrorMessage":"..."}}}
            if (root.TryGetProperty("MSG", out var msgElement) &&
                msgElement.TryGetProperty("Message", out var messageElement))
            {
                // 首先检查是否有直接的ReturnCode（错误响应格式）
                if (messageElement.TryGetProperty("ReturnCode", out var directReturnCode))
                {
                    var code = directReturnCode.GetString() ?? "9999";
                    response.ResponseCode = code;
                    
                    if (code != "0000")
                    {
                        var originalMsg = "未知错误";
                        if (messageElement.TryGetProperty("ErrorMessage", out var errorMsg))
                        {
                            originalMsg = errorMsg.GetString() ?? "未知错误";
                        }
                        response.ResponseMessage = GetFriendlyErrorMessage(code, originalMsg);
                    }
                    else
                    {
                        response.ResponseMessage = "交易成功";
                    }
                    
                    // 解析订单号
                    if (messageElement.TryGetProperty("OrderNo", out var orderNo))
                        response.OrderNo = orderNo.GetString();
                    
                    // 解析支付URL（页面支付成功时返回）
                    if (messageElement.TryGetProperty("PaymentURL", out var paymentURL))
                    {
                        response.PaymentURL = paymentURL.GetString();
                        _logger.LogInformation("页面支付URL: {PaymentURL}", response.PaymentURL);
                    }
                    
                    // 解析订单金额
                    if (messageElement.TryGetProperty("OrderAmount", out var orderAmount))
                        response.OrderAmount = orderAmount.GetString();
                    
                    _logger.LogInformation("解析农行响应成功: ReturnCode={Code}, Message={Message}", 
                        response.ResponseCode, response.ResponseMessage);
                    
                    return response;
                }
                
                // 解析 TrxResponse（成功交易的响应格式）
                if (messageElement.TryGetProperty("TrxResponse", out var trxResponse))
                {
                    if (trxResponse.TryGetProperty("ReturnCode", out var returnCode))
                    {
                        var code = returnCode.GetString() ?? "9999";
                        response.ResponseCode = code;
                        
                        // 0000 表示成功，其他都是错误
                        if (code != "0000")
                        {
                            var originalMsg = "未知错误";
                            if (trxResponse.TryGetProperty("ErrorMessage", out var errorMsg))
                            {
                                originalMsg = errorMsg.GetString() ?? "未知错误";
                            }
                            // 使用友好的错误消息
                            response.ResponseMessage = GetFriendlyErrorMessage(code, originalMsg);
                        }
                        else
                        {
                            response.ResponseMessage = "交易成功";
                        }
                    }
                    
                    // 解析订单号
                    if (trxResponse.TryGetProperty("OrderNo", out var orderNo))
                        response.OrderNo = orderNo.GetString();
                    
                    // 解析交易流水号
                    if (trxResponse.TryGetProperty("TrxId", out var trxId))
                        response.TrxId = trxId.GetString();
                    
                    // 解析支付状态
                    if (trxResponse.TryGetProperty("PayStatus", out var payStatus))
                        response.PayStatus = payStatus.GetString();
                    
                    // 解析二维码URL (微信支付)
                    if (trxResponse.TryGetProperty("QRCodeURL", out var qrCodeUrl))
                        response.QRCodeUrl = qrCodeUrl.GetString();
                    
                    // 🆕 解析一码多扫二维码URL
                    if (trxResponse.TryGetProperty("ScanPayQRURL", out var scanPayQRURL))
                    {
                        response.QRCodeUrl = scanPayQRURL.GetString();
                        _logger.LogInformation("一码多扫二维码URL: {QRCodeUrl}", response.QRCodeUrl);
                    }
                    
                    // 🆕 解析页面支付URL
                    if (trxResponse.TryGetProperty("PaymentURL", out var paymentURL))
                    {
                        response.PaymentURL = paymentURL.GetString();
                        _logger.LogInformation("页面支付URL: {PaymentURL}", response.PaymentURL);
                    }
                    
                    // 解析订单金额
                    if (trxResponse.TryGetProperty("OrderAmount", out var orderAmount))
                        response.OrderAmount = orderAmount.GetString();
                }
                
                _logger.LogInformation("解析农行响应成功: ReturnCode={Code}, Message={Message}", 
                    response.ResponseCode, response.ResponseMessage);
                
                return response;
            }

            // 兼容旧格式或其他格式
            if (root.TryGetProperty("ResponseCode", out var code2))
                response.ResponseCode = code2.GetString() ?? "9999";
            else if (root.TryGetProperty("RspCode", out var rspCode))
                response.ResponseCode = rspCode.GetString() ?? "9999";
            else
                response.ResponseCode = "9999";

            if (root.TryGetProperty("ResponseMessage", out var msg))
                response.ResponseMessage = msg.GetString() ?? "未知响应";
            else if (root.TryGetProperty("RspMsg", out var rspMsg))
                response.ResponseMessage = rspMsg.GetString() ?? "未知响应";
            else
                response.ResponseMessage = "未知响应";

            if (root.TryGetProperty("OrderNo", out var orderNo2))
                response.OrderNo = orderNo2.GetString();

            if (root.TryGetProperty("TrxId", out var trxId2))
                response.TrxId = trxId2.GetString();

            if (root.TryGetProperty("PayStatus", out var payStatus2))
                response.PayStatus = payStatus2.GetString();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析响应失败: {Response}", responseContent);
            return new PaymentResponse
            {
                ResponseCode = "9997",
                ResponseMessage = "响应解析失败",
                RawResponse = responseContent
            };
        }
    }

    /// <summary>
    /// 查询订单状态
    /// </summary>
    public async Task<PaymentResponse> QueryOrderAsync(string orderNo)
    {
        _logger.LogInformation("查询订单状态: OrderNo={OrderNo}", orderNo);

        var data = new Dictionary<string, string>
        {
            ["TrxType"] = "OrderQuery",
            ["OrderNo"] = orderNo,
            ["MerchantID"] = _config.MerchantIds.FirstOrDefault() ?? ""
        };

        return await SendToAbcAsync(data);
    }

    /// <summary>
    /// 处理微信支付请求
    /// </summary>
    /// <remarks>
    /// 通过农行综合收银台 API 进行微信支付
    /// 流程：
    /// 1. APP 调用此方法创建微信支付订单
    /// 2. 农行系统生成 prepay_id
    /// 3. 返回微信 SDK 所需的签名参数
    /// 4. APP 使用这些参数调用微信原生 SDK 发起支付
    /// </remarks>
    public async Task<PaymentResponse> ProcessWeChatPaymentAsync(PaymentRequest request)
    {
        try
        {
            _logger.LogInformation("开始处理微信支付请求: OrderNo={OrderNo}, Amount={Amount}, OpenId={OpenId}", 
                request.OrderNo, request.OrderAmount, request.OpenId);

            // 构建微信支付请求数据
            var requestData = BuildWeChatRequestData(request);

            // 发送到农行支付平台（使用 Form 表单格式）
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("微信支付请求完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理微信支付请求失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"系统错误: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建微信支付请求数据
    /// </summary>
    private Dictionary<string, string> BuildWeChatRequestData(PaymentRequest request)
    {
        // 构建嵌套的请求结构（农行V3.0.0格式）
        var trxRequest = new Dictionary<string, object>
        {
            // 交易类型
            ["TrxType"] = "EWalletPayReq",
            
            // 农行电子钱包支付固定参数
            ["PaymentType"] = "D",              // D=电子钱包支付（微信/支付宝）
            ["PaymentLinkType"] = "2",          // 2=被扫模式（用户扫商户二维码）
            
            // 订单基本信息
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.OrderAmount,
            ["OrderTime"] = request.OrderTime ?? DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["NotifyType"] = "1"                // 1=异步通知
        };

        // 添加可选字段
        if (!string.IsNullOrEmpty(request.OrderDesc))
            trxRequest["OrderDesc"] = request.OrderDesc;
        
        if (!string.IsNullOrEmpty(request.OrderValidTime))
            trxRequest["OrderValidTime"] = request.OrderValidTime;
        
        if (!string.IsNullOrEmpty(request.ProductName))
            trxRequest["ProductName"] = request.ProductName;
        
        if (!string.IsNullOrEmpty(request.ResultNotifyURL))
            trxRequest["ResultNotifyURL"] = request.ResultNotifyURL;
        
        if (!string.IsNullOrEmpty(request.MerchantRemarks))
            trxRequest["MerchantRemarks"] = request.MerchantRemarks;
        
        if (!string.IsNullOrEmpty(request.Token))
            trxRequest["Token"] = request.Token;

        // 添加微信支付特定字段
        if (!string.IsNullOrEmpty(request.OpenId))
            trxRequest["OpenId"] = request.OpenId;
        
        if (!string.IsNullOrEmpty(request.ClientIP))
            trxRequest["ClientIP"] = request.ClientIP;
        
        if (!string.IsNullOrEmpty(request.SceneInfo))
            trxRequest["SceneInfo"] = request.SceneInfo;
        
        if (!string.IsNullOrEmpty(request.GoodsId))
            trxRequest["GoodsId"] = request.GoodsId;
        
        if (request.GoodsQuantity.HasValue)
            trxRequest["GoodsQuantity"] = request.GoodsQuantity.Value.ToString();
        
        if (!string.IsNullOrEmpty(request.Attach))
            trxRequest["Attach"] = request.Attach;
        
        if (!string.IsNullOrEmpty(request.Detail))
            trxRequest["Detail"] = request.Detail;

        // 构建完整的消息结构（农行V3.0.0格式）
        var message = new Dictionary<string, object>
        {
            ["Version"] = "V3.0.0",
            ["Format"] = "JSON",
            ["Merchant"] = new Dictionary<string, string>
            {
                ["ECMerchantType"] = "EBUS",
                ["MerchantID"] = _config.MerchantIds.FirstOrDefault() ?? ""
            },
            ["TrxRequest"] = trxRequest
        };
        
        var msg = new Dictionary<string, object>
        {
            ["Message"] = message
        };
        
        // 序列化为JSON字符串，然后作为表单的MSG字段发送
        var jsonString = JsonSerializer.Serialize(msg, new JsonSerializerOptions 
        { 
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        });
        
        return new Dictionary<string, string>
        {
            ["MSG"] = jsonString
        };
    }

    /// <summary>
    /// 处理支付宝支付请求
    /// </summary>
    public async Task<PaymentResponse> ProcessAlipayPaymentAsync(AlipayQRCodeRequest request)
    {
        try
        {
            _logger.LogInformation("开始处理支付宝支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 构建支付宝支付请求数据
            var requestData = BuildAlipayRequestData(request);

            // 发送到农行支付平台
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("支付宝支付请求完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理支付宝支付请求失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"系统错误: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建支付宝支付请求数据（扫码支付 - 被扫模式）
    /// </summary>
    private Dictionary<string, string> BuildAlipayRequestData(AlipayQRCodeRequest request)
    {
        // 构建嵌套的请求结构（农行V3.0.0格式）
        var trxRequest = new Dictionary<string, object>
        {
            // 交易类型
            ["TrxType"] = "EWalletPayReq",
            
            // 农行电子钱包支付固定参数
            ["PaymentType"] = "D",              // D=电子钱包支付（微信/支付宝）
            ["PaymentLinkType"] = "2",          // 2=被扫模式（用户扫商户二维码）
            
            // 订单基本信息
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.Amount.ToString("F2"),
            ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["NotifyType"] = "1",               // 1=异步通知
            ["OrderDesc"] = request.GoodsName ?? "商品购买"  // 订单详情（必填）
        };

        // 添加必填和可选字段
        if (!string.IsNullOrEmpty(request.GoodsName))
            trxRequest["ProductName"] = request.GoodsName;
        
        if (!string.IsNullOrEmpty(request.NotifyUrl))
            trxRequest["ResultNotifyURL"] = request.NotifyUrl;
        
        if (!string.IsNullOrEmpty(request.ReturnUrl))
            trxRequest["ReturnURL"] = request.ReturnUrl;
        
        if (!string.IsNullOrEmpty(request.ExpiredDate))
            trxRequest["OrderValidTime"] = request.ExpiredDate;
        
        if (!string.IsNullOrEmpty(request.Attach))
            trxRequest["MerchantRemarks"] = request.Attach;
        
        if (!string.IsNullOrEmpty(request.LimitPay))
            trxRequest["LimitPay"] = request.LimitPay;

        return BuildV3Message(request.MerchantId, trxRequest);
    }

    /// <summary>
    /// 处理支付宝WAP支付请求
    /// </summary>
    public async Task<PaymentResponse> ProcessAlipayWapPaymentAsync(AlipayWapRequest request)
    {
        try
        {
            _logger.LogInformation("开始处理支付宝WAP支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            // 构建支付宝WAP支付请求数据
            var requestData = BuildAlipayWapRequestData(request);

            // 发送到农行支付平台
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("支付宝WAP支付请求完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理支付宝WAP支付请求失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"系统错误: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建支付宝WAP支付请求数据
    /// </summary>
    private Dictionary<string, string> BuildAlipayWapRequestData(AlipayWapRequest request)
    {
        var trxRequest = new Dictionary<string, object>
        {
            ["TrxType"] = "EWalletPayReq",
            ["PaymentType"] = "D",              // D=电子钱包支付
            ["PaymentLinkType"] = "1",          // 1=主扫模式（跳转到支付宝页面）
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.Amount.ToString("F2"),
            ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["NotifyType"] = "1",
            ["OrderDesc"] = request.GoodsName ?? "商品购买"
        };

        if (!string.IsNullOrEmpty(request.GoodsName))
            trxRequest["ProductName"] = request.GoodsName;
        
        if (!string.IsNullOrEmpty(request.NotifyUrl))
            trxRequest["ResultNotifyURL"] = request.NotifyUrl;
        
        if (!string.IsNullOrEmpty(request.ReturnUrl))
            trxRequest["ReturnURL"] = request.ReturnUrl;
        
        if (!string.IsNullOrEmpty(request.QuitUrl))
            trxRequest["QuitURL"] = request.QuitUrl;

        return BuildV3Message(request.MerchantId, trxRequest);
    }

    /// <summary>
    /// 处理支付宝APP支付请求
    /// </summary>
    public async Task<PaymentResponse> ProcessAlipayAppPaymentAsync(AlipayAppRequest request)
    {
        try
        {
            _logger.LogInformation("开始处理支付宝APP支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            var requestData = BuildAlipayAppRequestData(request);
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("支付宝APP支付请求完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理支付宝APP支付请求失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"系统错误: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建支付宝APP支付请求数据
    /// </summary>
    private Dictionary<string, string> BuildAlipayAppRequestData(AlipayAppRequest request)
    {
        var trxRequest = new Dictionary<string, object>
        {
            ["TrxType"] = "EWalletPayReq",
            ["PaymentType"] = "D",
            ["PaymentLinkType"] = "3",          // 3=APP支付
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.Amount.ToString("F2"),
            ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["NotifyType"] = "1",
            ["OrderDesc"] = request.GoodsName ?? "商品购买"
        };

        if (!string.IsNullOrEmpty(request.GoodsName))
            trxRequest["ProductName"] = request.GoodsName;
        
        if (!string.IsNullOrEmpty(request.NotifyUrl))
            trxRequest["ResultNotifyURL"] = request.NotifyUrl;

        return BuildV3Message(request.MerchantId, trxRequest);
    }

    /// <summary>
    /// 处理支付宝PC网页支付请求
    /// </summary>
    public async Task<PaymentResponse> ProcessAlipayPCPaymentAsync(AlipayPCRequest request)
    {
        try
        {
            _logger.LogInformation("开始处理支付宝PC支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            var requestData = BuildAlipayPCRequestData(request);
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("支付宝PC支付请求完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理支付宝PC支付请求失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"系统错误: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建支付宝PC支付请求数据
    /// </summary>
    private Dictionary<string, string> BuildAlipayPCRequestData(AlipayPCRequest request)
    {
        var trxRequest = new Dictionary<string, object>
        {
            ["TrxType"] = "EWalletPayReq",
            ["PaymentType"] = "D",
            ["PaymentLinkType"] = "1",          // 1=主扫模式（PC和WAP都用1）
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.Amount.ToString("F2"),
            ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["NotifyType"] = "1",
            ["OrderDesc"] = request.GoodsName ?? "商品购买"
        };

        if (!string.IsNullOrEmpty(request.GoodsName))
            trxRequest["ProductName"] = request.GoodsName;
        
        if (!string.IsNullOrEmpty(request.NotifyUrl))
            trxRequest["ResultNotifyURL"] = request.NotifyUrl;
        
        if (!string.IsNullOrEmpty(request.ReturnUrl))
            trxRequest["ReturnURL"] = request.ReturnUrl;
        
        if (!string.IsNullOrEmpty(request.QuitUrl))
            trxRequest["QuitURL"] = request.QuitUrl;

        return BuildV3Message(request.MerchantId, trxRequest);
    }

    /// <summary>
    /// 处理支付宝付款码支付请求（主扫模式）
    /// </summary>
    public async Task<PaymentResponse> ProcessAlipayBarCodePaymentAsync(AlipayBarCodeRequest request)
    {
        try
        {
            _logger.LogInformation("开始处理支付宝付款码支付请求: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            var requestData = BuildAlipayBarCodeRequestData(request);
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("支付宝付款码支付请求完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理支付宝付款码支付请求失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"系统错误: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建支付宝付款码支付请求数据
    /// </summary>
    private Dictionary<string, string> BuildAlipayBarCodeRequestData(AlipayBarCodeRequest request)
    {
        var trxRequest = new Dictionary<string, object>
        {
            ["TrxType"] = "EWalletPayReq",
            ["PaymentType"] = "D",
            ["PaymentLinkType"] = "4",          // 4=付款码支付（主扫）
            ["PayQRCode"] = request.AuthCode,   // 用户的付款码
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.Amount.ToString("F2"),
            ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["NotifyType"] = "1",
            ["OrderDesc"] = request.GoodsName ?? "商品购买"
        };

        if (!string.IsNullOrEmpty(request.GoodsName))
            trxRequest["ProductName"] = request.GoodsName;
        
        if (!string.IsNullOrEmpty(request.NotifyUrl))
            trxRequest["ResultNotifyURL"] = request.NotifyUrl;
        
        if (!string.IsNullOrEmpty(request.Attach))
            trxRequest["MerchantRemarks"] = request.Attach;

        return BuildV3Message(request.MerchantId, trxRequest);
    }

    /// <summary>
    /// 查询支付宝订单状态
    /// </summary>
    public async Task<PaymentResponse> QueryAlipayOrderAsync(string orderNo, string merchantId)
    {
        try
        {
            _logger.LogInformation("查询支付宝订单: OrderNo={OrderNo}", orderNo);

            var trxRequest = new Dictionary<string, object>
            {
                ["TrxType"] = "OrderQuery",
                ["OrderNo"] = orderNo,
                ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss")
            };

            var requestData = BuildV3Message(merchantId, trxRequest);
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("查询订单完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                orderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询订单失败: OrderNo={OrderNo}", orderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"查询失败: {ex.Message}",
                OrderNo = orderNo
            };
        }
    }

    /// <summary>
    /// 支付宝退款
    /// </summary>
    public async Task<PaymentResponse> RefundAlipayOrderAsync(AlipayRefundRequest request)
    {
        try
        {
            _logger.LogInformation("支付宝退款: OrderNo={OrderNo}, RefundAmount={RefundAmount}", 
                request.OrderNo, request.RefundAmount);

            var trxRequest = new Dictionary<string, object>
            {
                ["TrxType"] = "Refund",
                ["OrderNo"] = request.OrderNo,
                ["RefundAmount"] = request.RefundAmount.ToString("F2"),
                ["RefundReason"] = request.RefundReason ?? "用户申请退款",
                ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss")
            };

            var requestData = BuildV3Message(request.MerchantId, trxRequest);
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("退款完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退款失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"退款失败: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 支付宝订单预创建（ALI_PRECREATE - PayTypeID=4）
    /// </summary>
    public async Task<PaymentResponse> ProcessAlipayPrecreateAsync(AlipayPrecreateRequest request)
    {
        try
        {
            _logger.LogInformation("开始支付宝订单预创建: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            var requestData = BuildAlipayPrecreateRequestData(request);
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("订单预创建完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订单预创建失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"订单预创建失败: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建支付宝订单预创建请求数据（PayTypeID=4）
    /// </summary>
    private Dictionary<string, string> BuildAlipayPrecreateRequestData(AlipayPrecreateRequest request)
    {
        var trxRequest = new Dictionary<string, object>
        {
            ["TrxType"] = "EWalletPayReq",      // 电子钱包支付请求
            ["PayTypeID"] = "4",                // PayTypeID=4 表示 ALI_PRECREATE（订单预创建）
            ["PaymentType"] = "D",              // D=电子钱包支付
            ["PaymentLinkType"] = "2",          // 2=被扫模式（生成二维码供用户扫）
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.Amount.ToString("F2"),
            ["OrderTime"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["NotifyType"] = "1",               // 1=异步通知
            ["OrderDesc"] = request.GoodsName ?? "商品购买"
        };

        // 添加可选字段
        if (!string.IsNullOrEmpty(request.GoodsName))
            trxRequest["ProductName"] = request.GoodsName;
        
        if (!string.IsNullOrEmpty(request.NotifyUrl))
            trxRequest["ResultNotifyURL"] = request.NotifyUrl;
        
        if (!string.IsNullOrEmpty(request.ExpiredDate))
            trxRequest["OrderValidTime"] = request.ExpiredDate;
        
        if (!string.IsNullOrEmpty(request.LimitPay))
            trxRequest["LimitPay"] = request.LimitPay;
        
        if (!string.IsNullOrEmpty(request.Attach))
            trxRequest["MerchantRemarks"] = request.Attach;
        
        if (!string.IsNullOrEmpty(request.StoreId))
            trxRequest["StoreID"] = request.StoreId;
        
        if (!string.IsNullOrEmpty(request.TerminalId))
            trxRequest["TerminalID"] = request.TerminalId;

        return BuildV3Message(request.MerchantId, trxRequest);
    }

    /// <summary>
    /// 农行一码多扫线上扫码下单
    /// </summary>
    public async Task<PaymentResponse> ProcessAbcScanPayAsync(AbcScanPayRequest request)
    {
        try
        {
            _logger.LogInformation("开始农行一码多扫下单: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            var requestData = BuildAbcScanPayRequestData(request);
            var response = await SendToAbcAsync(requestData);

            _logger.LogInformation("农行一码多扫下单完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "农行一码多扫下单失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"一码多扫下单失败: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 农行页面支付下单
    /// </summary>
    public async Task<PaymentResponse> ProcessAbcPagePayAsync(AbcPagePayRequest request)
    {
        try
        {
            _logger.LogInformation("开始农行页面支付下单: OrderNo={OrderNo}, Amount={Amount}", 
                request.OrderNo, request.Amount);

            var requestData = BuildAbcPagePayRequestData(request);
            
            // 🔑 页面支付使用常规交易URL（与Demo一致：/ebus/ReceiveMerchantTrxReqServlet）
            // 注意：不是IE URL！Demo测试证明常规URL才是正确的
            var response = await SendToAbcAsync(requestData, useIEUrl: false);

            _logger.LogInformation("农行页面支付下单完成: OrderNo={OrderNo}, ResponseCode={ResponseCode}", 
                request.OrderNo, response.ResponseCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "农行页面支付下单失败: OrderNo={OrderNo}", request.OrderNo);
            return new PaymentResponse
            {
                ResponseCode = "9999",
                ResponseMessage = $"页面支付下单失败: {ex.Message}",
                OrderNo = request.OrderNo
            };
        }
    }

    /// <summary>
    /// 构建农行一码多扫请求数据
    /// </summary>
    private Dictionary<string, string> BuildAbcScanPayRequestData(AbcScanPayRequest request)
    {
        // ⚠️ 关键：Order对象（订单基本信息）- 严格按照Demo格式，包含所有字段（即使为空）
        var order = new Dictionary<string, string>
        {
            ["PayTypeID"] = request.PayTypeID ?? "ImmediatePay",      // 交易类型
            ["OrderNo"] = request.OrderNo,                            // 订单号（必填）
            ["ExpiredDate"] = request.ExpiredDate ?? "30",            // 订单保存时间（天）
            ["OrderAmount"] = request.Amount.ToString("F2"),          // 订单金额（必填）
            ["SubsidyAmount"] = "",                                   // 营销补贴金额（Demo有此字段，留空）
            ["Fee"] = "",                                             // 手续费金额（Demo有此字段，留空）
            ["AccountNo"] = "",                                       // 支付账户（Demo有此字段，留空）
            ["CurrencyCode"] = request.CurrencyCode ?? "156",         // 币种
            ["ReceiverAddress"] = request.ReceiverAddress ?? "北京",  // 收货地址（Demo有默认值）
            ["InstallmentMark"] = request.InstallmentMark ?? "0",     // 分期标识
            ["BuyIP"] = request.BuyIP ?? "",                          // 客户IP（Demo有此字段，留空）
            ["OrderDesc"] = request.GoodsName ?? "商品购买",          // 订单描述
            ["OrderURL"] = $"http://127.0.0.1/Merchant/MerchantQueryOrder.aspx?ON={request.OrderNo}&DetailQuery=1",
            ["OrderDate"] = DateTime.Now.ToString("yyyy/MM/dd"),      // 订单日期（必填）
            ["OrderTime"] = DateTime.Now.ToString("HH:mm:ss"),        // 订单时间（必填）
            ["orderTimeoutDate"] = DateTime.Now.AddDays(1).ToString("yyyyMMddHHmmss"),  // 订单有效期
            ["CommodityType"] = request.CommodityType ?? "0202"       // 商品种类（Demo用0202）
        };

        // 构建OrderItems数组 - Demo格式完整
        var orderItems = new[]
        {
            new Dictionary<string, string>
            {
                ["SubMerName"] = "测试二级商户1",                     // Demo有此字段
                ["SubMerId"] = "12345",                               // Demo有此字段
                ["SubMerMCC"] = "0000",                               // Demo有此字段
                ["SubMerchantRemarks"] = "测试",                      // Demo有此字段
                ["ProductID"] = "IP000001",                           // 商品代码
                ["ProductName"] = request.GoodsName ?? "商品",        // 商品名称
                ["UnitPrice"] = request.Amount.ToString("F2"),        // 商品总价
                ["Qty"] = "1",                                        // 商品数量
                ["ProductRemarks"] = request.GoodsName ?? "商品购买", // 商品备注
                ["ProductType"] = "充值类",                           // Demo有此字段
                ["ProductDiscount"] = "0.9",                          // Demo有此字段
                ["ProductExpiredDate"] = "10"                         // Demo有此字段
            }
        };
        
        // 🔑 关键：TrxRequest严格按照Demo格式，包含所有字段（即使为空字符串）
        var trxRequest = new Dictionary<string, object>
        {
            // 交易类型：OLScanPayOrderReq（一码多扫线上扫码下单）
            ["TrxType"] = "OLScanPayOrderReq",
            
            // 支付方式配置 - 与Demo一致
            ["PaymentType"] = request.PaymentType ?? "1",             // 最新Demo用"1"
            ["PaymentLinkType"] = request.PaymentLinkType ?? "1",     // 1=internet
            
            // 以下字段Demo都有，即使为空也要包含
            ["ReceiveAccount"] = "",                                  // 收款方账号（Demo有此字段）
            ["ReceiveAccName"] = "",                                  // 收款方户名（Demo有此字段）
            
            // 通知配置
            ["NotifyType"] = request.NotifyType ?? "0",               // Demo用"0"
            ["ResultNotifyURL"] = request.NotifyUrl ?? "http://127.0.0.1/Merchant/MerchantResult.aspx",
            
            // 以下字段Demo都有，即使为空也要包含
            ["MerchantRemarks"] = request.MerchantRemarks ?? "",      // 附言
            ["OrderFrom"] = request.OrderFrom ?? "",                  // 订单来源
            ["ReceiveMark"] = "",                                     // 交易是否入二级商户账户
            ["ReceiveMerchantType"] = request.ReceiveMerchantType ?? "", // 收款方账户类型
            ["IsBreakAccount"] = request.IsBreakAccount ?? "0",       // 是否分账
            ["SplitAccTemplate"] = request.SplitAccTemplate ?? "",    // 分账模版编号
            
            // Demo还有这些字段
            ["VerifyFlag"] = "0",                                     // 验证标识
            ["VerifyType"] = "",                                      // 验证类型
            ["VerifyNo"] = "",                                        // 验证号码
            
            // 📦 Order对象（订单基本信息）- 包含OrderItems
            ["Order"] = new Dictionary<string, object>(order.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value))
            {
                ["OrderItems"] = orderItems                           // Demo用OrderItems不是OrderDetail
            }
        };

        return BuildV3Message(request.MerchantId, trxRequest);
    }

    /// <summary>
    /// 构建农行页面支付请求数据
    /// </summary>
    private Dictionary<string, string> BuildAbcPagePayRequestData(AbcPagePayRequest request)
    {
        // 构建Order对象（订单基本信息）- 严格按照官方Demo格式
        var order = new Dictionary<string, object>
        {
            ["PayTypeID"] = request.PayTypeID ?? "ImmediatePay",      // 交易类型
            ["OrderNo"] = request.OrderNo,                            // 订单号（必填）
            ["ExpiredDate"] = request.ExpiredDate ?? "30",            // 订单保存时间（天）
            ["OrderAmount"] = request.Amount.ToString("F2"),          // 订单金额（必填）
            ["SubsidyAmount"] = "",                                   // 补贴金额（官方demo有此字段）
            ["Fee"] = "",                                             // 手续费（官方demo有此字段）
            ["AccountNo"] = "",                                       // 账号（官方demo有此字段）
            ["CurrencyCode"] = request.CurrencyCode ?? "156",         // 币种
            ["ReceiverAddress"] = request.ReceiverAddress ?? "北京",  // 收货地址
            ["InstallmentMark"] = request.InstallmentMark ?? "0",     // 分期标识
            ["BuyIP"] = request.BuyIP ?? "",                          // 客户IP（官方demo留空）
            ["OrderDesc"] = request.GoodsName ?? "商品购买",          // 订单描述
            ["OrderURL"] = request.OrderURL ?? $"http://127.0.0.1/Merchant/MerchantQueryOrder.aspx?ON={request.OrderNo}&DetailQuery=1",
            ["OrderDate"] = DateTime.Now.ToString("yyyy/MM/dd"),      // 订单日期（必填）
            ["OrderTime"] = DateTime.Now.ToString("HH:mm:ss"),        // 订单时间（必填）
            ["orderTimeoutDate"] = request.OrderTimeoutDate ?? DateTime.Now.AddDays(1).ToString("yyyyMMddHHmmss"),
            ["CommodityType"] = request.CommodityType ?? "0201",      // 商品种类 (0201=实物类，0202=虚拟类)
            // OrderItems数组 - 官方Demo必须有
            ["OrderItems"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["SubMerName"] = "测试二级商户1",
                    ["SubMerId"] = "12345",
                    ["SubMerMCC"] = "0000",
                    ["SubMerchantRemarks"] = "测试",
                    ["ProductID"] = "IP000001",
                    ["ProductName"] = request.GoodsName ?? "商品",
                    ["UnitPrice"] = request.Amount.ToString("F2"),
                    ["Qty"] = "1",
                    ["ProductRemarks"] = request.GoodsName ?? "商品购买",
                    ["ProductType"] = "充值类",
                    ["ProductDiscount"] = "1.0",
                    ["ProductExpiredDate"] = "10"
                }
            }
        };

        // TrxRequest - 严格按照官方Demo格式
        var trxRequest = new Dictionary<string, object>
        {
            // 交易类型：PayReq（页面支付下单）
            ["TrxType"] = "PayReq",
            ["PaymentType"] = request.PaymentType ?? "1",             // 最新Demo用"1" (2026-01-21测试验证)
            ["PaymentLinkType"] = request.PaymentLinkType ?? "1",     // 1=internet
            ["ReceiveAccount"] = request.ReceiveAccount ?? "",        // 收款账号（可为空但必须发送）
            ["ReceiveAccName"] = request.ReceiveAccName ?? "",        // 收款户名（可为空但必须发送）
            ["NotifyType"] = "0",                                     // 官方Demo用"0"
            ["ResultNotifyURL"] = request.NotifyUrl,
            ["MerchantRemarks"] = request.MerchantRemarks ?? "",      // 附言
            ["OrderFrom"] = "",                                       // 官方Demo有此字段
            ["ReceiveMark"] = "",                                     // 官方Demo有此字段
            ["ReceiveMerchantType"] = "",                             // 官方Demo有此字段
            ["IsBreakAccount"] = request.IsBreakAccount ?? "0",       // 0=不分账
            ["SplitAccTemplate"] = "",                                // 官方Demo有此字段
            ["VerifyFlag"] = "0",                                     // 官方Demo有此字段
            ["VerifyType"] = "",                                      // 官方Demo有此字段
            ["VerifyNo"] = "",                                        // 官方Demo有此字段
            ["Order"] = order
        };

        return BuildV3Message(request.MerchantId, trxRequest);
    }

    /// <summary>
    /// 构建农行V3.0.0格式的完整消息（包含数字签名）
    /// 参照官方Demo格式：直接发送 {"Message":{...},"Signature-Algorithm":"SHA1withRSA","Signature":"..."}
    /// 不需要外层MSG包装
    /// </summary>
    private Dictionary<string, string> BuildV3Message(string? merchantId, Dictionary<string, object> trxRequest)
    {
        var message = new Dictionary<string, object>
        {
            ["Version"] = "V3.0.0",
            ["Format"] = "JSON",
            ["Merchant"] = new Dictionary<string, string>
            {
                ["ECMerchantType"] = "EBUS",
                ["MerchantID"] = merchantId ?? _config.MerchantIds.FirstOrDefault() ?? ""
            },
            ["TrxRequest"] = trxRequest
        };
        
        // 🔐 关键修复：签名内容是 message 本身，不包含 "Message" 包装！
        // Demo日志显示：签名前内容是 {"Version":"V3.0.0","Format":"JSON",...}
        // 而不是 {"Message":{"Version":"V3.0.0",...}}
        var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions 
        { 
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        });
        
        // 输出签名前的原始内容便于对比
        _logger.LogInformation("签名前的内容（无Message包装）: {MessageJson}", messageJson);
        Console.WriteLine($"=== 签名前的MSG内容（与Demo一致：无Message包装） ===");
        Console.WriteLine(messageJson);
        Console.WriteLine($"=== 签名前的MSG内容结束 ===");
        
        // 🔑 对 message（不带Message包装）进行SHA1withRSA签名
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);
        _logger.LogDebug("签名数据字节数: {ByteCount}", messageBytes.Length);
        
        var signature = _certificateService.SignData(messageBytes);
        var signatureBase64 = Convert.ToBase64String(signature);
        
        _logger.LogDebug("消息签名完成: 签名算法=SHA1withRSA, 签名长度={Length}", signature.Length);
        
        // 构建发送格式（与Demo完全一致）：{"Message":{...},"Signature-Algorithm":"...","Signature":"..."}
        var signedMsg = new Dictionary<string, object>
        {
            ["Message"] = message,
            ["Signature-Algorithm"] = "SHA1withRSA",
            ["Signature"] = signatureBase64
        };
        
        var jsonString = JsonSerializer.Serialize(signedMsg, new JsonSerializerOptions 
        { 
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        });
        
        // 输出完整MSG便于调试对比
        _logger.LogInformation("生成的完整MSG报文: {MSG}", jsonString);
        Console.WriteLine($"=== 签名后的完整MSG报文 ===");
        Console.WriteLine(jsonString);
        Console.WriteLine($"=== 签名后的完整MSG报文结束 ===");
        
        return new Dictionary<string, string>
        {
            ["MSG"] = jsonString
        };
    }
}

