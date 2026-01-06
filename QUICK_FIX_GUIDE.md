# 立即行动计划 - 修复支付网关

## 问题 1: 修复 /health 端点 (当前优先级: 🔴 高)

### 根本原因
当前 `Program.cs` 的 `CreateBuilder` 在 Native AOT 中可能存在 minimal API 注册问题。

### 修复方案 A: 重新实现为完全最小化的 API

**新的 Program.cs:**

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

// 禁用需要 reflection 的功能
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// 使用最简单的端点定义
app.MapGet("/", HandleRoot);
app.MapGet("/health", HandleHealth);
app.MapGet("/ping", HandlePing);

app.Run();

static IResult HandleRoot()
{
    return Results.Json(new
    {
        name = "农行支付网关 API",
        version = "1.0",
        status = "running",
        timestamp = DateTime.UtcNow.ToString("O")
    });
}

static IResult HandleHealth()
{
    return Results.Json(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow.ToString("O")
    });
}

static IResult HandlePing()
{
    return Results.Ok("pong");
}
```

### 修复方案 B: 使用自定义中间件实现 /health

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// 健康检查中间件
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health")
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow.ToString("O")
        });
        return;
    }
    await next();
});

// 其他端点
app.MapGet("/", () => Results.Json(new { name = "农行支付网关 API", version = "1.0" }));
app.MapGet("/ping", () => Results.Ok("pong"));

app.Run();
```

### 测试步骤

1. **本地测试** (Windows):
```powershell
# 构建项目
dotnet build -c Release

# 运行
dotnet run --no-build -c Release

# 在另一个 PowerShell 中测试
Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing | Select-Object -ExpandProperty Content
```

2. **Docker 测试** (Linux 服务器):
```bash
# SSH 到服务器
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net

# 进入项目目录
cd /opt/payment-gateway

# 构建镜像
docker compose build --no-cache

# 重启容器
docker compose down
docker compose up -d
sleep 3

# 测试本地端点
docker exec payment-gateway curl -s http://localhost:8080/health | jq .

# 查看日志
docker logs payment-gateway
```

---

## 问题 2: 配置 Traefik ACME 证书解析器

### 原因
Traefik 启动时没有配置 `letsencrypt` certificate resolver，导致 HTTPS 路由失败。

### 修复步骤

**步骤 1: 找到 Traefik 配置文件**

```bash
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net

# 查找 Traefik 的 docker-compose.yml
find /opt -name "docker-compose.yml" -type f

# 通常在
ls -la /opt/traefik/ 或 /opt/docker-compose.yml
```

**步骤 2: 修改 Traefik docker-compose.yml**

如果 Traefik 使用 docker-compose.yml 启动:

```yaml
services:
  traefik:
    image: traefik:v3.2
    command:
      - "--api.insecure=true"
      - "--api.dashboard=true"
      - "--providers.docker=true"
      - "--providers.docker.exposedbydefault=false"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      # ✅ 添加这些行来配置 ACME
      - "--certificatesresolvers.letsencrypt.acme.email=admin@qsgl.net"
      - "--certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json"
      - "--certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web"
      # ✅ 也可以使用 DNS challenge (如果已配置)
      # - "--certificatesresolvers.letsencrypt.acme.dnschallenge=true"
      # - "--certificatesresolvers.letsencrypt.acme.dnschallenge.provider=cloudflare"
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - /letsencrypt:/letsencrypt  # ✅ 持久化证书存储
      - ./traefik.yml:/traefik.yml  # 如果使用配置文件
    networks:
      - traefik-network
    restart: always
```

**步骤 3: 重启 Traefik**

```bash
# 停止当前 Traefik
docker compose -f /path/to/traefik/docker-compose.yml down

# 启动新配置的 Traefik
docker compose -f /path/to/traefik/docker-compose.yml up -d

# 等待启动完成 (5-10 秒)
sleep 5

# 查看日志
docker logs traefik -f | grep -E "letsencrypt|certificate|ACME"
```

**步骤 4: 验证 ACME 初始化**

```bash
# 检查 acme.json 文件是否存在
ls -la /letsencrypt/acme.json

# 查看 Traefik 日志中是否有成功消息
docker logs traefik | grep -i "success\|certificate obtained"
```

### 替代方案: 如果无法等待 ACME 颁发证书

可以临时使用自签名证书进行测试:

```yaml
- "traefik.http.routers.payment-secure.tls=true"
- "traefik.http.routers.payment-secure.tls.certresolver=selfsigned"
```

然后在 Traefik 命令中添加:
```
--certificatesresolvers.selfsigned.acme.storage=/letsencrypt/acme.json
--certificatesresolvers.selfsigned.acme.httpchallenge.entrypoint=web
```

---

## 问题 3: 验证端到端 HTTPS 连接

### 测试步骤

**步骤 1: 检查 DNS 解析**

```bash
# 从服务器检查
nslookup payment.qsgl.net
# 应该返回服务器 IP 地址
```

**步骤 2: 测试本地 HTTP (容器内部)**

```bash
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net

# 测试容器端口
docker exec payment-gateway curl -v http://localhost:8080/health

# 预期输出:
# HTTP/1.1 200 OK
# 
# {"status":"healthy","timestamp":"2026-01-06T14:15:30.1234567Z"}
```

**步骤 3: 测试通过 Traefik 的 HTTP**

```bash
# 从服务器本地测试 (不需要 DNS)
curl -H "Host: payment.qsgl.net" http://localhost/health

# 或指定 Traefik IP
curl -H "Host: payment.qsgl.net" http://172.22.0.1/health

# 预期: 200 OK 和 JSON 响应
```

**步骤 4: 测试通过 Traefik 的 HTTPS (等待证书)**

```bash
# 等待 Let's Encrypt 证书颁发 (可能需要 1-5 分钟)
sleep 60

# 测试 HTTPS (忽略证书警告用于初始测试)
curl -k https://payment.qsgl.net/health

# 如果 DNS 正确配置，也可以
curl https://payment.qsgl.net/health

# 预期: 200 OK 和 JSON 响应
```

**步骤 5: Windows 客户端测试**

```powershell
# PowerShell 测试 HTTPS
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://payment.qsgl.net/health" `
  -SkipCertificateCheck | Select-Object -ExpandProperty Content | ConvertFrom-Json
```

---

## 🔧 完整修复检查清单

### Phase 1: 修复 /health 端点

- [ ] 备份当前 Program.cs
- [ ] 实现方案 A 或 B
- [ ] 本地构建测试 (dotnet run)
- [ ] 上传修改的 Program.cs 到服务器
- [ ] Docker 构建 (no-cache)
- [ ] 验证容器启动成功
- [ ] 测试 `curl http://localhost:8080/health` 返回 200
- [ ] ✅ 验证完成

### Phase 2: 配置 Traefik ACME

- [ ] 找到 Traefik docker-compose.yml
- [ ] 添加 ACME 配置参数
- [ ] 创建 /letsencrypt 目录和 acme.json
- [ ] 重启 Traefik
- [ ] 等待 5-10 分钟让 Let's Encrypt 颁发证书
- [ ] 验证 acme.json 包含证书
- [ ] ✅ Traefik 日志显示成功

### Phase 3: 端到端验证

- [ ] 测试 `http://localhost/health` (本地)
- [ ] 测试 `https://localhost/health` (本地, -k)
- [ ] 测试 `curl https://payment.qsgl.net/health` (外部, 等待 DNS)
- [ ] 验证浏览器访问 `https://payment.qsgl.net` 不显示证书警告
- [ ] ✅ 完全验证成功

---

## 预期结果

修复完成后:

✅ `https://payment.qsgl.net/health` 返回 200 OK
```json
{
  "status": "healthy",
  "timestamp": "2026-01-06T14:15:30.1234567Z"
}
```

✅ `https://payment.qsgl.net/` 返回 API 信息
```json
{
  "name": "农行支付网关 API",
  "version": "1.0",
  "status": "running",
  "timestamp": "2026-01-06T14:15:30.1234567Z"
}
```

✅ `https://payment.qsgl.net/ping` 返回 `pong`

✅ Gateway Timeout 错误消失

---

## 故障排查

如果仍然有问题:

```bash
# 查看 Traefik 路由配置
docker logs traefik | grep -E "Routes|Certificate|Error"

# 验证网络连接
docker network inspect traefik-network

# 查看 payment-gateway 日志
docker logs payment-gateway -f

# 进入容器手动测试
docker exec -it payment-gateway sh
curl http://localhost:8080/health
```

---

**文档版本**: 1.0  
**最后更新**: 2026-01-06  
**状态**: 待执行
