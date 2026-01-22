# ABC Payment Gateway 备份摘要

**日期**: 2026年1月22日  
**状态**: ✅ 所有备份已完成

## 1. Docker镜像备份

### 备份镜像列表
```bash
# 查看镜像
docker images | grep abc-payment-gateway

abc-payment-gateway:stable-with-openssl-fix   # 稳定版本（已解决SSL问题）
abc-payment-gateway:working-20260122_111416   # 带时间戳的备份
```

### 镜像文件
- **位置**: `/root/backups/abc-payment-gateway-stable-openssl-fix.tar`
- **大小**: 224MB
- **包含**: 完整的.NET 10运行环境 + 应用程序 + 依赖项

### 导入镜像命令
```bash
# 在新服务器上导入
docker load -i abc-payment-gateway-stable-openssl-fix.tar
```

## 2. Git仓库备份

### 仓库信息
- **仓库**: https://github.com/qsswgl/AbcPaymentGateway
- **分支**: master
- **最新提交**: efb6059 - "Fix: Resolve OpenSSL legacy renegotiation issue"
- **提交时间**: 2026-01-22

### 提交内容
- 73个文件更改
- 18,426行新增代码
- 363行删除代码

### 主要更新
1. **核心修复**:
   - OpenSSL配置文件 (`runtimeconfig.template.json`)
   - 程序启动配置更新 (`Program.cs`)
   - SSL证书服务 (`Services/AbcCertificateService.cs`)

2. **新增功能**:
   - ABC支付控制器 (`Controllers/AbcPaymentController.cs`)
   - 支付宝控制器 (`Controllers/AlipayController.cs`)
   - 文件日志记录器 (`Logging/FileLogger.cs`)

3. **文档**:
   - 15+ Markdown文档
   - 测试报告和诊断信息
   - 部署指南

## 3. 关键配置文件

### OpenSSL配置 (`/opt/abc-payment/openssl_custom.conf`)
```conf
openssl_conf = openssl_init

[openssl_init]
ssl_conf = ssl_sect

[ssl_sect]
system_default = system_default_sect

[system_default_sect]
Options = UnsafeLegacyRenegotiation
MinProtocol = TLSv1
MaxProtocol = TLSv1.2
```

### 证书位置
- **商户证书**: `/opt/abc-payment/cert/103881636900016.pfx`
- **密码**: ay365365
- **TrustPay证书**: `/opt/abc-payment/cert/prod/TrustPay.cer`
- **根证书**: `/opt/abc-payment/cert/prod/abc.truststore`

## 4. 容器运行配置

### Docker运行命令
```bash
docker run -d --name abc-payment-gateway \
  --network traefik-net \
  --restart unless-stopped \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5000 \
  -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  -e OPENSSL_CONF=/opt/app/openssl_custom.conf \
  -v /opt/abc-payment:/opt/app \
  -w /opt/app \
  -l "traefik.enable=true" \
  -l "traefik.http.routers.abc-payment.rule=Host(\`payment.qsgl.net\`)" \
  -l "traefik.http.routers.abc-payment.entrypoints=websecure" \
  -l "traefik.http.routers.abc-payment.tls.certresolver=letsencrypt" \
  -l "traefik.http.services.abc-payment.loadbalancer.server.port=5000" \
  abc-payment-gateway:stable-with-openssl-fix \
  dotnet AbcPaymentGateway.dll
```

### 环境变量说明
- `ASPNETCORE_ENVIRONMENT`: Production
- `OPENSSL_CONF`: 指向自定义OpenSSL配置文件
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED`: 支持Traefik反向代理

## 5. 问题修复记录

### 问题描述
- **错误码**: EUNKWN / 9998
- **错误信息**: "The SSL connection could not be established"
- **详细错误**: `error:0A000152:SSL routines::unsafe legacy renegotiation disabled`

### 根本原因
OpenSSL 3.0默认禁用了不安全的遗留重新协商(unsafe legacy renegotiation)，而农行支付服务器需要此功能。

### 解决方案
1. 创建自定义OpenSSL配置文件
2. 启用`UnsafeLegacyRenegotiation`选项
3. 设置TLS协议范围：TLSv1 - TLSv1.2
4. 通过环境变量`OPENSSL_CONF`指定配置文件

### 验证结果
```json
{
  "isSuccess": true,
  "orderNo": "TEST1769051242678",
  "paymentURL": "https://pay.abchina.com/EbusPerbankFront/PaymentModeNewAct?TOKEN=...",
  "amount": 1,
  "status": "SUCCESS",
  "message": "交易成功",
  "errorCode": "0000"
}
```

## 6. 恢复步骤

### 完整恢复流程

#### Step 1: 恢复Docker镜像
```bash
# 下载镜像文件（如果需要）
scp -i K:\Key\tx.qsgl.net_id_ed25519 root@tx.qsgl.net:/root/backups/abc-payment-gateway-stable-openssl-fix.tar ./

# 导入镜像
docker load -i abc-payment-gateway-stable-openssl-fix.tar
```

#### Step 2: 准备证书和配置
```bash
# 确保证书目录存在
ls -lh /opt/abc-payment/cert/

# 确保OpenSSL配置存在
cat /opt/abc-payment/openssl_custom.conf
```

#### Step 3: 停止旧容器（如果存在）
```bash
docker stop abc-payment-gateway
docker rm abc-payment-gateway
```

#### Step 4: 启动新容器
使用上述Docker运行命令启动容器

#### Step 5: 验证服务
```bash
# 检查容器状态
docker ps | grep abc-payment

# 查看日志
docker logs abc-payment-gateway --tail 50

# 测试API
curl -X POST https://payment.qsgl.net/api/payment/abc/pagepay \
  -H "Content-Type: application/json" \
  -d '{"merchantId":"103881636900016","amount":1.00,"orderNo":"TEST12345","orderDesc":"测试","notifyUrl":"https://payment.qsgl.net/notify","merchantSuccessUrl":"https://payment.qsgl.net/success","merchantErrorUrl":"https://payment.qsgl.net/error"}'
```

## 7. Git克隆和部署

### 克隆仓库
```bash
git clone https://github.com/qsswgl/AbcPaymentGateway.git
cd AbcPaymentGateway
```

### 本地编译
```powershell
dotnet publish -c Release -o ./publish
```

### 部署到服务器
```powershell
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r ./publish/* root@tx.qsgl.net:/opt/abc-payment/
```

## 8. 监控和维护

### 日志位置
- **容器日志**: `docker logs abc-payment-gateway`
- **应用日志**: `/opt/abc-payment/logs/payment_YYYYMMDD.log`

### 健康检查
```bash
# 检查容器状态
docker ps | grep abc-payment-gateway

# 检查Traefik路由
curl -H "Host: payment.qsgl.net" http://localhost/api/health
```

### 定期备份建议
1. **每周**: 创建容器镜像快照
2. **每月**: 导出镜像文件到备份存储
3. **代码更新后**: 立即提交到Git并创建镜像备份

## 9. 相关链接

- **GitHub仓库**: https://github.com/qsswgl/AbcPaymentGateway
- **生产环境**: https://payment.qsgl.net
- **Swagger文档**: https://payment.qsgl.net/swagger

## 10. 联系信息

- **服务器**: tx.qsgl.net
- **SSH密钥**: K:\Key\tx.qsgl.net_id_ed25519
- **备份位置**: /root/backups/

---

**备份完成时间**: 2026-01-22 11:20  
**备份状态**: ✅ 全部成功  
**下次备份建议**: 2026-01-29（一周后）
