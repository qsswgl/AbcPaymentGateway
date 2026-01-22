namespace AbcPaymentGateway.Models;

/// <summary>
/// 支付响应模型
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// 响应码
    /// </summary>
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>
    /// 响应消息
    /// </summary>
    public string ResponseMessage { get; set; } = string.Empty;

    /// <summary>
    /// 订单号
    /// </summary>
    public string? OrderNo { get; set; }

    /// <summary>
    /// 交易流水号
    /// </summary>
    public string? TrxId { get; set; }

    /// <summary>
    /// 支付状态
    /// </summary>
    public string? PayStatus { get; set; }

    /// <summary>
    /// 二维码URL（微信/支付宝扫码支付）
    /// </summary>
    public string? QRCodeUrl { get; set; }

    /// <summary>
    /// 支付页面URL（页面支付）
    /// </summary>
    public string? PaymentURL { get; set; }

    /// <summary>
    /// 原始JSON响应
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// 第三方订单号（支付宝/微信订单号）
    /// </summary>
    public string? ThirdPartyOrderNo { get; set; }

    /// <summary>
    /// 订单金额
    /// </summary>
    public string? OrderAmount { get; set; }

    /// <summary>
    /// 支付时间
    /// </summary>
    public DateTime? PayTime { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => ResponseCode == "0000" || ResponseCode == "00";
}
