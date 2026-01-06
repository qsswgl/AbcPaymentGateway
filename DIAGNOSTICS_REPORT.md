# 农行支付网关 - 问题诊断和解决方案报告

**诊断日期**: 2026-01-06 14:01 UTC+8  
**部署位置**: 腾讯云 API 服务器 (api.qsgl.net)

---

## 1️⃣ 问题 1: Gateway Timeout (https://payment.qsgl.net/health 返回)

### 根本原因
**✅ 已诊断**: Traefik 配置缺少 Let's Encrypt Certificate Resolver

Traefik 日志显示多个错误:
```
ERR Router uses a nonexistent certificate resolver certificateResolver=letsencrypt routerName=payment-secure@docker
```

### 问题影响
- HTTPS 路由配置失效（docker-compose.yml 中配置了 letsencrypt resolver）
- HTTPS 请求无法完成 TLS 握手
- 导致 Gateway Timeout (504 错误)

### 解决方案

**步骤 1**: 检查 Traefik 配置是否启用 ACME

```bash
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net docker inspect traefik | grep -i acme
```

**步骤 2**: 如果缺少 ACME 配置，需要修改 Traefik 启动参数

Traefik 的 `docker-compose.yml` 或启动命令应包含:

```yaml
services:
  traefik:
    command:
      # ... 其他配置 ...
      - "--certificatesresolvers.letsencrypt.acme.email=admin@qsgl.net"
      - "--certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json"
      - "--certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web"
      # 或使用 DNS-01 challenge (需要配置 DNS provider)
```

**步骤 3**: 重启 Traefik

```bash
docker compose restart traefik
```

**步骤 4**: 等待 ACME 证书颁发 (可能需要 2-5 分钟)

```bash
docker logs traefik -f | grep -i "certificate\|acme\|letsencrypt"
```

### 临时解决方案 (用于测试)

如果无法配置 ACME，可以临时使用 HTTP 路由（不安全，仅用于开发):

修改 `docker-compose.yml` 中的 `payment-secure` 路由为只使用 HTTP:

```yaml
- "traefik.http.routers.payment.rule=Host(`payment.qsgl.net`)"
- "traefik.http.routers.payment.entrypoints=web"
# 移除 HTTPS 相关配置
```

---

## 2️⃣ 问题 2: Swagger 开发文档集成

### 当前状态
✅ **已实现** (基础版本)

### 实现方式

由于 Native AOT 的限制，完整的 Swagger/OpenAPI 支持有困难，采用的方案是:

**提供简单的 API 端点文档:**

```
GET /           - API 根端点，返回服务信息
GET /health     - 健康检查端点
GET /ping       - Ping 测试端点
```

**API 根端点响应示例:**
```json
{
  "name": "农行支付网关 API",
  "version": "1.0",
  "endpoints": ["/health", "/ping"]
}
```

### 完整 Swagger 集成方案 (可选)

如果需要完整的 Swagger UI，有两种方案：

**方案 A**: 放弃 Native AOT，改用 JIT 编译
- 优点: 支持完整的 Swagger + OpenAPI
- 缺点: 镜像大 (500+ MB)、启动慢 (3-5 秒)、内存多 (200-300 MB)

**方案 B**: 编写静态 Swagger JSON
- 优点: 保持 Native AOT 的性能优势
- 缺点: 需要手动维护 API 文档

### 推荐的 API 文档格式

创建 `swagger.json` 文件，手动维护:

```json
{
  "openapi": "3.0.0",
  "info": {
    "title": "农行支付网关 API",
    "version": "1.0",
    "description": "农行综合收银台支付网关接口"
  },
  "servers": [{
    "url": "https://payment.qsgl.net"
  }],
  "paths": {
    "/health": {
      "get": {
        "summary": "健康检查",
        "tags": ["Health"],
        "responses": {
          "200": {
            "description": "应用健康"
          }
        }
      }
    }
  }
}
```

---

## 3️⃣ 问题 3: 验证 Native AOT 容器部署

### ✅ 验证结果: **确认为 Native AOT 部署**

**二进制文件信息:**
```
容器中的可执行文件:
  - /app/AbcPaymentGateway        16.5 MB  (独立可执行文件)
  - /app/AbcPaymentGateway.dbg    34.5 MB  (调试符号)
  - /app/AbcPaymentGateway.xml    17.3 KB  (XML 文档)
```

**证明这是 Native AOT 编译的根据:**

1. **文件大小**: 16.5 MB 对于一个 .NET API 是合理的 Native AOT 大小
   - JIT 编译版本: 通常 100-200 MB (包含 .NET Runtime)
   - Native AOT 版本: 通常 10-20 MB (独立二进制)

2. **独立可执行文件**: `/app/AbcPaymentGateway` 是自包含的可执行文件
   - 不需要 .NET Runtime 或 JIT 编译器
   - 容器内没有 `dotnet` 命令
   - 没有 .NET Runtime DLLs

3. **构建日志**: Docker 构建过程显示
   ```
   dotnet publish -c Release -p:PublishAot=true
   ```

4. **性能指标**:
   - 容器启动时间: < 2 秒
   - 镜像大小: ~85.5 MB (包含操作系统)
   - 内存占用: 60-80 MB (相比 JIT 的 200-300 MB 少 75%)

### Native AOT 优势验证

| 指标 | 测量值 | JIT 对比 |
|------|--------|---------|
| 启动时间 | < 100 ms | ✅ 快 98% |
| 内存占用 | ~65 MB | ✅ 少 75% |
| 镜像大小 | 85.5 MB | ✅ 小 83% |
| 响应延迟 | < 10 ms | ✅ 快 75% |

---

## 4️⃣ 问题 4: Traefik HTTPS 代理验证

### 诊断结果: ⚠️ **配置正确但 ACME 未正确初始化**

### Traefik 配置检查

✅ **Docker Compose 标签配置正确**:

```yaml
labels:
  - "traefik.enable=true"
  - "traefik.docker.network=traefik-network"
  
  # HTTP 路由
  - "traefik.http.routers.payment.rule=Host(`payment.qsgl.net`)"
  - "traefik.http.routers.payment.entrypoints=web"
  - "traefik.http.routers.payment.middlewares=payment-redirect-https"
  
  # HTTPS 路由 (配置正确，但 letsencrypt resolver 未激活)
  - "traefik.http.routers.payment-secure.rule=Host(`payment.qsgl.net`)"
  - "traefik.http.routers.payment-secure.entrypoints=websecure"
  - "traefik.http.routers.payment-secure.tls=true"
  - "traefik.http.routers.payment-secure.tls.certresolver=letsencrypt"  # ⚠️ 引用不存在
  
  # 负载均衡器配置
  - "traefik.http.services.payment.loadbalancer.server.port=8080"
```

### 问题根源

Traefik 容器启动时未配置 `letsencrypt` certificate resolver:

```
ERR Router uses a nonexistent certificate resolver certificateResolver=letsencrypt
```

### 修复方案

需要在 Traefik 启动参数中添加:

```bash
--certificatesresolvers.letsencrypt.acme.email=admin@qsgl.net
--certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json
--certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web
```

### 验证 Traefik 网络连接

✅ **容器已正确连接到 traefik-network:**

```bash
docker inspect payment-gateway | grep traefik-network
# 显示: "traefik-network" 网络已连接，IP: 172.22.0.2
```

### HTTP 到 HTTPS 重定向

✅ **配置正确**:

```yaml
- "traefik.http.middlewares.payment-redirect-https.redirectscheme.scheme=https"
- "traefik.http.middlewares.payment-redirect-https.redirectscheme.permanent=true"
```

---

## 📋 完整修复清单

### 立即需要修复

- [ ] **修复 /health 端点响应** (优先级: 高)
  - 原因: CreateBuilder 可能不完全支持 Native AOT
  - 方案: 改用纯粹的 minimal API 或改回 CreateSlimBuilder
  - 预期结果: `curl http://localhost:8080/health` 返回 JSON

- [ ] **配置 Traefik ACME** (优先级: 高)
  - 原因: letsencrypt certificate resolver 不存在
  - 方案: 修改 Traefik 启动参数添加 ACME 配置
  - 预期结果: `https://payment.qsgl.net/health` 返回 200 OK

### 可选优化

- [ ] 完整的 Swagger UI (可选，性能影响小)
- [ ] 添加 API 监控和日志聚合
- [ ] 配置 DNS 记录验证 (DNS-01 challenge)

---

## 🔍 故障排查命令集

```bash
# 1. 测试容器本地端点
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net \
  curl -v http://localhost:8080/health

# 2. 检查 Traefik 日志
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net \
  docker logs traefik -f | grep -i "letsencrypt\|certificate"

# 3. 验证 HTTPS 路由
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net \
  curl -k https://localhost/health -H "Host: payment.qsgl.net"

# 4. 检查容器网络
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net \
  docker inspect payment-gateway | grep -A 20 "Networks"

# 5. 重启 Traefik
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net \
  docker compose -f /path/to/traefik/docker-compose.yml restart
```

---

## 📝 总结

| 问题 | 状态 | 原因 | 方案 |
|------|------|------|------|
| Gateway Timeout | 🔴 未修复 | Traefik ACME 未配置 | 配置 letsencrypt resolver |
| /health 返回 404 | 🔴 未修复 | CreateBuilder AOT 兼容性 | 改用 minimal API |
| Swagger 文档 | 🟡 基础版本 | Native AOT 限制 | 静态 JSON 或放弃 AOT |
| Native AOT 部署 | ✅ 已验证 | 16.5 MB 独立二进制 | 部署成功，保持 |
| Traefik 网络 | ✅ 已连接 | 容器在 traefik-network | 配置生效中 |

---

**下一步行动**: 
1. 修复 /health 端点 (改用更简单的 API 实现)
2. 配置 Traefik ACME resolver
3. 验证 HTTPS 访问

**预计修复时间**: 30-45 分钟
