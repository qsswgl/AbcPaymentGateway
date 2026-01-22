using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using AbcPaymentGateway.Models;

namespace AbcPaymentGateway.Services;

/// <summary>
/// 农行商户证书管理服务
/// </summary>
public interface IAbcCertificateService
{
    /// <summary>
    /// 获取商户证书
    /// </summary>
    X509Certificate2 GetMerchantCertificate(int index = 0);

    /// <summary>
    /// 获取TrustPay证书
    /// </summary>
    X509Certificate2? GetTrustPayCertificate();

    /// <summary>
    /// 使用商户证书签名数据
    /// </summary>
    byte[] SignData(byte[] data, int certificateIndex = 0);

    /// <summary>
    /// 验证签名
    /// </summary>
    bool VerifySignature(byte[] data, byte[] signature);

    /// <summary>
    /// 获取证书状态信息
    /// </summary>
    object GetCertificateStatus();
}

/// <summary>
/// 农行商户证书管理服务实现
/// </summary>
public class AbcCertificateService : IAbcCertificateService
{
    private readonly AbcPaymentConfig _config;
    private readonly ILogger<AbcCertificateService> _logger;
    private readonly Dictionary<int, X509Certificate2> _merchantCertificates;
    private X509Certificate2? _trustPayCertificate;

    public AbcCertificateService(
        IOptions<AbcPaymentConfig> config,
        ILogger<AbcCertificateService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _merchantCertificates = new Dictionary<int, X509Certificate2>();

        // 加载商户证书
        LoadMerchantCertificates();

        // 加载TrustPay证书
        LoadTrustPayCertificate();
    }

    /// <summary>
    /// 加载商户证书
    /// </summary>
    private void LoadMerchantCertificates()
    {
        _logger.LogInformation("=== 开始加载商户证书 ===");
        _logger.LogInformation("证书配置数量: {Count}", _config.CertificatePaths.Count);
        _logger.LogInformation("基础目录: AppContext.BaseDirectory={BaseDir}", AppContext.BaseDirectory);
        _logger.LogInformation("当前工作目录: Directory.GetCurrentDirectory={CurrentDir}", Directory.GetCurrentDirectory());
        
        for (int i = 0; i < _config.CertificatePaths.Count; i++)
        {
            try
            {
                var certPath = _config.CertificatePaths[i];
                var certPassword = i < _config.CertificatePasswords.Count 
                    ? _config.CertificatePasswords[i] 
                    : string.Empty;

                _logger.LogInformation("处理证书 [{Index}]: 配置路径={Path}", i, certPath);

                // 支持相对路径和绝对路径，增加多个路径尝试策略
                string fullPath;
                if (Path.IsPathRooted(certPath))
                {
                    fullPath = certPath;
                    _logger.LogInformation("  使用绝对路径: {FullPath}", fullPath);
                }
                else
                {
                    // 尝试多个可能的基础路径
                    var possiblePaths = new[]
                    {
                        Path.Combine(AppContext.BaseDirectory, certPath),
                        Path.Combine(Directory.GetCurrentDirectory(), certPath),
                        Path.Combine(Environment.CurrentDirectory, certPath),
                        certPath  // 相对于当前工作目录
                    };

                    _logger.LogInformation("  尝试路径:");
                    for (int j = 0; j < possiblePaths.Length; j++)
                    {
                        var exists = File.Exists(possiblePaths[j]);
                        _logger.LogInformation("    [{Idx}] {Path} -> {Exists}", j, possiblePaths[j], exists ? "存在" : "不存在");
                    }

                    fullPath = possiblePaths.FirstOrDefault(p => File.Exists(p)) 
                        ?? possiblePaths[0];
                    _logger.LogInformation("  选择路径: {FullPath}", fullPath);
                }

                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("❌ 商户证书文件不存在: {Path}", fullPath);
                    continue;
                }

                var fileInfo = new FileInfo(fullPath);
                _logger.LogInformation("✅ 找到证书文件: 大小={Size} 字节", fileInfo.Length);

                var certificate = new X509Certificate2(
                    fullPath,
                    certPassword,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
                );

                _merchantCertificates[i] = certificate;

                _logger.LogInformation(
                    "✅ 商户证书加载成功 [{Index}] - 主题: {Subject}, 序列号: {SerialNumber}, 有效期至: {NotAfter}",
                    i,
                    certificate.Subject,
                    certificate.SerialNumber,
                    certificate.NotAfter
                );

                // 验证证书是否过期
                if (certificate.NotAfter < DateTime.Now)
                {
                    _logger.LogWarning("⚠️  商户证书已过期: {NotAfter}", certificate.NotAfter);
                }
                else
                {
                    var daysRemaining = (certificate.NotAfter - DateTime.Now).Days;
                    _logger.LogInformation("证书有效期剩余: {Days} 天", daysRemaining);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 加载商户证书失败 (索引 {Index}): {Message}", i, ex.Message);
            }
        }

        if (_merchantCertificates.Count == 0)
        {
            _logger.LogWarning("❌ 没有成功加载任何商户证书");
        }
        else
        {
            _logger.LogInformation("✅ 成功加载 {Count} 个商户证书", _merchantCertificates.Count);
        }
        _logger.LogInformation("=== 商户证书加载完成 ===");
    }

    /// <summary>
    /// 加载TrustPay证书
    /// </summary>
    private void LoadTrustPayCertificate()
    {
        try
        {
            _logger.LogInformation("=== 开始加载TrustPay证书 ===");
            
            if (string.IsNullOrEmpty(_config.TrustPayCertPath))
            {
                _logger.LogWarning("未配置TrustPay证书路径");
                return;
            }

            _logger.LogInformation("配置的TrustPay证书路径: {Path}", _config.TrustPayCertPath);

            var fullPath = Path.IsPathRooted(_config.TrustPayCertPath)
                ? _config.TrustPayCertPath
                : Path.Combine(AppContext.BaseDirectory, _config.TrustPayCertPath);

            _logger.LogInformation("完整路径: {FullPath}", fullPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("❌ TrustPay证书文件不存在: {Path}", fullPath);
                return;
            }

            var fileInfo = new FileInfo(fullPath);
            _logger.LogInformation("✅ 找到TrustPay证书: 大小={Size} 字节", fileInfo.Length);

            _trustPayCertificate = new X509Certificate2(fullPath);

            _logger.LogInformation(
                "✅ TrustPay证书加载成功 - 主题: {Subject}, 有效期至: {NotAfter}",
                _trustPayCertificate.Subject,
                _trustPayCertificate.NotAfter
            );
            _logger.LogInformation("=== TrustPay证书加载完成 ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载TrustPay证书失败: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// 获取商户证书
    /// </summary>
    public X509Certificate2 GetMerchantCertificate(int index = 0)
    {
        if (!_merchantCertificates.TryGetValue(index, out var certificate))
        {
            throw new InvalidOperationException($"商户证书 (索引 {index}) 未加载或加载失败");
        }

        return certificate;
    }

    /// <summary>
    /// 获取TrustPay证书
    /// </summary>
    public X509Certificate2? GetTrustPayCertificate()
    {
        return _trustPayCertificate;
    }

    /// <summary>
    /// 加载truststore中的所有根证书到系统受信任存储
    /// </summary>
    public void LoadTrustStoreCertificates()
    {
        try
        {
            var certDir = Path.Combine(AppContext.BaseDirectory, "cert", "prod");
            
            // TrustPay是中间CA证书，应该放在中间证书颁发机构存储
            var trustPayPath = Path.Combine(certDir, "TrustPay.cer");
            if (File.Exists(trustPayPath))
            {
                using var intermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
                intermediateStore.Open(OpenFlags.ReadWrite);
                
                var trustPayCert = new X509Certificate2(trustPayPath);
                if (!intermediateStore.Certificates.Contains(trustPayCert))
                {
                    intermediateStore.Add(trustPayCert);
                    _logger.LogInformation("✅ 添加农行中间CA证书: {Subject}", trustPayCert.Subject);
                }
                intermediateStore.Close();
            }
            
            // truststore中的根证书
            var trustStoreCerts = new[]
            {
                "baltimore.cer",
                "digicert-g2.cer",
                "digicert-root.cer",
                "digicert-sha2.cer",  // 这个可能也是中间CA
                "verisign-g5.cer"
            };

            using var rootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            rootStore.Open(OpenFlags.ReadWrite);
            
            using var intermediateStore2 = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
            intermediateStore2.Open(OpenFlags.ReadWrite);

            foreach (var certFile in trustStoreCerts)
            {
                var certPath = Path.Combine(certDir, certFile);
                if (!File.Exists(certPath))
                {
                    _logger.LogWarning("根证书文件不存在: {Path}", certPath);
                    continue;
                }

                var cert = new X509Certificate2(certPath);
                
                // 判断是根证书还是中间CA（根证书的Issuer和Subject相同）
                bool isRootCA = cert.Issuer == cert.Subject;
                
                if (isRootCA)
                {
                    if (!rootStore.Certificates.Contains(cert))
                    {
                        rootStore.Add(cert);
                        _logger.LogInformation("✅ 添加根证书到受信任存储: {Subject}", cert.Subject);
                    }
                }
                else
                {
                    // 中间CA证书
                    if (!intermediateStore2.Certificates.Contains(cert))
                    {
                        intermediateStore2.Add(cert);
                        _logger.LogInformation("✅ 添加中间CA证书: {Subject}", cert.Subject);
                    }
                }
            }

            rootStore.Close();
            intermediateStore2.Close();
            _logger.LogInformation("✅ Truststore证书加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 加载truststore证书失败: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// 使用商户证书签名数据（农行V3.0.0要求SHA1withRSA）
    /// </summary>
    public byte[] SignData(byte[] data, int certificateIndex = 0)
    {
        var certificate = GetMerchantCertificate(certificateIndex);

        if (certificate.PrivateKey == null)
        {
            throw new InvalidOperationException("证书没有私钥，无法签名");
        }

        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa == null)
        {
            throw new InvalidOperationException("无法获取RSA私钥");
        }

        // 🔑 农行要求使用SHA1withRSA签名（不是SHA256！）
        var signature = rsa.SignData(
            data,
            System.Security.Cryptography.HashAlgorithmName.SHA1,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1
        );

        _logger.LogDebug("数据签名成功（SHA1withRSA），签名长度: {Length} 字节", signature.Length);

        return signature;
    }

    /// <summary>
    /// 验证签名（用于验证农行返回的数据）
    /// </summary>
    public bool VerifySignature(byte[] data, byte[] signature)
    {
        if (_trustPayCertificate == null)
        {
            _logger.LogWarning("TrustPay证书未加载，无法验证签名");
            return false;
        }

        try
        {
            using var rsa = _trustPayCertificate.GetRSAPublicKey();
            if (rsa == null)
            {
                _logger.LogError("无法获取TrustPay证书的RSA公钥");
                return false;
            }

            var isValid = rsa.VerifyData(
                data,
                signature,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1
            );

            _logger.LogDebug("签名验证结果: {IsValid}", isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证签名失败: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 获取证书状态信息
    /// </summary>
    public object GetCertificateStatus()
    {
        try
        {
            var basePath = AppContext.BaseDirectory;
            var certPath = Path.Combine(basePath, "cert");
            var prodCertPath = Path.Combine(certPath, "prod");
            var testCertPath = Path.Combine(certPath, "test");

            var merchantCertInfo = new List<object>();
            foreach (var kvp in _merchantCertificates)
            {
                var cert = kvp.Value;
                merchantCertInfo.Add(new
                {
                    index = kvp.Key,
                    subject = cert.Subject,
                    issuer = cert.Issuer,
                    thumbprint = cert.Thumbprint,
                    notBefore = cert.NotBefore,
                    notAfter = cert.NotAfter,
                    isExpired = DateTime.Now > cert.NotAfter,
                    daysUntilExpiry = (cert.NotAfter - DateTime.Now).Days,
                    hasPrivateKey = cert.HasPrivateKey,
                    serialNumber = cert.SerialNumber
                });
            }

            return new
            {
                basePath = basePath,
                certPath = certPath,
                environment = _config.Environment,
                paths = new
                {
                    certDirectory = Directory.Exists(certPath),
                    prodDirectory = Directory.Exists(prodCertPath),
                    testDirectory = Directory.Exists(testCertPath)
                },
                merchantCertificates = new
                {
                    count = _merchantCertificates.Count,
                    certificates = merchantCertInfo,
                    configuredPaths = _config.CertificatePaths
                },
                trustPayCertificate = _trustPayCertificate != null ? new
                {
                    subject = _trustPayCertificate.Subject,
                    issuer = _trustPayCertificate.Issuer,
                    thumbprint = _trustPayCertificate.Thumbprint,
                    notBefore = _trustPayCertificate.NotBefore,
                    notAfter = _trustPayCertificate.NotAfter,
                    isExpired = DateTime.Now > _trustPayCertificate.NotAfter,
                    serialNumber = _trustPayCertificate.SerialNumber
                } : null,
                certificateFiles = new
                {
                    prod = Directory.Exists(prodCertPath) ? Directory.GetFiles(prodCertPath).Select(f => new
                    {
                        name = Path.GetFileName(f),
                        size = new FileInfo(f).Length,
                        lastModified = new FileInfo(f).LastWriteTime
                    }).ToList() : (object)new List<object>(),
                    test = Directory.Exists(testCertPath) ? Directory.GetFiles(testCertPath).Select(f => new
                    {
                        name = Path.GetFileName(f),
                        size = new FileInfo(f).Length,
                        lastModified = new FileInfo(f).LastWriteTime
                    }).ToList() : (object)new List<object>()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取证书状态失败");
            return new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            };
        }
    }
}
