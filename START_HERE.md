# 🚀 立即开始部署（Native AOT 高性能版本）

按照以下步骤，5分钟内完成 Native AOT 部署！

## ⚡ Native AOT 性能优势

| 指标 | 传统模式 | Native AOT | 提升 |
|------|---------|-----------|------|
| 启动速度 | 2-3秒 | 0.5-1秒 | **3-5倍** ⚡ |
| 内存占用 | 100MB | 40MB | **减少60%** 💾 |
| 镜像大小 | 200MB | 80MB | **减少60%** 📦 |

## 第一步：配置证书（2分钟）

### 1. 复制证书文件

将农行证书文件复制到项目目录：

```
K:\payment\AbcPaymentGateway\cert\
├── prod\
│   ├── 103881636900016.pfx       ← 你的生产证书
│   └── TrustPay.cer                ← 农行平台证书
└── test\
    ├── 103881636900016.pfx       ← 你的测试证书
    └── abc.truststore
```

💡 **提示**: 证书文件在 `K:\payment\综合收银台接口包_V3.3.3软件包\cert\` 目录

### 2. 修改配置文件

打开 `appsettings.json`，修改以下内容：

```json
{
  "AbcPayment": {
    "MerchantIds": ["你的商户ID"],              ← 改这里
    "CertificatePasswords": ["你的证书密码"]     ← 改这里
  }
}
```

## 第二步：本地测试（1分钟）

```powershell
# 在项目目录运行
cd K:\payment\AbcPaymentGateway

# 构建项目
dotnet build

# 运行项目
dotnet run

# 新开一个终端窗口，测试健康检查
curl http://localhost:5000/api/payment/health
```

✅ 看到 `"status": "healthy"` 表示成功！

## 第三步：部署到服务器（2分钟）

### 🎯 一键自动部署（推荐）⭐

```powershell
# 在项目目录运行 Native AOT 自动部署
cd K:\payment\AbcPaymentGateway
.\deploy.ps1
```

**部署脚本会自动完成：**
1. ✅ 本地构建验证
2. ✅ 上传证书到服务器
3. ✅ 上传项目代码
4. ✅ 构建 Native AOT 镜像（可能需要5-8分钟）
5. ✅ 启动容器（标签: payment）
6. ✅ 验证健康检查
7. ✅ 显示部署结果

⏱️ **首次部署时间**: 约 8-10 分钟（Native AOT 编译）
⏱️ **后续更新**: 约 3-5 分钟（Docker 缓存加速）

### 方式 B：手动部署

```powershell
# 1. 上传证书到服务器
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r K:\payment\综合收银台接口包_V3.3.3软件包\cert root@api.qsgl.net:/opt/certs/

# 2. 上传项目文件
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net "mkdir -p /opt/payment"
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r * root@api.qsgl.net:/opt/payment/

# 3. SSH 登录服务器
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net

# 4. 在服务器上执行 Native AOT 构建
cd /opt/payment
docker-compose build --no-cache  # Native AOT 构建
docker-compose up -d              # 启动容器

# 5. 查看日志
docker logs -f payment-gateway
```

## 第四步：验证部署（30秒）

### 在服务器上测试

```bash
# 测试内部访问
curl http://localhost:8080/api/payment/health

# 测试外部访问
curl https://payment.qsgl.net/api/payment/health
```

### 在本地浏览器测试

打开浏览器访问：
```
https://payment.qsgl.net/api/payment/health
```

✅ 看到 JSON 响应表示部署成功！

## 🎉 完成！

现在你的支付网关 API 已经运行在：

🌐 **https://payment.qsgl.net**

## 📱 移动端调用示例

### Android (Kotlin)

```kotlin
val response = PaymentClient.api.createQRCodePayment(
    PaymentRequest(
        orderNo = "ORDER001",
        orderAmount = "1000",
        payQRCode = "扫码内容"
    )
)
```

### iOS (Swift)

```swift
PaymentService.shared.createQRCodePayment(
    orderNo: "ORDER001",
    amount: "1000",
    qrCode: "扫码内容"
) { result in
    // 处理结果
}
```

## 📚 需要帮助？

查看详细文档：

- **快速开始**: [QUICKSTART.md](QUICKSTART.md)
- **API 示例**: [API_EXAMPLES.md](API_EXAMPLES.md)
- **部署指南**: [DEPLOYMENT.md](DEPLOYMENT.md)
- **检查清单**: [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md)

## ⚠️ 重要提醒

1. 部署前，务必修改 `appsettings.json` 中的证书密码
2. 确保证书文件路径正确
3. 生产环境使用生产证书和生产服务器地址
4. 定期查看日志，监控服务状态

## 🛠️ 常用命令

```bash
# 查看日志
docker logs -f payment-gateway

# 重启服务
docker-compose restart

# 停止服务
docker-compose down

# 更新代码后重新部署
docker-compose up -d --build
```

---

**现在就开始吧！** 🚀
