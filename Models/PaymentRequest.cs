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

    /// <summary>
    /// 用户OpenID (微信支付使用，用于预签约)
    /// </summary>
    public string? OpenId { get; set; }

    /// <summary>
    /// 用户IP地址 (微信支付使用，用于风控)
    /// </summary>
    public string? ClientIP { get; set; }

    /// <summary>
    /// 场景信息 (微信支付使用，JSON格式，包含门店/收银机等信息)
    /// </summary>
    public string? SceneInfo { get; set; }

    /// <summary>
    /// 商品ID (微信支付使用)
    /// </summary>
    public string? GoodsId { get; set; }

    /// <summary>
    /// 商品数量 (微信支付使用)
    /// </summary>
    public int? GoodsQuantity { get; set; }

    /// <summary>
    /// 附加数据 (微信支付使用，会在回调中原样返回)
    /// </summary>
    public string? Attach { get; set; }

    /// <summary>
    /// 商品详情 (微信支付使用，JSON格式)
    /// </summary>
    public string? Detail { get; set; }
}
