# 修复执行检查清单

**项目**: 农行支付网关 API  
**诊断日期**: 2026-01-06  
**状态**: 待执行  
**预计用时**: 1 小时  

---

## ✅ 修复清单

### Phase 1: 修复 /health 端点 (15 分钟)

**目标**: 恢复 `/health` 端点响应，修复容器健康检查

- [ ] **步骤 1.1**: 备份当前 Program.cs
  ```bash
  ssh root@api.qsgl.net "cp /opt/payment-gateway/Program.cs /opt/payment-gateway/Program.cs.backup"
  ```

- [ ] **步骤 1.2**: 复制改进的 Program.cs 到远程服务器
  ```powershell
  scp -i K:\Key\tx.qsgl.net_id_ed25519 `
    K:\payment\AbcPaymentGateway\Program_FIXED.cs `
    root@api.qsgl.net:/opt/payment-gateway/Program.cs
  ```

- [ ] **步骤 1.3**: 在服务器上重命名文件
  ```bash
  ssh root@api.qsgl.net "mv /opt/payment-gateway/Program.cs /opt/payment-gateway/Program.cs"
  ```

- [ ] **步骤 1.4**: 构建新镜像 (不使用缓存)
  ```bash
  ssh root@api.qsgl.net "cd /opt/payment-gateway && docker compose build --no-cache"
  ```
  
  预期输出:
  ```
  Successfully built payment-gateway-aot:latest
  Successfully tagged payment-gateway-aot:latest
  ```

- [ ] **步骤 1.5**: 停止旧容器
  ```bash
  ssh root@api.qsgl.net "docker compose down"
  ```

- [ ] **步骤 1.6**: 启动新容器
  ```bash
  ssh root@api.qsgl.net "docker compose up -d"
  ```

- [ ] **步骤 1.7**: 等待容器启动
  ```bash
  ssh root@api.qsgl.net "sleep 3 && docker compose ps"
  ```
  
  验证: 状态应该是 `Up X seconds (healthy)`

- [ ] **步骤 1.8**: 测试端点
  ```bash
  ssh root@api.qsgl.net "curl -s http://localhost:8080/health | jq ."
  ```
  
  预期输出:
  ```json
  {
    "status": "healthy",
    "timestamp": "2026-01-06T14:30:00.1234567Z",
    "uptime": 123
  }
  ```

- [ ] **步骤 1.9**: 查看日志确认无错误
  ```bash
  ssh root@api.qsgl.net "docker logs payment-gateway | tail -20"
  ```

- [ ] **步骤 1.10**: ✅ Phase 1 验收
  - 容器状态: healthy
  - 端点响应: 200 OK
  - JSON 包含: status, timestamp, uptime

---

### Phase 2: 配置 Traefik ACME (20 分钟)

**目标**: 启用 Let's Encrypt 自动证书管理，修复 HTTPS 路由

- [ ] **步骤 2.1**: 找到 Traefik 配置文件
  ```bash
  ssh root@api.qsgl.net "find /opt -name 'docker-compose.yml' -exec grep -l traefik {} \;"
  ```
  
  记录找到的路径: ___________________________

- [ ] **步骤 2.2**: 查看现有 Traefik 配置
  ```bash
  ssh root@api.qsgl.net "cat /opt/traefik/docker-compose.yml"
  # 或者
  ssh root@api.qsgl.net "cat /opt/docker-compose.yml | grep -A 50 'traefik:'"
  ```

- [ ] **步骤 2.3**: 创建 acme.json 持久化目录
  ```bash
  ssh root@api.qsgl.net "mkdir -p /letsencrypt && chmod 600 /letsencrypt"
  ```

- [ ] **步骤 2.4**: 编辑 Traefik docker-compose.yml
  
  使用 vim/nano 打开配置文件:
  ```bash
  ssh root@api.qsgl.net "nano /opt/traefik/docker-compose.yml"
  ```
  
  找到 `traefik:` 服务的 `command:` 部分，在以下位置之后添加 ACME 配置:
  
  ```yaml
  command:
    - "--api.insecure=true"
    - "--api.dashboard=true"
    - "--providers.docker=true"
    - "--providers.docker.exposedbydefault=false"
    - "--entrypoints.web.address=:80"
    - "--entrypoints.websecure.address=:443"
    
    # ✅ 添加以下三行
    - "--certificatesresolvers.letsencrypt.acme.email=admin@qsgl.net"
    - "--certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json"
    - "--certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web"
  ```
  
  并确保 `volumes:` 部分包含:
  ```yaml
  volumes:
    - /var/run/docker.sock:/var/run/docker.sock
    - /letsencrypt:/letsencrypt
  ```

- [ ] **步骤 2.5**: 保存配置文件
  ```
  Ctrl+X, Y, Enter (在 nano 中)
  ```

- [ ] **步骤 2.6**: 重启 Traefik
  ```bash
  ssh root@api.qsgl.net "docker compose -f /opt/traefik/docker-compose.yml restart"
  # 或如果在 /opt 目录
  ssh root@api.qsgl.net "cd /opt && docker compose restart traefik"
  ```

- [ ] **步骤 2.7**: 等待 Traefik 启动 (10 秒)
  ```bash
  sleep 10
  ```

- [ ] **步骤 2.8**: 监控 ACME 证书颁发
  ```bash
  ssh root@api.qsgl.net "docker logs traefik -f | grep -E 'letsencrypt|certificate|acme'"
  ```
  
  等待看到类似消息:
  ```
  Certificate obtained for payment.qsgl.net
  ACME challenge successful
  ```
  
  按 Ctrl+C 停止监控 (等待 5-30 秒)

- [ ] **步骤 2.9**: 验证 acme.json 已创建
  ```bash
  ssh root@api.qsgl.net "ls -lh /letsencrypt/acme.json"
  ```
  
  预期: 文件存在且大小 > 1 KB

- [ ] **步骤 2.10**: 检查是否有错误
  ```bash
  ssh root@api.qsgl.net "docker logs traefik | grep -i 'error\|ERR' | tail -10"
  ```
  
  应该 **没有** 关于 `letsencrypt` resolver 的错误

- [ ] **步骤 2.11**: ✅ Phase 2 验收
  - [ ] Traefik 运行正常
  - [ ] acme.json 文件已创建
  - [ ] 没有 letsencrypt resolver 错误
  - [ ] 证书已颁发 (查看日志)

---

### Phase 3: 端到端验证 (20 分钟)

**目标**: 验证完整的 HTTP/HTTPS 路由和 DNS 解析

- [ ] **步骤 3.1**: 本地 HTTP 测试 (容器直接访问)
  ```bash
  ssh root@api.qsgl.net "curl -v http://localhost:8080/health"
  ```
  
  预期:
  ```
  HTTP/1.1 200 OK
  Content-Type: application/json
  
  {"status":"healthy", ...}
  ```
  
  验证: ✓ HTTP 200

- [ ] **步骤 3.2**: Traefik HTTP 转发测试
  ```bash
  ssh root@api.qsgl.net "curl -H 'Host: payment.qsgl.net' http://localhost/health"
  ```
  
  预期: 200 OK + JSON 响应
  
  验证: ✓ HTTP 200

- [ ] **步骤 3.3**: Traefik HTTPS 重定向测试
  ```bash
  ssh root@api.qsgl.net "curl -L http://localhost/health 2>&1 | head -20"
  ```
  
  预期: 重定向到 HTTPS (curl 会跟随)
  
  验证: ✓ 301/302 重定向 + 最终 200

- [ ] **步骤 3.4**: 本地 HTTPS 测试 (忽略证书)
  ```bash
  ssh root@api.qsgl.net "curl -k https://localhost/health"
  ```
  
  预期: 200 OK + JSON 响应
  
  验证: ✓ HTTPS 200 (即使证书警告)

- [ ] **步骤 3.5**: 检查 DNS 解析
  ```bash
  nslookup payment.qsgl.net
  # 或
  ssh root@api.qsgl.net "nslookup payment.qsgl.net"
  ```
  
  预期: 返回服务器 IP 地址 (e.g., 123.456.789.0)
  
  记录 IP: ___________________________

- [ ] **步骤 3.6**: 等待 DNS 生效 (如需要)
  ```
  如果 DNS 未生效，等待 5-10 分钟后重试
  或修改本地 hosts 文件添加:
  123.456.789.0  payment.qsgl.net
  ```

- [ ] **步骤 3.7**: 远程 HTTPS 测试 (从本地 Windows)
  ```powershell
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  $response = Invoke-WebRequest -Uri "https://payment.qsgl.net/health" `
    -SkipCertificateCheck
  $response.StatusCode
  $response.Content | ConvertFrom-Json
  ```
  
  预期:
  ```
  StatusCode: 200
  status    : healthy
  timestamp : 2026-01-06T14:35:00...
  ```
  
  验证: ✓ HTTPS 200 + JSON

- [ ] **步骤 3.8**: 浏览器测试
  
  在浏览器中访问:
  ```
  https://payment.qsgl.net/health
  ```
  
  预期:
  - 没有证书警告 (绿色锁头)
  - 显示 JSON 响应
  - 状态码 200
  
  验证: ✓ 浏览器绿色锁头

- [ ] **步骤 3.9**: 测试其他端点
  ```bash
  curl https://payment.qsgl.net/
  curl https://payment.qsgl.net/ping
  ```
  
  预期: 200 OK 响应
  
  验证: ✓ 所有端点正常

- [ ] **步骤 3.10**: 查看 Traefik 日志确认路由成功
  ```bash
  docker logs traefik | tail -30 | grep -i "payment\|200"
  ```
  
  预期: 看到访问日志，状态码 200
  
  验证: ✓ 路由日志正常

- [ ] **步骤 3.11**: ✅ Phase 3 验收
  - [ ] HTTP 端点: 200 OK
  - [ ] HTTPS 端点: 200 OK
  - [ ] DNS 解析: 正常
  - [ ] 浏览器: 绿色锁头，无警告
  - [ ] 所有端点: 响应正常

---

### Phase 4: 可选 - 添加 Swagger 文档 (30 分钟)

**目标**: 提供 API 开发文档

- [ ] **步骤 4.1**: 创建 swagger.json 文件
  ```bash
  ssh root@api.qsgl.net "cat > /opt/payment-gateway/Web/swagger.json << 'EOF'
  {
    "openapi": "3.0.0",
    "info": {
      "title": "农行支付网关 API",
      "version": "1.0",
      "description": "农行综合收银台支付网关接口服务"
    },
    "servers": [
      {
        "url": "https://payment.qsgl.net",
        "description": "生产环境"
      }
    ],
    "paths": {
      "/health": {
        "get": {
          "summary": "健康检查端点",
          "tags": ["Health"],
          "responses": {
            "200": {
              "description": "应用健康状态",
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "properties": {
                      "status": { "type": "string", "example": "healthy" },
                      "timestamp": { "type": "string", "format": "date-time" },
                      "uptime": { "type": "integer", "description": "运行时间(秒)" }
                    }
                  }
                }
              }
            }
          }
        }
      },
      "/ping": {
        "get": {
          "summary": "Ping 测试端点",
          "tags": ["Utility"],
          "responses": {
            "200": {
              "description": "Pong 响应",
              "content": {
                "text/plain": { "schema": { "type": "string", "example": "pong" } }
              }
            }
          }
        }
      }
    }
  }
  EOF"
  ```

- [ ] **步骤 4.2**: 创建 Swagger UI HTML
  ```bash
  ssh root@api.qsgl.net "cat > /opt/payment-gateway/Web/swagger-ui.html << 'EOF'
  <!DOCTYPE html>
  <html>
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>农行支付网关 API 文档</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/swagger-ui/4.15.5/swagger-ui.min.css">
    <style>
      html { box-sizing: border-box; overflow: -moz-scrollbars-vertical; overflow-y: scroll; }
      *, *:before, *:after { box-sizing: inherit; }
      body { margin:0; padding: 20px; }
    </style>
  </head>
  <body>
    <div id="swagger-ui"></div>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/swagger-ui/4.15.5/swagger-ui.min.js"></script>
    <script>
      const ui = SwaggerUIBundle({
        url: "/swagger.json",
        dom_id: '#swagger-ui',
        presets: [
          SwaggerUIBundle.presets.apis,
          SwaggerUIBundle.SwaggerUIStandalonePreset
        ],
        layout: "StandaloneLayout"
      });
      window.ui = ui;
    </script>
  </body>
  </html>
  EOF"
  ```

- [ ] **步骤 4.3**: 在 Program.cs 中添加 Swagger 端点
  
  编辑 Program.cs，在 `app.MapGet("/ping", ...)` 之后添加:
  ```csharp
  app.MapGet("/swagger.json", GetSwaggerJson)
      .WithName("Swagger")
      .WithOpenApi();

  app.MapGet("/docs", GetSwaggerUI)
      .WithName("Docs")
      .WithOpenApi();

  // ... 在文件末尾添加
  static IResult GetSwaggerJson()
  {
      var json = System.IO.File.ReadAllText("Web/swagger.json");
      return Results.Text(json, "application/json");
  }

  static IResult GetSwaggerUI()
  {
      var html = System.IO.File.ReadAllText("Web/swagger-ui.html");
      return Results.Text(html, "text/html");
  }
  ```

- [ ] **步骤 4.4**: 上传修改的 Program.cs
  ```powershell
  scp -i K:\Key\tx.qsgl.net_id_ed25519 `
    K:\payment\AbcPaymentGateway\Program_FIXED.cs `
    root@api.qsgl.net:/opt/payment-gateway/Program.cs
  ```

- [ ] **步骤 4.5**: 构建和重启容器
  ```bash
  ssh root@api.qsgl.net "cd /opt/payment-gateway && docker compose build --no-cache && docker compose restart"
  sleep 5
  ```

- [ ] **步骤 4.6**: 验证 Swagger UI
  ```bash
  curl -s https://payment.qsgl.net/docs | head -20
  ```
  
  预期: HTML 内容以 `<!DOCTYPE html>` 开头

- [ ] **步骤 4.7**: 浏览器验证
  
  访问: `https://payment.qsgl.net/docs`
  
  预期:
  - Swagger UI 加载成功
  - 显示 API 列表 (/health, /ping)
  - 可以展开端点查看文档
  - Try it out 按钮可用
  
  验证: ✓ Swagger UI 正常

- [ ] **步骤 4.8**: ✅ Phase 4 验收
  - [ ] `/swagger.json` 返回 JSON
  - [ ] `/docs` 返回 HTML
  - [ ] Swagger UI 加载成功
  - [ ] 端点文档显示正确

---

## 🎯 最终验收

所有 Phase 完成后，进行最终验收:

### 功能验收清单

- [ ] `/health` 端点
  - [ ] 返回 200 OK
  - [ ] 响应时间 < 100ms
  - [ ] 包含 status, timestamp, uptime 字段

- [ ] HTTPS 访问
  - [ ] 浏览器显示绿色锁头 (无证书警告)
  - [ ] 证书颁发者: Let's Encrypt
  - [ ] 有效期 > 30 天

- [ ] Docker 健康检查
  - [ ] 容器状态: `Up X minutes (healthy)`
  - [ ] healthcheck 通过

- [ ] Swagger 文档 (可选)
  - [ ] `/docs` 加载成功
  - [ ] API 列表显示正确
  - [ ] Try it out 功能正常

### 性能验收

```bash
# 响应时间测试
for i in {1..10}; do
  time curl -s https://payment.qsgl.net/health > /dev/null
done
```

预期:
- 平均响应时间: 5-20ms
- 99 percentile: < 100ms

### 负载测试 (可选)

```bash
# 使用 ab (Apache Bench) 进行简单负载测试
ab -n 100 -c 10 https://payment.qsgl.net/health
```

预期:
- Requests per second: > 100
- Failed requests: 0

---

## 🆘 故障排查

如果任何步骤失败，使用以下命令诊断:

### 问题: /health 端点仍然返回 404

```bash
# 查看容器日志
ssh root@api.qsgl.net "docker logs payment-gateway"

# 检查编译错误
ssh root@api.qsgl.net "docker compose build --no-cache 2>&1 | tail -50"

# 进入容器手动测试
ssh root@api.qsgl.net "docker exec -it payment-gateway sh -c 'curl http://localhost:8080/health'"
```

**解决方案**: 重新上传 Program_FIXED.cs，确保文件内容完整

### 问题: HTTPS 返回 503/504 Gateway Timeout

```bash
# 检查 Traefik 错误
ssh root@api.qsgl.net "docker logs traefik | grep -i error"

# 验证 ACME 配置
ssh root@api.qsgl.net "docker inspect traefik | jq '.[] | .Config.Cmd' | grep letsencrypt"

# 检查证书存储
ssh root@api.qsgl.net "ls -la /letsencrypt/"
```

**解决方案**: 重新检查 Traefik 启动参数中的 ACME 配置是否正确

### 问题: Let's Encrypt 证书未获取

```bash
# 查看 ACME 日志
ssh root@api.qsgl.net "docker logs traefik -f | grep -i 'acme\|certificate'"

# 检查 DNS 解析
ssh root@api.qsgl.net "nslookup payment.qsgl.net"

# 测试 HTTP-01 challenge 可达性
curl -v http://payment.qsgl.net/.well-known/acme-challenge/test
```

**解决方案**: 
1. 确保 DNS 正确指向服务器
2. 确保 80 端口未被占用
3. 等待 5-10 分钟后重试

### 问题: DNS 未解析或需要手动配置

```
临时解决方案 (Windows):
编辑 C:\Windows\System32\drivers\etc\hosts
添加行:
123.456.789.0  payment.qsgl.net
```

---

## 📋 记录和文档

### 修复记录

修复日期: ________________  
修复人员: ________________  
修复时长: ________________  

每个 Phase 完成时间:
- Phase 1 完成时间: ________________
- Phase 2 完成时间: ________________
- Phase 3 完成时间: ________________
- Phase 4 完成时间: ________________

### 修复后的关键指标

| 指标 | 修复前 | 修复后 | 目标 |
|------|--------|--------|------|
| HTTPS 可用性 | ❌ | ✅ | 100% |
| /health 响应 | 404 | 200 | 200 |
| 证书状态 | 无效 | 有效 | Let's Encrypt |
| 容器健康 | 不确定 | Healthy | Healthy |
| API 文档 | 无 | 有 | 有 |

---

**清单版本**: 1.0  
**最后更新**: 2026-01-06  
**状态**: ⏳ 待执行

当所有项目都打勾 (✓) 时，修复完成！
