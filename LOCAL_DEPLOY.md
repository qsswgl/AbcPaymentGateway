# 本地构建与远程部署指南

## 概述

采用**本地编译打包 + SSH/SCP 远程部署**方案，避免 GitHub Actions 中的 SSH 认证问题。

**工作流程**:
```
本地编译 Docker 镜像 
  ↓
导出为 TAR 文件
  ↓
SCP 上传到腾讯云服务器 (/tmp/)
  ↓
SSH 远程执行部署脚本
  ↓
加载新镜像 → 重启容器 → 健康检查
  ↓
✅ 部署完成
```

## 前置准备

### 1. 本地环境要求

- **Windows PowerShell 5.1+** 或 **PowerShell Core 7.0+**
- **Docker Desktop** 已安装并运行
- **SSH 私钥文件**: `K:\Key\tx.qsgl.net_id_ed25519`（已存在）
- **Git** 已安装

### 2. 腾讯云服务器配置（已有）

- 地址: `tx.qsgl.net`
- SSH 登录用户: `root`
- SSH 端口: `22`
- 部署目录: `/opt/payment-gateway`（包含 `docker-compose.yml` 和项目文件）
- Traefik 反向代理: 已配置
- Docker & Docker Compose: 已安装

## 部署步骤

### 方式 1: 使用默认参数部署（推荐）

在项目根目录打开 PowerShell，运行：

```powershell
.\build-and-deploy.ps1
```

该脚本将使用默认参数：
- 远程主机: `tx.qsgl.net`
- 远程用户: `root`
- 远程端口: `22`
- 部署目录: `/opt/payment-gateway`
- SSH 私钥: `K:\Key\tx.qsgl.net_id_ed25519`

### 方式 2: 使用自定义参数部署

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

## 脚本执行过程

脚本执行以下 5 个步骤：

### [1/5] 检查必要条件
- 验证 SSH 私钥文件存在
- 验证当前目录是项目根目录（有 Dockerfile）
- 检查 Docker 是否可用

### [2/5] 本地构建 Docker 镜像
```bash
docker build -t payment-gateway-jit:latest .
```
- 使用 .NET 10 SDK 编译应用
- 应用 Native AOT 优化（更小的镜像、更快的启动）
- 生成 Alpine 基础的最终镜像

**预计时间**: 3-5 分钟（首次构建会更长）

### [3/5] 导出镜像为 TAR 文件
```bash
docker save payment-gateway-jit:latest | gzip > payment-gateway-latest.tar.gz
```
- 导出镜像为压缩的 TAR 文件
- 文件大小通常 100-200 MB

### [4/5] 上传到腾讯云服务器
```bash
scp -i K:\Key\tx.qsgl.net_id_ed25519 payment-gateway-latest.tar.gz root@tx.qsgl.net:/tmp/
```
- 通过 SSH/SCP 安全上传文件
- 上传到服务器 `/tmp/` 目录

**预计时间**: 取决于网络速度（通常 1-3 分钟）

### [5/5] 远程执行部署

在腾讯云服务器上执行以下操作：

```bash
# 进入部署目录
cd /opt/payment-gateway

# 加载新镜像
docker load < /tmp/payment-gateway-latest.tar.gz

# 重启容器（使用新镜像）
docker-compose down
docker-compose up -d

# 等待服务启动
sleep 5

# 健康检查
curl http://localhost:8080/health

# 清理临时文件
rm /tmp/payment-gateway-latest.tar.gz

# 显示运行中的容器
docker ps | grep payment-gateway
```

**预计时间**: 30-60 秒

## 可能的错误及解决方案

### 错误 1: SSH 连接失败

**症状**:
```
error copy file to dest: Permission denied
```

**原因**: SSH 私钥权限或内容有问题

**解决方案**:
1. 检查私钥文件权限:
   ```powershell
   Get-Item "K:\Key\tx.qsgl.net_id_ed25519"
   ```
2. 验证可以手动 SSH 连接:
   ```powershell
   ssh -i "K:\Key\tx.qsgl.net_id_ed25519" -p 22 root@tx.qsgl.net "echo test"
   ```

### 错误 2: Docker 构建失败

**症状**:
```
error: docker build failed
```

**原因**: .NET 编译或依赖问题

**解决方案**:
1. 手动测试本地构建:
   ```bash
   docker build -t payment-gateway-jit:test .
   ```
2. 检查 Dockerfile 中的依赖是否都已安装
3. 清理 Docker 缓存:
   ```bash
   docker builder prune -a
   ```

### 错误 3: 健康检查失败

**症状**:
```
❌ 健康检查失败
```

**原因**: 容器启动失败或服务不可用

**解决方案**:
1. SSH 到服务器查看日志:
   ```bash
   ssh -i "K:\Key\tx.qsgl.net_id_ed25519" root@tx.qsgl.net
   docker logs payment-gateway
   ```
2. 检查 docker-compose.yml 中的环境变量
3. 确认服务器上的证书目录 `/opt/certs/cert` 存在

## 验证部署成功

部署完成后，可以通过以下方式验证：

### 1. 查看服务状态
```powershell
ssh -i "K:\Key\tx.qsgl.net_id_ed25519" root@tx.qsgl.net "docker ps | grep payment-gateway"
```

### 2. 访问 API 文档
```
https://payment.qsgl.net/swagger/
```

### 3. 执行健康检查
```powershell
Invoke-WebRequest -Uri "https://payment.qsgl.net/health"
```

### 4. 查看容器日志
```powershell
ssh -i "K:\Key\tx.qsgl.net_id_ed25519" root@tx.qsgl.net "docker logs -f payment-gateway"
```

## GitHub Actions 工作流

现在 GitHub Actions 只负责**通知**，具体部署由本地脚本完成：

**工作流**: `.github/workflows/auto-deploy.yml`

当推送代码到 `master` 分支时，Actions 会：
1. 检出代码
2. 提取提交信息
3. 在 Actions 日志中显示部署说明

**无需再配置 GitHub Secrets!**

## 快速参考

| 命令 | 说明 |
|------|------|
| `.\build-and-deploy.ps1` | 本地构建并部署到腾讯云 |
| `docker ps` | 查看本地运行的容器 |
| `ssh -i K:\Key\... root@tx.qsgl.net` | SSH 连接到腾讯云服务器 |
| `https://payment.qsgl.net` | 生产环境地址 |
| `https://payment.qsgl.net/swagger` | API 文档 |

## 故障排查

如果部署出现问题，按以下顺序检查：

1. **本地环境**:
   - [ ] Docker 正在运行
   - [ ] SSH 私钥可访问
   - [ ] 当前目录是项目根目录

2. **网络连接**:
   - [ ] 可以 ping 通 `tx.qsgl.net`
   - [ ] SSH 端口 22 可连接

3. **远程服务器**:
   - [ ] `/opt/payment-gateway` 目录存在
   - [ ] `docker-compose.yml` 文件存在
   - [ ] Docker 服务正在运行

4. **镜像和容器**:
   - [ ] 本地 Docker 镜像构建成功
   - [ ] 镜像文件上传到服务器 `/tmp/`
   - [ ] 容器能正常启动

有问题时，建议：
- 查看脚本输出的详细日志
- 手动 SSH 到服务器执行部分命令进行调试
- 检查 `docker logs payment-gateway` 查看应用错误

