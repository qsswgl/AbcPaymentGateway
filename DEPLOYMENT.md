# 腾讯云 API 服务器自动化部署文档

本项目使用 .NET 10 Native AOT 技术构建高性能支付网关 API，并通过 Docker 容器化部署到腾讯云 API 服务器。

## 部署架构

```
本地开发环境 (Windows)
    ↓
PowerShell deploy.ps1 脚本
    ↓
SCP 文件上传 → 腾讯云 API 服务器 (/opt/payment-gateway)
    ↓
Docker Compose 构建 Native AOT 容器镜像
    ↓
Docker 容器启动 & 健康检查
    ↓
Traefik 反向代理 & HTTPS 路由
    ↓
https://payment.qsgl.net (对外服务)
```

## 快速部署

### 前置条件

1. **本地环境**
   - Windows PowerShell 5.1+
   - OpenSSH 客户端 (ssh, scp 命令)
   - SSH 密钥: K:\Key\tx.qsgl.net_id_ed25519

2. **远程服务器**
   - 腾讯云 API 服务器: api.qsgl.net
   - SSH 用户: root
   - Docker & Docker Compose 已安装
   - Traefik 网络: traefik-network 已存在

### 执行部署

在 PowerShell 中运行:

```powershell
cd K:\payment\AbcPaymentGateway
.\deploy.ps1
```

## 服务访问

**HTTPS 服务地址**: https://payment.qsgl.net
**健康检查端点**: https://payment.qsgl.net/health

## 容器配置

- 镜像: payment-gateway-aot:latest
- 容器名: payment-gateway
- 端口: 8080 (HTTP, Traefik 处理 HTTPS)
- 网络: traefik-network (Traefik 自动发现)

## 常用命令

### 查看日志
```bash
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net docker logs -f payment-gateway
```

### 重启容器
```bash
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net "cd /opt/payment-gateway && docker compose restart"
```

### 停止容器
```bash
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net "cd /opt/payment-gateway && docker compose down"
```
- **域名**: https://payment.qsgl.net

## 前置条件

### 服务器端准备

1. **确保已安装 Docker**
```bash
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net
docker --version
```

2. **确保已安装 Docker Compose**
```bash
docker-compose --version
```

3. **确保 Traefik 网络存在**
```bash
docker network ls | grep traefik
# 如果不存在，创建网络
docker network create traefik-network
```

4. **上传证书文件到服务器**

将农行证书上传到服务器：
```powershell
# 在本地 Windows 执行
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r K:\payment\综合收银台接口包_V3.3.3软件包\cert root@api.qsgl.net:/opt/certs/
```

## 部署方式

### 方式一：使用自动部署脚本（推荐）

在项目目录下运行 PowerShell 脚本：

```powershell
cd K:\payment\AbcPaymentGateway
.\deploy.ps1
```

脚本会自动完成：
- ✅ 本地构建测试
- ✅ 打包项目文件
- ✅ 上传到服务器
- ✅ 构建 Docker 镜像
- ✅ 启动容器
- ✅ 健康检查

### 方式二：手动部署

#### 步骤 1: 本地构建测试

```powershell
cd K:\payment\AbcPaymentGateway
dotnet build -c Release
```

#### 步骤 2: 上传项目到服务器

```powershell
# 创建远程目录
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net "mkdir -p /opt/payment"

# 上传项目文件（使用 scp）
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r * root@api.qsgl.net:/opt/payment/
```

#### 步骤 3: 在服务器上构建和运行

```bash
# SSH 登录到服务器
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net

# 进入项目目录
cd /opt/payment

# 更新证书路径配置
# 编辑 docker-compose.yml，确保证书路径正确

# 停止旧容器
docker-compose down

# 构建镜像
docker-compose build

# 启动容器
docker-compose up -d

# 查看日志
docker logs -f payment-gateway
```

#### 步骤 4: 验证部署

```bash
# 检查容器状态
docker ps | grep payment

# 测试健康检查
curl http://localhost:8080/api/payment/health

# 测试外部访问（需要等待 Traefik 配置生效）
curl https://payment.qsgl.net/api/payment/health
```

## Traefik 配置说明

Docker Compose 文件已经包含了必要的 Traefik 标签：

- ✅ 自动服务发现
- ✅ 自动 SSL 证书（Let's Encrypt）
- ✅ HTTP 到 HTTPS 重定向
- ✅ 域名路由 (payment.qsgl.net)

**重要**: 确保服务器上的 Traefik 容器正在运行：

```bash
docker ps | grep traefik
```

如果 Traefik 未运行，需要先启动 Traefik 服务。

## 配置生产环境

### 1. 修改配置文件

编辑 `appsettings.json` 设置生产环境配置：

```json
{
  "AbcPayment": {
    "MerchantIds": ["你的生产商户ID"],
    "CertificatePaths": ["./cert/prod/你的证书.pfx"],
    "CertificatePasswords": ["生产证书密码"],
    "TrustPayCertPath": "./cert/prod/TrustPay.cer",
    "ServerName": "pay.abchina.com",
    "IsTestEnvironment": false
  }
}
```

### 2. 使用环境变量（更安全）

在 `docker-compose.yml` 中添加环境变量：

```yaml
environment:
  - AbcPayment__MerchantIds__0=你的商户ID
  - AbcPayment__CertificatePasswords__0=你的证书密码
```

### 3. 配置证书路径

在 `docker-compose.yml` 中更新证书挂载路径：

```yaml
volumes:
  - /opt/certs:/app/cert:ro
  - ./logs:/app/logs
```

## 监控和维护

### 查看日志

```bash
# 实时日志
docker logs -f payment-gateway

# 最近 100 行
docker logs --tail 100 payment-gateway

# 应用日志
tail -f /opt/payment/logs/TrxLog.$(date +%Y%m%d).log
```

### 重启服务

```bash
cd /opt/payment
docker-compose restart
```

### 更新代码

```bash
cd /opt/payment

# 拉取新代码
git pull  # 如果使用 Git

# 或者重新上传文件
# scp -i K:\Key\tx.qsgl.net_id_ed25519 -r * root@api.qsgl.net:/opt/payment/

# 重新构建和部署
docker-compose down
docker-compose build
docker-compose up -d
```

### 备份和恢复

```bash
# 备份配置和日志
tar -czf payment-backup-$(date +%Y%m%d).tar.gz /opt/payment

# 恢复
tar -xzf payment-backup-YYYYMMDD.tar.gz -C /
```

## 故障排查

### 问题 1: 容器无法启动

```bash
# 查看详细日志
docker logs payment-gateway

# 检查配置
docker inspect payment-gateway

# 检查端口占用
netstat -tlnp | grep 8080
```

### 问题 2: Traefik 无法访问服务

```bash
# 检查 Traefik 日志
docker logs traefik

# 检查网络
docker network inspect traefik-network

# 检查标签
docker inspect payment-gateway | grep -A 20 Labels

# 测试内部访问
curl http://localhost:8080/api/payment/health
```

### 问题 3: SSL 证书问题

```bash
# 检查 Traefik ACME 日志
docker logs traefik | grep acme

# 检查域名解析
nslookup payment.qsgl.net

# 手动触发证书申请
# 删除容器重新创建
docker-compose down
docker-compose up -d
```

### 问题 4: 支付接口调用失败

```bash
# 检查农行证书
ls -l /app/cert/

# 检查应用日志
docker exec payment-gateway cat logs/TrxLog.$(date +%Y%m%d).log

# 测试网络连接
docker exec payment-gateway ping pay.abchina.com
```

## 性能优化

### 1. 调整日志级别

生产环境建议使用 `Information` 级别：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 2. 配置资源限制

在 `docker-compose.yml` 中添加：

```yaml
deploy:
  resources:
    limits:
      cpus: '1'
      memory: 512M
    reservations:
      cpus: '0.5'
      memory: 256M
```

### 3. 启用日志轮转

配置日志自动清理：

```bash
# 添加定时任务清理旧日志
crontab -e

# 每天凌晨 2 点清理 30 天前的日志
0 2 * * * find /opt/payment/logs -name "*.log" -mtime +30 -delete
```

## 安全建议

1. ✅ 使用防火墙限制访问
2. ✅ 定期更新 Docker 和系统
3. ✅ 使用强密码保护证书
4. ✅ 启用日志审计
5. ✅ 定期备份数据
6. ✅ 监控异常访问

## 联系支持

如遇到问题，请检查：
1. 应用日志
2. Docker 日志
3. Traefik 日志
4. 系统日志

必要时联系技术支持。
