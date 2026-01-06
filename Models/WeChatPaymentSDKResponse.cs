namespace AbcPaymentGateway.Models;

/// <summary>
/// 微信支付 SDK 响应模型
/// 用于返回给 APP 端，APP 端使用这些参数调用微信原生 SDK 发起支付
/// </summary>
public class WeChatPaymentSDKResponse
{
    /// <summary>
    /// 应用 ID（微信分配）- 农行聚合收银台的 AppID
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 当前时间戳（10位秒级时间戳）
    /// </summary>
    public string TimeStamp { get; set; } = string.Empty;

    /// <summary>
    /// 随机字符串，长度为32以内的字符串和数字组合
    /// </summary>
    public string NonceStr { get; set; } = string.Empty;

    /// <summary>
    /// 统一下单接口返回的 prepay_id 参数值
    /// 格式如：prepay_id=wx2017033010242291fcfe0db70013231072
    /// </summary>
    public string Package { get; set; } = string.Empty;

    /// <summary>
    /// 签名算法，暂支持 MD5、HMAC-SHA256、SHA256
    /// 如果为空，则默认为 MD5
    /// </summary>
    public string SignType { get; set; } = "MD5";

    /// <summary>
    /// 签名，使用字段 appId、timeStamp、nonceStr、package 按照微信签名算法生成
    /// </summary>
    public string PaySign { get; set; } = string.Empty;

    /// <summary>
    /// 订单号
    /// </summary>
    public string? OrderNo { get; set; }

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    public string? TrxId { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 支付金额（分为单位）
    /// </summary>
    public string? Amount { get; set; }

    /// <summary>
    /// 商品描述
    /// </summary>
    public string? GoodsDescription { get; set; }
}
