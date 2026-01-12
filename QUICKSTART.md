# 🚀 快速开始部署指南

## 概览

新的**本地构建 + 远程部署方案**已部署完毕。无需再依赖 GitHub Actions SSH 认证问题，直接从本地通过 SSH/SCP 上传镜像到腾讯云服务器。

## 前置条件（已验证）

✅ SSH 私钥: `K:\Key\tx.qsgl.net_id_ed25519`  
✅ 网络连接: 可达 `tx.qsgl.net`  
✅ 远程服务器: 已装有 Docker & Docker Compose  
✅ 部署目录: `/opt/payment-gateway` (已配置 Traefik)

## 📋 部署步骤

### 第 0 步：验证环境（可选但推荐）

```powershell
cd K:\payment\AbcPaymentGateway
.\check-deploy-env.ps1
```

应该看到：`Results: 7/7 passed` 和 `OK: Environment is ready for deployment!`

### 第 1 步：执行部署脚本

在项目根目录 (`K:\payment\AbcPaymentGateway`) 打开 PowerShell：

```powershell
.\build-and-deploy.ps1
```

**就这么简单！** 脚本将自动：

1. 🐳 **本地构建 Docker 镜像**
   - 编译 .NET 10 应用（Native AOT）
   - 预计时间：3-5 分钟（首次更长）

2. 📦 **打包镜像**
   - 导出为 `.tar.gz` 文件
   - 预计大小：100-200 MB

3. 📤 **上传到服务器**
   - 通过 SSH/SCP 上传到 `/tmp/`
   - 预计时间：1-3 分钟（取决于网速）

4. 🎯 **远程部署**
   - 加载镜像 → 重启容器 → 健康检查
   - 预计时间：30-60 秒

5. ✅ **完成**
   - 输出显示 "✅ 部署完成!" 和运行中的容器信息

## 📊 预期输出

```
========================================
Payment Gateway 本地构建与远程部署
========================================

[1/5] 检查必要条件...
✅ Docker 已安装
✅ SSH 私钥已找到

[2/5] 构建 Docker 镜像 (payment-gateway-jit)...
... 构建过程输出 ...
✅ Docker 镜像构建成功

[3/5] 导出镜像为 TAR 文件...
✅ 镜像已导出: payment-gateway-latest.tar.gz (大小: 145.23 MB)

[4/5] 上传镜像到远程服务器 (tx.qsgl.net)...
payment-gateway-latest.tar.gz          100%  145MB   5.2MB/s
✅ 镜像已上传到 /tmp/payment-gateway-latest.tar.gz

[5/5] 在远程服务器执行部署...
=== 开始远程部署 ===
步骤 1: 加载新镜像...
Loaded image: payment-gateway-jit:latest

步骤 2: 删除旧容器...
Removing payment-gateway ... done

步骤 3: 使用新镜像启动容器...
Creating payment-gateway ... done

步骤 4: 等待服务启动...

步骤 5: 健康检查...
{"status":"Healthy","timestamp":"2026-01-12T10:30:45Z"}
✅ 健康检查通过

步骤 6: 清理临时文件...

=== ✅ 部署成功! ===
CONTAINER ID   IMAGE                         COMMAND   CREATED   STATUS
a1b2c3d4e5f6   payment-gateway-jit:latest    ...       1m        Up 1m

✅ 部署完成!
服务地址: https://payment.qsgl.net
```

## 📱 验证部署

部署完成后，验证服务正常运行：

### 1️⃣ 查看运行状态

```powershell
# 连接到服务器查看容器
ssh -i "K:\Key\tx.qsgl.net_id_ed25519" root@tx.qsgl.net "docker ps | grep payment"
```

### 2️⃣ 访问 API 文档

```
https://payment.qsgl.net/swagger/
```

### 3️⃣ 检查健康状态

```powershell
Invoke-WebRequest -Uri "https://payment.qsgl.net/health" -UseBasicParsing
```

### 4️⃣ 查看应用日志

```powershell
ssh -i "K:\Key\tx.qsgl.net_id_ed25519" root@tx.qsgl.net "docker logs -f payment-gateway"
```

## ⚡ 高级用法

### 使用自定义参数

```powershell
.\build-and-deploy.ps1 `
  -RemoteHost "tx.qsgl.net" `
  -RemoteUser "root" `
  -RemotePort 22 `
  -RemoteDir "/opt/payment-gateway" `
  -SSHKeyPath "K:\Key\tx.qsgl.net_id_ed25519" `
  -ImageName "payment-gateway-jit" `
  -ImageTag "latest"
```

### 仅构建镜像（不部署）

```powershell
# 使用 Docker CLI 直接构建
docker build -t payment-gateway-jit:latest .

# 验证镜像
docker images | grep payment-gateway-jit
```

### 手动上传和部署

```powershell
# 步骤 1: 导出镜像
docker save payment-gateway-jit:latest | gzip > image.tar.gz

# 步骤 2: 上传
scp -i "K:\Key\tx.qsgl.net_id_ed25519" image.tar.gz root@tx.qsgl.net:/tmp/

# 步骤 3: 远程加载并重启
ssh -i "K:\Key\tx.qsgl.net_id_ed25519" root@tx.qsgl.net << 'EOF'
cd /opt/payment-gateway
docker load < /tmp/image.tar.gz
docker-compose down
docker-compose up -d
EOF
```

## 🔧 故障排查

| 问题 | 解决方案 |
|------|---------|
| `Docker not found` | 确保 Docker Desktop 已启动 |
| `SSH connection failed` | 检查网络和 SSH 私钥权限 |
| `Health check failed` | 运行 `docker logs payment-gateway` 查看错误 |
| `Permission denied` | 确保 SSH 私钥权限为 600：`chmod 600 K:\Key\*` |

## 📚 详细文档

完整的部署指南参考：`LOCAL_DEPLOY.md`

```powershell
# 在编辑器中打开
notepad .\LOCAL_DEPLOY.md
```

## 🎯 完整工作流（推荐）

1. **本地开发**
   ```powershell
   # 修改代码，git commit & push
   git add .
   git commit -m "feature: add new payment method"
   git push origin master
   ```

2. **GitHub Actions 通知** (自动触发)
   - 代码已推送到 GitHub
   - Actions 显示部署说明

3. **本地部署**
   ```powershell
   .\build-and-deploy.ps1
   ```

4. **验证部署**
   ```powershell
   Invoke-WebRequest https://payment.qsgl.net/health
   ```

## 📞 需要帮助？

- 查看脚本源代码：`build-and-deploy.ps1`
- 查看环境检查：`check-deploy-env.ps1`
- 查看完整文档：`LOCAL_DEPLOY.md`
- 查看 GitHub 工作流：`.github/workflows/auto-deploy.yml`

---

**就这么简单！使用 `.\build-and-deploy.ps1` 一键部署到生产环境！** 🚀- ✅ 农行商户证书（.pfx 文件）
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
