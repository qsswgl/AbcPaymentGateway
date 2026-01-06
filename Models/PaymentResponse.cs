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
    /// 原始JSON响应
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => ResponseCode == "0000" || ResponseCode == "00";
}
