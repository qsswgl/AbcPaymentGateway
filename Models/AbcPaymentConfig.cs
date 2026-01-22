namespace AbcPaymentGateway.Models;

/// <summary>
/// 农行支付配置
/// </summary>
public class AbcPaymentConfig
{
    /// <summary>
    /// 商户ID列表
    /// </summary>
    public List<string> MerchantIds { get; set; } = new();

    /// <summary>
    /// 商户证书路径列表 (PFX格式)
    /// </summary>
    public List<string> CertificatePaths { get; set; } = new();

    /// <summary>
    /// 商户证书密码列表
    /// </summary>
    public List<string> CertificatePasswords { get; set; } = new();

    /// <summary>
    /// 支付平台证书路径
    /// </summary>
    public string TrustPayCertPath { get; set; } = string.Empty;

    /// <summary>
    /// 是否打印日志
    /// </summary>
    public bool PrintLog { get; set; } = true;

    /// <summary>
    /// 日志路径
    /// </summary>
    public string LogPath { get; set; } = "logs";

    /// <summary>
    /// 服务器超时时间（毫秒）
    /// </summary>
    public int Timeout { get; set; } = 30000;

    /// <summary>
    /// 支付服务器地址
    /// </summary>
    public string ServerName { get; set; } = "pay.abchina.com";

    /// <summary>
    /// 服务器端口
    /// </summary>
    public string ServerPort { get; set; } = "443";

    /// <summary>
    /// 连接方法 (https)
    /// </summary>
    public string ConnectMethod { get; set; } = "https";

    /// <summary>
    /// 交易URL路径
    /// </summary>
    public string TrxUrlPath { get; set; } = "/ebus/ReceiveMerchantTrxReqServlet";

    /// <summary>
    /// 页面支付URL路径 (IE提交地址)
    /// </summary>
    public string IETrxUrlPath { get; set; } = "/ebus/ReceiveMerchantIERequestServlet";

    /// <summary>
    /// 商户错误返回URL
    /// </summary>
    public string MerchantErrorUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否测试环境
    /// </summary>
    public bool IsTestEnvironment { get; set; } = false;

    /// <summary>
    /// 运行环境 (Production/Development/Test)
    /// </summary>
    public string Environment { get; set; } = "Production";
}
