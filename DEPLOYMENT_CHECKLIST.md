# 部署检查清单

在部署到生产环境之前，请确保完成以下所有检查项。

## ✅ 部署前检查

### 代码和配置

- [ ] 代码已提交到版本控制系统
- [ ] 所有测试通过
- [ ] 本地构建成功 (`dotnet build -c Release`)
- [ ] 敏感信息（密码、密钥）未提交到代码库
- [ ] `.gitignore` 正确配置

### 证书配置

- [ ] 农行商户证书（.pfx）已准备
- [ ] 证书密码已确认
- [ ] 农行平台证书（TrustPay.cer）已准备
- [ ] 证书有效期已检查（未过期）
- [ ] 证书路径在配置文件中正确设置

### 应用配置

- [ ] `appsettings.json` 生产环境配置已更新
- [ ] 商户 ID 正确
- [ ] 服务器地址正确（生产：pay.abchina.com）
- [ ] 日志路径已配置
- [ ] 回调 URL 已配置
- [ ] CORS 策略已审查

### 服务器准备

- [ ] SSH 访问权限已确认
- [ ] Docker 已安装 (`docker --version`)
- [ ] Docker Compose 已安装 (`docker-compose --version`)
- [ ] .NET 10 运行时环境已安装
- [ ] 服务器磁盘空间充足（建议 > 10GB）
- [ ] 服务器内存充足（建议 > 2GB）

### 网络和域名

- [ ] 域名 DNS 已解析到服务器 IP
- [ ] 防火墙规则已配置
  - [ ] 允许 80 端口（HTTP）
  - [ ] 允许 443 端口（HTTPS）
  - [ ] 允许 SSH 端口
- [ ] Traefik 反向代理已运行
- [ ] Traefik 网络已创建 (`docker network ls`)

### Traefik 配置

- [ ] Traefik 容器正在运行
- [ ] `traefik-network` 网络存在
- [ ] Let's Encrypt 邮箱已配置
- [ ] SSL 证书自动申请已启用
- [ ] docker-compose.yml 中的 Traefik 标签正确

## ✅ 部署过程检查

### 文件上传

- [ ] 证书文件已上传到服务器
- [ ] 项目文件已上传到服务器
- [ ] 文件权限正确设置

### Docker 构建

- [ ] Docker 镜像构建成功
- [ ] 容器启动成功
- [ ] 容器日志无错误
- [ ] 容器健康检查通过

### 服务验证

- [ ] 容器正在运行 (`docker ps`)
- [ ] 端口映射正确
- [ ] 日志目录已创建并可写
- [ ] 证书目录挂载正确

## ✅ 部署后验证

### 基础功能测试

- [ ] 健康检查接口可访问
  ```bash
  curl http://localhost:8080/api/payment/health
  ```
- [ ] 通过 Traefik 访问成功
  ```bash
  curl https://payment.qsgl.net/api/payment/health
  ```
- [ ] HTTPS 证书有效
- [ ] HTTP 自动重定向到 HTTPS

### API 功能测试

- [ ] 扫码支付接口测试通过
- [ ] 电子钱包支付接口测试通过
- [ ] 订单查询接口测试通过
- [ ] 错误处理正常
- [ ] 超时处理正常

### 日志和监控

- [ ] 应用日志正常写入
- [ ] 日志级别适当（生产环境建议 Information）
- [ ] 错误日志能正常记录
- [ ] Docker 日志可查看
- [ ] 日志轮转配置（可选）

### 安全检查

- [ ] 证书文件权限正确（只读）
- [ ] 密码未在日志中显示
- [ ] API 不暴露敏感信息
- [ ] 防火墙规则合理
- [ ] 容器以非 root 用户运行（可选）

### 性能测试

- [ ] 单个请求响应时间 < 3秒
- [ ] 并发请求测试通过
- [ ] 内存使用正常
- [ ] CPU 使用正常

## ✅ 文档和培训

- [ ] README.md 文档完整
- [ ] API 文档已提供
- [ ] 部署文档已更新
- [ ] 运维人员已培训
- [ ] 联系方式已记录

## ✅ 备份和恢复

- [ ] 配置文件已备份
- [ ] 证书文件已备份
- [ ] 回滚方案已准备
- [ ] 数据备份策略已制定（如适用）

## ✅ 监控和告警

- [ ] 服务监控已配置（可选）
- [ ] 告警规则已设置（可选）
- [ ] 日志聚合已配置（可选）
- [ ] 性能监控已启用（可选）

## 📝 部署记录

**部署日期**: _______________

**部署人员**: _______________

**版本号**: _______________

**服务器**: api.qsgl.net

**域名**: payment.qsgl.net

**特殊说明**:
```
（记录本次部署的特殊情况、遇到的问题及解决方案）
```

## 🆘 故障联系人

**技术负责人**: _______________

**联系电话**: _______________

**备用联系**: _______________

---

## 部署命令快速参考

```bash
# 本地构建测试
dotnet build -c Release

# 上传证书
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r cert root@api.qsgl.net:/opt/certs/

# 上传项目
scp -i K:\Key\tx.qsgl.net_id_ed25519 -r * root@api.qsgl.net:/opt/payment/

# SSH 登录
ssh -i K:\Key\tx.qsgl.net_id_ed25519 root@api.qsgl.net

# 部署容器
cd /opt/payment
docker-compose up -d --build

# 查看日志
docker logs -f payment-gateway

# 检查状态
docker ps | grep payment
curl http://localhost:8080/api/payment/health
curl https://payment.qsgl.net/api/payment/health
```

---

**所有检查项完成后，方可上线！** ✅
