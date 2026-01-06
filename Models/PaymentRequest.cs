using System.ComponentModel.DataAnnotations;

namespace AbcPaymentGateway.Models;

/// <summary>
/// 支付请求模型
/// </summary>
public class PaymentRequest
{
    /// <summary>
    /// 交易类型 (EWalletPayReq: 电子钱包支付, UDCAppQRCodePayReq: 扫码支付)
    /// </summary>
    [Required]
    public string TrxType { get; set; } = "UDCAppQRCodePayReq";

    /// <summary>
    /// 订单号 (商户唯一订单号)
    /// </summary>
    [Required]
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 订单金额 (单位：分)
    /// </summary>
    [Required]
    public string OrderAmount { get; set; } = string.Empty;

    /// <summary>
    /// 订单描述
    /// </summary>
    public string? OrderDesc { get; set; }

    /// <summary>
    /// 订单有效时间 (格式：yyyyMMddHHmmss)
    /// </summary>
    public string? OrderValidTime { get; set; }

    /// <summary>
    /// 支付二维码内容 (微信、支付宝扫码内容)
    /// </summary>
    public string? PayQRCode { get; set; }

    /// <summary>
    /// 订单时间 (格式：yyyyMMddHHmmss)
    /// </summary>
    public string? OrderTime { get; set; }

    /// <summary>
    /// 订单摘要
    /// </summary>
    public string? OrderAbstract { get; set; }

    /// <summary>
    /// 结果通知URL
    /// </summary>
    public string? ResultNotifyURL { get; set; }

    /// <summary>
    /// 产品名称
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 支付类型
    /// </summary>
    public string? PaymentType { get; set; }

    /// <summary>
    /// 支付链接类型
    /// </summary>
    public string? PaymentLinkType { get; set; }

    /// <summary>
    /// 商户备注
    /// </summary>
    public string? MerchantRemarks { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public string? NotifyType { get; set; }

    /// <summary>
    /// Token令牌 (电子钱包支付使用)
    /// </summary>
    public string? Token { get; set; }
}
