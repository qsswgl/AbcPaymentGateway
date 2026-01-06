# 农行支付网关 API (Native AOT 高性能版本)

基于 .NET 10 Native AOT 开发的农行综合收银台支付网关服务，支持移动端（Android/iOS）通过 API 调用农行支付功能。

## ⚡ Native AOT 性能优势

采用 .NET 10 Native AOT 编译，实现：
- **启动速度**: 提升 3-5 倍（< 1秒）⚡
- **内存占用**: 减少 60%（~40MB）💾
- **镜像大小**: 减少 60%（~80MB）�
- **执行效率**: 无 JIT 编译开销 🚀

## �📚 文档导航

- **[立即开始](START_HERE.md)** - 5分钟快速部署 ⭐
- **[Native AOT 指南](NATIVE_AOT.md)** - AOT 编译说明
- **[快速开始指南](QUICKSTART.md)** - 3 步完成部署
- **[详细部署文档](DEPLOYMENT.md)** - 完整部署说明
- **[API 使用示例](API_EXAMPLES.md)** - Android/iOS 集成代码
- **[部署检查清单](DEPLOYMENT_CHECKLIST.md)** - 上线前检查

## 🚀 一键部署

```powershell
# 1. 配置证书和密码（编辑 appsettings.json）
# 2. 本地测试
dotnet run

# 3. Native AOT 自动部署到腾讯云
.\deploy.ps1
```

详细步骤请查看 **[立即开始](START_HERE.md)**

## 功能特性

- ✅ 微信扫码支付
- ✅ 电子钱包支付
- ✅ 订单查询
- ✅ 支付回调处理
- ✅ 健康检查
- ✅ 跨平台支持（Android/iOS）
- ✅ Docker 容器化部署
- ✅ Traefik 反向代理集成

## 技术栈

- .NET 10
- ASP.NET Core Web API
- Docker
- Traefik (反向代理)

## API 接口

### 1. 扫码支付

**POST** `/api/payment/qrcode`

请求示例：
```json
{
  "orderNo": "202601060001",
  "orderAmount": "1000",
  "orderDesc": "商品购买",
  "payQRCode": "微信或支付宝二维码内容",
  "resultNotifyURL": "https://your-domain.com/callback"
}
```

### 2. 电子钱包支付

**POST** `/api/payment/ewallet`

请求示例：
```json
{
  "orderNo": "202601060002",
  "orderAmount": "2000",
  "orderDesc": "充值",
  "token": "用户Token",
  "productName": "余额充值",
  "resultNotifyURL": "https://your-domain.com/callback"
}
```

### 3. 查询订单

**GET** `/api/payment/query/{orderNo}`

### 4. 支付回调

**POST** `/api/payment/notify`

### 5. 健康检查

**GET** `/api/payment/health`

## 本地开发

### 前置条件

- .NET 10 SDK
- 农行商户证书（.pfx 格式）
- 农行支付平台证书

### 配置

1. 复制证书文件到 `cert` 目录：
```
cert/
  prod/
    TrustPay.cer
  test/
    103881636900016.pfx
    abc.truststore
```

2. 修改 `appsettings.json` 配置：
```json
{
  "AbcPayment": {
    "MerchantIds": ["你的商户ID"],
    "CertificatePaths": ["./cert/test/你的证书.pfx"],
    "CertificatePasswords": ["证书密码"]
  }
}
```

### 运行

```bash
# 恢复依赖
dotnet restore

# 运行应用
dotnet run

# 应用将在 http://localhost:5000 启动
```

## Docker 部署

### 本地构建测试

```bash
# 构建镜像
docker build -t abc-payment-gateway .

# 运行容器
docker run -d \
  -p 8080:8080 \
  -v $(pwd)/../综合收银台接口包_V3.3.3软件包/cert:/app/cert:ro \
  -v $(pwd)/logs:/app/logs \
  --name payment \
  abc-payment-gateway
```

### 使用 Docker Compose

```bash
# 启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

## 腾讯云服务器部署

### 准备工作

1. SSH 登录到服务器：
```bash
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net
```

2. 确保服务器已安装：
   - Docker
   - Docker Compose
   - Traefik (反向代理)

### 部署步骤

1. 上传项目文件到服务器：
```bash
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r ./AbcPaymentGateway root@api.qsgl.net:/opt/
```

2. 上传证书文件：
```bash
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r ./综合收银台接口包_V3.3.3软件包/cert root@api.qsgl.net:/opt/certs/
```

3. 在服务器上构建和运行：
```bash
cd /opt/AbcPaymentGateway
docker-compose up -d --build
```

4. 检查服务状态：
```bash
docker ps
docker logs payment-gateway
curl http://localhost:8080/api/payment/health
```

### Traefik 配置

Docker Compose 文件已包含 Traefik 标签配置，Traefik 会自动：
- 发现 `payment` 容器
- 配置域名 `payment.qsgl.net`
- 自动申请 Let's Encrypt SSL 证书
- 将 HTTP 重定向到 HTTPS
- 将请求代理到容器的 8080 端口

## 监控和维护

### 查看日志

```bash
# 应用日志
tail -f logs/TrxLog.$(date +%Y%m%d).log

# Docker 日志
docker logs -f payment-gateway
```

### 更新部署

```bash
# 拉取最新代码
git pull

# 重新构建和部署
docker-compose up -d --build

# 清理旧镜像
docker image prune -f
```

## 安全注意事项

1. ⚠️ 请妥善保管商户证书和密码
2. ⚠️ 不要将证书和密码提交到代码仓库
3. ⚠️ 生产环境请使用环境变量或密钥管理服务存储敏感信息
4. ⚠️ 定期更新证书和密码
5. ⚠️ 启用防火墙，只开放必要端口

## 故障排查

### 问题：容器无法启动

检查：
1. 日志输出：`docker logs payment-gateway`
2. 证书路径是否正确
3. 端口是否被占用

### 问题：Traefik 无法访问

检查：
1. Traefik 网络是否存在：`docker network ls`
2. 容器标签是否正确：`docker inspect payment-gateway`
3. DNS 解析是否正确：`nslookup payment.qsgl.net`

## 许可证

Copyright © 2026

## 联系方式

如有问题，请联系技术支持。
