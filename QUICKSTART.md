# 快速开始指南

本指南帮助你快速部署和使用农行支付网关 API。

## 📋 清单

部署前，请确保准备好：

- ✅ .NET 10 SDK 已安装（本地和服务器）
- ✅ Docker 和 Docker Compose 已安装（服务器）
- ✅ 农行商户证书（.pfx 文件）
- ✅ 农行支付平台证书（TrustPay.cer）
- ✅ SSH 访问权限到服务器
- ✅ 域名 DNS 解析已配置

## 🚀 快速部署（3 步完成）

### 步骤 1: 配置证书和密码

1. 将农行证书复制到项目的 `cert` 目录：
```
AbcPaymentGateway/
  cert/
    prod/
      103881636900016.pfx    (你的生产证书)
      TrustPay.cer            (农行平台证书)
    test/
      103881636900016.pfx    (你的测试证书)
      abc.truststore
```

2. 编辑 `appsettings.json`，修改以下配置：
```json
{
  "AbcPayment": {
    "MerchantIds": ["你的商户ID"],
    "CertificatePaths": ["./cert/prod/你的证书.pfx"],
    "CertificatePasswords": ["你的证书密码"]
  }
}
```

### 步骤 2: 本地测试

```powershell
# 进入项目目录
cd K:\payment\AbcPaymentGateway

# 构建项目
dotnet build

# 运行项目
dotnet run

# 测试健康检查
# 在浏览器打开: http://localhost:5000/api/payment/health
```

### 步骤 3: 部署到服务器

**方式 A - 使用自动部署脚本（推荐）**:

```powershell
cd K:\payment\AbcPaymentGateway
.\deploy.ps1
```

**方式 B - 手动部署**:

```powershell
# 1. 上传证书到服务器
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r K:\payment\综合收银台接口包_V3.3.3软件包\cert root@api.qsgl.net:/opt/certs/

# 2. 上传项目文件
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net "mkdir -p /opt/payment"
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r * root@api.qsgl.net:/opt/payment/

# 3. SSH 登录服务器并部署
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net

cd /opt/payment

# 更新 docker-compose.yml 中的证书路径
# 修改: - /opt/certs:/app/cert:ro

docker-compose up -d --build

# 查看日志
docker logs -f payment-gateway
```

## ✅ 验证部署

### 1. 检查容器状态
```bash
docker ps | grep payment
```

预期输出：
```
CONTAINER ID   IMAGE                    STATUS         PORTS      NAMES
xxxxxxxxxx     payment_payment          Up 2 minutes   8080/tcp   payment-gateway
```

### 2. 测试健康检查
```bash
curl http://localhost:8080/api/payment/health
```

预期输出：
```json
{
  "status": "healthy",
  "timestamp": "2026-01-06T...",
  "service": "ABC Payment Gateway"
}
```

### 3. 测试外部访问
```bash
curl https://payment.qsgl.net/api/payment/health
```

## 📱 移动端集成

### Android 示例

```kotlin
// 创建支付
val paymentService = PaymentClient.api
val request = PaymentRequest(
    orderNo = "ORDER${System.currentTimeMillis()}",
    orderAmount = "1000",
    payQRCode = "用户扫码内容",
    resultNotifyURL = "https://your-app.com/callback"
)
val response = paymentService.createQRCodePayment(request)
```

### iOS 示例

```swift
PaymentService.shared.createQRCodePayment(
    orderNo: "ORDER\(Date().timeIntervalSince1970)",
    amount: "1000",
    qrCode: "用户扫码内容"
) { result in
    // 处理结果
}
```

详细示例请查看 [API_EXAMPLES.md](API_EXAMPLES.md)

## 🔍 常见问题

### Q1: 容器启动失败？

**检查**:
```bash
docker logs payment-gateway
```

**常见原因**:
- 证书路径不正确
- 证书密码错误
- 端口被占用

### Q2: Traefik 无法访问？

**检查**:
```bash
# 检查 Traefik 是否运行
docker ps | grep traefik

# 检查网络
docker network ls | grep traefik

# 检查域名解析
nslookup payment.qsgl.net
```

### Q3: 支付接口调用失败？

**检查**:
- 商户证书是否正确
- 网络是否可达农行服务器
- 查看应用日志

## 📁 项目结构

```
AbcPaymentGateway/
├── Controllers/           # API 控制器
│   └── PaymentController.cs
├── Models/               # 数据模型
│   ├── PaymentRequest.cs
│   ├── PaymentResponse.cs
│   └── AbcPaymentConfig.cs
├── Services/             # 业务服务
│   └── AbcPaymentService.cs
├── cert/                 # 证书目录（不提交到 Git）
├── logs/                 # 日志目录
├── Dockerfile           # Docker 构建文件
├── docker-compose.yml   # Docker Compose 配置
├── appsettings.json     # 应用配置
└── Program.cs           # 程序入口
```

## 📚 文档

- [README.md](README.md) - 项目概述
- [DEPLOYMENT.md](DEPLOYMENT.md) - 详细部署文档
- [API_EXAMPLES.md](API_EXAMPLES.md) - API 使用示例

## 🔧 维护命令

```bash
# 查看日志
docker logs -f payment-gateway

# 重启服务
docker-compose restart

# 停止服务
docker-compose down

# 更新部署
docker-compose up -d --build

# 清理旧镜像
docker image prune -f
```

## 🆘 获取帮助

如遇到问题：

1. 查看应用日志: `docker logs payment-gateway`
2. 查看 Traefik 日志: `docker logs traefik`
3. 检查证书配置
4. 查阅详细文档
5. 联系技术支持

## 🎯 下一步

部署成功后：

1. ✅ 在测试环境测试所有接口
2. ✅ 配置监控和告警
3. ✅ 设置日志备份
4. ✅ 编写移动端集成代码
5. ✅ 进行压力测试

---

祝部署顺利！🎉
