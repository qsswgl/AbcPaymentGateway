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

    public AbcPaymentService(
        IOptions<AbcPaymentConfig> config,
        ILogger<AbcPaymentService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("AbcPayment");
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

            // 签名请求数据
            var signedData = SignRequestData(requestData);

            // 发送到农行支付平台
            var response = await SendToAbcAsync(signedData);

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
        // 这里需要使用商户证书对数据进行签名
        // 具体实现需要根据农行的签名算法
        // 这是一个简化的示例，实际项目中需要完整实现

        try
        {
            // 加载商户证书
            if (_config.CertificatePaths.Count == 0 || _config.CertificatePasswords.Count == 0)
            {
                _logger.LogWarning("未配置商户证书");
                return JsonSerializer.Serialize(data, AppJsonSerializerContext.Default.DictionaryStringString);
            }

            var certPath = _config.CertificatePaths[0];
            var certPassword = _config.CertificatePasswords[0];

            // 注意：实际使用时需要根据农行SDK的签名要求进行签名
            // 这里仅返回JSON格式的数据作为示例
            var jsonData = JsonSerializer.Serialize(data, AppJsonSerializerContext.Default.DictionaryStringString);
            
            if (_config.PrintLog)
            {
                _logger.LogDebug("请求数据: {Data}", jsonData);
            }

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
    private async Task<PaymentResponse> SendToAbcAsync(string signedData)
    {
        try
        {
            var url = $"{_config.ConnectMethod}://{_config.ServerName}:{_config.ServerPort}{_config.TrxUrlPath}";
            
            _logger.LogDebug("发送请求到: {Url}", url);

            var content = new StringContent(signedData, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("收到响应: {Response}", responseContent);

            // 解析响应
            return ParseResponse(responseContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP请求失败");
            return new PaymentResponse
            {
                ResponseCode = "9998",
                ResponseMessage = $"网络错误: {ex.Message}"
            };
        }
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

            // 尝试解析常见字段
            if (root.TryGetProperty("ResponseCode", out var code))
                response.ResponseCode = code.GetString() ?? "9999";
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

            if (root.TryGetProperty("OrderNo", out var orderNo))
                response.OrderNo = orderNo.GetString();

            if (root.TryGetProperty("TrxId", out var trxId))
                response.TrxId = trxId.GetString();

            if (root.TryGetProperty("PayStatus", out var payStatus))
                response.PayStatus = payStatus.GetString();

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

        var signedData = SignRequestData(data);
        return await SendToAbcAsync(signedData);
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

            // 签名请求数据
            var signedData = SignRequestData(requestData);

            // 发送到农行支付平台
            var response = await SendToAbcAsync(signedData);

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
        var data = new Dictionary<string, string>
        {
            ["TrxType"] = "WeChatAppPayReq",
            ["OrderNo"] = request.OrderNo,
            ["OrderAmount"] = request.OrderAmount,
            ["MerchantID"] = _config.MerchantIds.FirstOrDefault() ?? "",
            ["OrderTime"] = request.OrderTime ?? DateTime.Now.ToString("yyyyMMddHHmmss")
        };

        // 添加微信支付特定字段
        if (!string.IsNullOrEmpty(request.OpenId))
            data["OpenId"] = request.OpenId;
        
        if (!string.IsNullOrEmpty(request.ClientIP))
            data["ClientIP"] = request.ClientIP;
        
        if (!string.IsNullOrEmpty(request.SceneInfo))
            data["SceneInfo"] = request.SceneInfo;
        
        if (!string.IsNullOrEmpty(request.GoodsId))
            data["GoodsId"] = request.GoodsId;
        
        if (request.GoodsQuantity.HasValue)
            data["GoodsQuantity"] = request.GoodsQuantity.Value.ToString();
        
        if (!string.IsNullOrEmpty(request.Attach))
            data["Attach"] = request.Attach;
        
        if (!string.IsNullOrEmpty(request.Detail))
            data["Detail"] = request.Detail;

        // 添加公共字段
        if (!string.IsNullOrEmpty(request.OrderDesc))
            data["OrderDesc"] = request.OrderDesc;
        
        if (!string.IsNullOrEmpty(request.ProductName))
            data["ProductName"] = request.ProductName;
        
        if (!string.IsNullOrEmpty(request.ResultNotifyURL))
            data["ResultNotifyURL"] = request.ResultNotifyURL;
        
        if (!string.IsNullOrEmpty(request.OrderValidTime))
            data["OrderValidTime"] = request.OrderValidTime;

        return data;
    }
}
