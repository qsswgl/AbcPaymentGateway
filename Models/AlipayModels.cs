namespace AbcPaymentGateway.Models;

/// <summary>
/// 支付宝扫码支付请求
/// </summary>
public class AlipayQRCodeRequest
{
    /// <summary>
    /// 商户订单号（必填）
    /// </summary>
    /// <example>ORD20260113001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额，单位：元（必填）
    /// </summary>
    /// <example>100.00</example>
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
    /// 支付回调地址（选填）
    /// </summary>
    /// <example>https://example.com/api/payment/notify</example>
    public string? NotifyUrl { get; set; }

    /// <summary>
    /// 支付完成返回地址（选填）
    /// </summary>
    /// <example>https://example.com/payment/result</example>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// 订单过期时间，单位：分钟（选填，默认30分钟）
    /// </summary>
    /// <example>30</example>
    public string? ExpiredDate { get; set; }

    /// <summary>
    /// 是否限制信用卡支付（选填，0=不限制，1=限制）
    /// </summary>
    /// <example>0</example>
    public string? LimitPay { get; set; }

    /// <summary>
    /// 子商户号（大商户模式使用）
    /// </summary>
    /// <example>12345</example>
    public string? ChildMerchantNo { get; set; }

    /// <summary>
    /// 附加数据（选填，在回调中原样返回）
    /// </summary>
    /// <example>custom_data_123</example>
    public string? Attach { get; set; }
}

/// <summary>
/// 支付宝扫码支付响应
/// </summary>
public class AlipayQRCodeResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    /// <example>ORD20260113001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    /// <example>ABC20260113123456001</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝二维码URL
    /// </summary>
    /// <example>https://qr.alipay.com/bax00000000000000000</example>
    public string QrCodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额
    /// </summary>
    /// <example>100.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态（PENDING=待支付，SUCCESS=已支付，FAILED=失败）
    /// </summary>
    /// <example>PENDING</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <example>支付订单创建成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 二维码过期时间
    /// </summary>
    /// <example>2026-01-13T15:30:00</example>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 错误代码（失败时返回）
    /// </summary>
    /// <example>PARAM_ERROR</example>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// 支付宝WAP支付请求
/// </summary>
public class AlipayWapRequest
{
    /// <summary>
    /// 商户订单号（必填）
    /// </summary>
    /// <example>ORD20260113002</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额，单位：元（必填）
    /// </summary>
    /// <example>88.88</example>
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
    /// 支付回调地址（选填）
    /// </summary>
    /// <example>https://example.com/api/payment/notify</example>
    public string? NotifyUrl { get; set; }

    /// <summary>
    /// 支付完成返回地址（必填）
    /// </summary>
    /// <example>https://example.com/payment/result</example>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// 用户中途退出返回地址（选填）
    /// </summary>
    /// <example>https://example.com</example>
    public string? QuitUrl { get; set; }

    /// <summary>
    /// 订单超时时间，格式：30m（选填）
    /// </summary>
    /// <example>30m</example>
    public string? TimeoutExpress { get; set; }
}

/// <summary>
/// 支付宝WAP支付响应
/// </summary>
public class AlipayWapResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    /// <example>ORD20260113002</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    /// <example>ABC20260113123456002</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// 支付页面URL（用户需跳转到此URL完成支付）
    /// </summary>
    /// <example>https://openapi.alipay.com/gateway.do?...</example>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额
    /// </summary>
    /// <example>88.88</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态
    /// </summary>
    /// <example>PENDING</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <example>支付订单创建成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误代码（失败时返回）
    /// </summary>
    /// <example>PARAM_ERROR</example>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// 支付宝APP支付请求
/// </summary>
public class AlipayAppRequest
{
    /// <summary>
    /// 商户订单号（必填）
    /// </summary>
    /// <example>ORD20260113003</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额，单位：元（必填）
    /// </summary>
    /// <example>199.99</example>
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
    /// 支付回调地址（选填）
    /// </summary>
    /// <example>https://example.com/api/payment/notify</example>
    public string? NotifyUrl { get; set; }

    /// <summary>
    /// 附加数据（选填）
    /// </summary>
    /// <example>custom_data_123</example>
    public string? Attach { get; set; }
}

/// <summary>
/// 支付宝APP支付响应
/// </summary>
public class AlipayAppResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    /// <example>ORD20260113003</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    /// <example>ABC20260113123456003</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// APP端调用支付宝SDK所需的订单字符串
    /// </summary>
    /// <example>app_id=2021000000000000&amp;biz_content=...</example>
    public string OrderString { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额
    /// </summary>
    /// <example>199.99</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态
    /// </summary>
    /// <example>PENDING</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <example>支付订单创建成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误代码（失败时返回）
    /// </summary>
    /// <example>PARAM_ERROR</example>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// 支付宝订单查询响应
/// </summary>
public class AlipayQueryResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    /// <example>ORD20260113001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    /// <example>ABC20260113123456001</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝订单号
    /// </summary>
    /// <example>2026011322001234567890123456</example>
    public string ThirdOrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额
    /// </summary>
    /// <example>100.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态（PENDING=待支付，SUCCESS=已支付，FAILED=失败，CLOSED=已关闭）
    /// </summary>
    /// <example>SUCCESS</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 支付完成时间
    /// </summary>
    /// <example>2026-01-13T14:30:00</example>
    public DateTime? PayTime { get; set; }

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <example>查询成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误代码（失败时返回）
    /// </summary>
    /// <example>ORDER_NOT_FOUND</example>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// 支付宝退款请求
/// </summary>
public class AlipayRefundRequest
{
    /// <summary>
    /// 商户订单号（必填）
    /// </summary>
    /// <example>ORD20260113001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 退款金额，单位：元（必填）
    /// </summary>
    /// <example>50.00</example>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 退款原因（选填）
    /// </summary>
    /// <example>用户申请退款</example>
    public string? RefundReason { get; set; }

    /// <summary>
    /// 商户ID（必填）
    /// </summary>
    /// <example>103881636900016</example>
    public string MerchantId { get; set; } = string.Empty;
}

/// <summary>
/// 支付宝退款响应
/// </summary>
public class AlipayRefundResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 原订单号
    /// </summary>
    /// <example>ORD20260113001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 退款流水号
    /// </summary>
    /// <example>RF20260113123456001</example>
    public string RefundNo { get; set; } = string.Empty;

    /// <summary>
    /// 退款金额
    /// </summary>
    /// <example>50.00</example>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 退款状态（SUCCESS=成功，PROCESSING=处理中，FAILED=失败）
    /// </summary>
    /// <example>SUCCESS</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <example>退款成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 退款完成时间
    /// </summary>
    /// <example>2026-01-13T15:00:00</example>
    public DateTime? RefundTime { get; set; }

    /// <summary>
    /// 错误代码（失败时返回）
    /// </summary>
    /// <example>REFUND_ERROR</example>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// 支付宝PC网页支付请求
/// </summary>
public class AlipayPCRequest
{
    /// <summary>
    /// 商户订单号（必填）
    /// </summary>
    /// <example>ORD20260113004</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额，单位：元（必填）
    /// </summary>
    /// <example>288.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 商户ID（必填）
    /// </summary>
    /// <example>103881636900016</example>
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// 商品名称/订单描述（必填）
    /// </summary>
    /// <example>电脑商品购买</example>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 支付回调地址（选填）
    /// </summary>
    /// <example>https://example.com/api/payment/notify</example>
    public string? NotifyUrl { get; set; }

    /// <summary>
    /// 支付完成返回地址（必填）
    /// </summary>
    /// <example>https://example.com/payment/result</example>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// 用户中途退出返回地址（选填）
    /// </summary>
    /// <example>https://example.com</example>
    public string? QuitUrl { get; set; }
}

/// <summary>
/// 支付宝PC网页支付响应
/// </summary>
public class AlipayPCResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    /// <example>ORD20260113004</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    /// <example>ABC20260113123456004</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// 支付页面URL（用户需跳转到此URL完成支付）
    /// </summary>
    /// <example>https://openapi.alipay.com/gateway.do?...</example>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额
    /// </summary>
    /// <example>288.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态
    /// </summary>
    /// <example>PENDING</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <example>支付订单创建成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误代码（失败时返回）
    /// </summary>
    /// <example>PARAM_ERROR</example>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// 支付宝付款码支付请求（商户扫用户付款码）
/// </summary>
public class AlipayBarCodeRequest
{
    /// <summary>
    /// 商户订单号（必填）
    /// </summary>
    /// <example>ORD20260113005</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额，单位：元（必填）
    /// </summary>
    /// <example>68.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 商户ID（必填）
    /// </summary>
    /// <example>103881636900016</example>
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// 商品名称/订单描述（必填）
    /// </summary>
    /// <example>线下扫码支付</example>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 用户付款码（必填，28开头的18位数字）
    /// </summary>
    /// <example>280123456789012345</example>
    public string AuthCode { get; set; } = string.Empty;

    /// <summary>
    /// 支付回调地址（选填）
    /// </summary>
    /// <example>https://example.com/api/payment/notify</example>
    public string? NotifyUrl { get; set; }

    /// <summary>
    /// 附加数据（选填）
    /// </summary>
    /// <example>store_001</example>
    public string? Attach { get; set; }
}

/// <summary>
/// 支付宝付款码支付响应
/// </summary>
public class AlipayBarCodeResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    /// <example>ORD20260113005</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    /// <example>ABC20260113123456005</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝订单号
    /// </summary>
    /// <example>2026011322001234567890123456</example>
    public string ThirdOrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额
    /// </summary>
    /// <example>68.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态（SUCCESS=支付成功，FAILED=支付失败，USERPAYING=用户支付中）
    /// </summary>
    /// <example>SUCCESS</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <example>支付成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 支付完成时间
    /// </summary>
    /// <example>2026-01-13T16:00:00</example>
    public DateTime? PayTime { get; set; }

    /// <summary>
    /// 错误代码（失败时返回）
    /// </summary>
    /// <example>AUTHCODE_INVALID</example>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// 支付宝订单预创建请求（ALI_PRECREATE）
/// </summary>
public class AlipayPrecreateRequest
{
    /// <summary>
    /// 商户订单号（必填）
    /// </summary>
    /// <example>ORD20260116001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额，单位：元（必填）
    /// </summary>
    /// <example>100.00</example>
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
    /// 支付回调地址（选填）
    /// </summary>
    /// <example>https://example.com/api/payment/notify</example>
    public string? NotifyUrl { get; set; }

    /// <summary>
    /// 订单过期时间，单位：分钟（选填，默认30分钟）
    /// </summary>
    /// <example>30</example>
    public string? ExpiredDate { get; set; }

    /// <summary>
    /// 是否限制信用卡支付（选填，0=不限制，1=限制）
    /// </summary>
    /// <example>0</example>
    public string? LimitPay { get; set; }

    /// <summary>
    /// 附加数据（选填，在回调中原样返回）
    /// </summary>
    /// <example>custom_data_123</example>
    public string? Attach { get; set; }

    /// <summary>
    /// 门店编号（选填）
    /// </summary>
    /// <example>STORE_001</example>
    public string? StoreId { get; set; }

    /// <summary>
    /// 终端号（选填）
    /// </summary>
    /// <example>TERM_001</example>
    public string? TerminalId { get; set; }
}

/// <summary>
/// 支付宝订单预创建响应
/// </summary>
public class AlipayPrecreateResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    /// <example>ORD20260116001</example>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 农行交易流水号
    /// </summary>
    /// <example>ABC20260116123456001</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝二维码URL（用户扫码支付）
    /// </summary>
    /// <example>https://qr.alipay.com/bax00000000000000000</example>
    public string QrCodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// 支付金额
    /// </summary>
    /// <example>100.00</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态（PENDING=待支付，SUCCESS=已支付，FAILED=失败）
    /// </summary>
    /// <example>PENDING</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <example>订单创建成功</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 二维码过期时间
    /// </summary>
    /// <example>2026-01-16T15:30:00</example>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 错误代码（失败时返回）
    /// </summary>
    /// <example>PARAM_ERROR</example>
    public string? ErrorCode { get; set; }
}
