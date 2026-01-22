namespace AbcPaymentGateway.Models;

/// <summary>
/// 农行页面支付下单请求
/// </summary>
public class AbcPagePayRequest
{
    /// <summary>
    /// 商户订单号（必填）
    /// </summary>
    /// <example>ORD20260119001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额，单位：元（必填）
    /// </summary>
    /// <example>10.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 商户ID（必填）
    /// </summary>
    /// <example>103881636900016</example>
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// 商品名称/订单描述（必填）
    /// </summary>
    /// <example>商品购买</example>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 支付回调地址（必填）
    /// </summary>
    /// <example>https://payment.qsgl.net/api/payment/notify</example>
    public string NotifyUrl { get; set; } = string.Empty;

    /// <summary>
    /// 商户成功返回URL（必填）
    /// </summary>
    /// <example>https://payment.qsgl.net/success</example>
    public string MerchantSuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// 商户失败返回URL（必填）
    /// </summary>
    /// <example>https://payment.qsgl.net/fail</example>
    public string MerchantErrorUrl { get; set; } = string.Empty;

    /// <summary>
    /// 交易类型（必填）
    /// ImmediatePay=即时支付
    /// </summary>
    /// <example>ImmediatePay</example>
    public string? PayTypeID { get; set; } = "ImmediatePay";

    /// <summary>
    /// 币种代码（156=人民币）
    /// </summary>
    /// <example>156</example>
    public string? CurrencyCode { get; set; } = "156";

    /// <summary>
    /// 商品种类（必填）
    /// 根据官方Demo: 0101=支付账户充值, 0201=虚拟类, 0202=传统类, 0203=实名类
    /// </summary>
    /// <example>0101</example>
    public string? CommodityType { get; set; } = "0101";

    /// <summary>
    /// 支付账户类型（必填）
    /// 1=农行借记卡, 3=农行贷记卡, A=支付方式合并
    /// 最新Demo使用"1" (2026-01-21测试验证)
    /// </summary>
    /// <example>1</example>
    public string? PaymentType { get; set; } = "1";

    /// <summary>
    /// 交易渠道（必填）
    /// 1=internet, 2=手机网络, 4=智能客户端
    /// </summary>
    /// <example>1</example>
    public string? PaymentLinkType { get; set; } = "1";

    /// <summary>
    /// 通知方式（必填）
    /// 0=URL页面通知, 1=服务器通知
    /// </summary>
    /// <example>1</example>
    public string? NotifyType { get; set; } = "1";

    /// <summary>
    /// 订单保存时间，单位：天（选填）
    /// </summary>
    /// <example>30</example>
    public string? ExpiredDate { get; set; } = "30";

    /// <summary>
    /// 分期标识（必填）
    /// 0=否, 1=分期
    /// </summary>
    /// <example>0</example>
    public string? InstallmentMark { get; set; } = "0";

    /// <summary>
    /// 交易是否分账（必填）
    /// 0=否, 1=是
    /// </summary>
    /// <example>0</example>
    public string? IsBreakAccount { get; set; } = "0";

    /// <summary>
    /// 收货地址（选填）
    /// </summary>
    /// <example>北京市朝阳区</example>
    public string? ReceiverAddress { get; set; }

    /// <summary>
    /// 客户交易IP（选填，根据官方Demo建议发送）
    /// </summary>
    /// <example>127.0.0.1</example>
    public string? BuyIP { get; set; }

    /// <summary>
    /// 收款账号（选填，根据官方Demo建议发送，即使为空也应发送）
    /// </summary>
    /// <example></example>
    public string? ReceiveAccount { get; set; }

    /// <summary>
    /// 收款户名（选填，根据官方Demo建议发送，即使为空也应发送）
    /// </summary>
    /// <example></example>
    public string? ReceiveAccName { get; set; }

    /// <summary>
    /// 订单超时时间（选填，格式yyyyMMddHHmmss）
    /// </summary>
    /// <example>20260120120000</example>
    public string? OrderTimeoutDate { get; set; }

    /// <summary>
    /// 订单查询URL（选填，根据官方Demo格式）
    /// </summary>
    /// <example>http://127.0.0.1/Merchant/MerchantQueryOrder.aspx?ON=xxx</example>
    public string? OrderURL { get; set; }

    /// <summary>
    /// 附言（选填，不超过100字符）
    /// </summary>
    /// <example>测试订单</example>
    public string? MerchantRemarks { get; set; }

    /// <summary>
    /// 订单来源（选填）
    /// 01=缴费, 02=开放银行, 03=消费商城
    /// </summary>
    /// <example>03</example>
    public string? OrderFrom { get; set; }

    /// <summary>
    /// 收款方账户类型（选填）
    /// 0=个人, 1=对公
    /// </summary>
    /// <example>1</example>
    public string? ReceiveMerchantType { get; set; }

    /// <summary>
    /// 分账模版编号（选填）
    /// </summary>
    public string? SplitAccTemplate { get; set; }
}

/// <summary>
/// 农行页面支付下单响应
/// </summary>
public class AbcPagePayResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    /// <example>ORD20260119001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    /// <example>ABC202601190001</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// 支付页面URL
    /// </summary>
    /// <example>https://pay.abchina.com/ebus/PaymentLink?id=xxx</example>
    public string PaymentURL { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额
    /// </summary>
    /// <example>10.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态
    /// SUCCESS=成功, FAILED=失败, UNKNOWN=未知
    /// </summary>
    /// <example>SUCCESS</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 响应消息
    /// </summary>
    /// <example>订单创建成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    /// <example>2026-01-19T12:00:00</example>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    /// <example>0000</example>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 农行返回码
    /// </summary>
    /// <example>0000</example>
    public string? ReturnCode { get; set; }
}
