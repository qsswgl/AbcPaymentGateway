# 📖 农行支付网关 API - 文档索引 (Native AOT 版本)

欢迎使用农行支付网关 API 项目！本项目采用 .NET 10 Native AOT 编译，提供高性能部署方案。

## ⚡ Native AOT 特性

- **编译模式**: Native AOT (提前编译)
- **启动速度**: < 1秒 (提升 3-5倍)
- **内存占用**: ~40MB (减少 60%)
- **镜像大小**: ~80MB (减少 60%)

## 🎯 我想...

### 立即开始部署
👉 **[START_HERE.md](START_HERE.md)** - 5分钟 Native AOT 快速部署 ⭐

### 了解 Native AOT
👉 **[NATIVE_AOT.md](NATIVE_AOT.md)** - Native AOT 编译和部署指南

### 了解项目概况
👉 **[README.md](README.md)** - 项目介绍和功能特性

### 3步完成部署
👉 **[QUICKSTART.md](QUICKSTART.md)** - 快速开始指南

### 详细部署说明
👉 **[DEPLOYMENT.md](DEPLOYMENT.md)** - 完整部署文档

### 移动端集成代码
👉 **[API_EXAMPLES.md](API_EXAMPLES.md)** - Android/iOS/React Native 示例

### 上线前检查
👉 **[DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md)** - 部署检查清单

### 了解项目成果
👉 **[PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)** - 项目总结

## 📁 文件说明

### 核心代码文件

| 文件 | 说明 |
|------|------|
| `Program.cs` | 程序入口，配置服务和中间件（AOT 优化） |
| `AppJsonSerializerContext.cs` | JSON 源生成器（支持 AOT）|
| `Controllers/PaymentController.cs` | 支付 API 控制器 |
| `Services/AbcPaymentService.cs` | 支付业务逻辑服务 |
| `Models/PaymentRequest.cs` | 支付请求模型 |
| `Models/PaymentResponse.cs` | 支付响应模型 |
| `Models/AbcPaymentConfig.cs` | 配置模型 |

### 配置文件

| 文件 | 说明 |
|------|------|
| `appsettings.json` | 生产环境配置 |
| `appsettings.Development.json` | 开发环境配置 |
| `.env.example` | 环境变量示例 |
| `AbcPaymentGateway.csproj` | 项目文件（含 AOT 配置）|

### Docker 文件

| 文件 | 说明 |
|------|------|
| `Dockerfile` | Docker 镜像构建文件 |
| `docker-compose.yml` | Docker Compose 配置 |
| `.dockerignore` | Docker 构建忽略文件 |

### 部署脚本

| 文件 | 说明 |
|------|------|
| `deploy.ps1` | Windows PowerShell 自动部署脚本 |
| `deploy.sh` | Linux Bash 自动部署脚本 |
| `verify-deployment.ps1` | 部署验证脚本 |

### 测试文件

| 文件 | 说明 |
|------|------|
| `test-api.http` | HTTP 接口测试文件（VS Code REST Client） |

### 其他文件

| 文件 | 说明 |
|------|------|
| `.gitignore` | Git 版本控制忽略文件 |
| `AbcPaymentGateway.csproj` | .NET 项目文件 |

## 🗂️ 目录结构

```
AbcPaymentGateway/
│
├── 📄 文档（从这里开始）
│   ├── START_HERE.md                  ⭐ 立即开始
│   ├── README.md                      📖 项目说明
│   ├── QUICKSTART.md                  🚀 快速开始
│   ├── DEPLOYMENT.md                  📋 部署指南
│   ├── API_EXAMPLES.md                📱 移动端示例
│   ├── DEPLOYMENT_CHECKLIST.md        ✅ 检查清单
│   ├── PROJECT_SUMMARY.md             📊 项目总结
│   └── INDEX.md                       📑 本文件
│
├── 💻 源代码
│   ├── Program.cs                     程序入口
│   ├── Controllers/                   API 控制器
│   ├── Services/                      业务服务
│   └── Models/                        数据模型
│
├── ⚙️ 配置文件
│   ├── appsettings.json               生产配置
│   ├── appsettings.Development.json   开发配置
│   └── .env.example                   环境变量示例
│
├── 🐳 Docker 文件
│   ├── Dockerfile                     镜像构建
│   ├── docker-compose.yml             容器编排
│   └── .dockerignore                  构建忽略
│
├── 🚀 部署脚本
│   ├── deploy.ps1                     Windows 部署
│   ├── deploy.sh                      Linux 部署
│   └── verify-deployment.ps1          部署验证
│
├── 🧪 测试文件
│   └── test-api.http                  API 测试
│
└── 📁 运行时目录
    ├── cert/                          证书文件（需配置）
    └── logs/                          日志文件（自动生成）
```

## 🎓 学习路径

### 初学者路径

1. **了解项目** → [README.md](README.md)
2. **快速部署** → [START_HERE.md](START_HERE.md)
3. **测试接口** → [test-api.http](test-api.http)
4. **移动端集成** → [API_EXAMPLES.md](API_EXAMPLES.md)

### 运维人员路径

1. **部署指南** → [DEPLOYMENT.md](DEPLOYMENT.md)
2. **检查清单** → [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md)
3. **运行脚本** → [deploy.ps1](deploy.ps1)
4. **验证部署** → [verify-deployment.ps1](verify-deployment.ps1)

### 开发人员路径

1. **项目结构** → [README.md](README.md)
2. **源代码** → `Controllers/`, `Services/`, `Models/`
3. **配置管理** → `appsettings.json`
4. **API 文档** → [API_EXAMPLES.md](API_EXAMPLES.md)

## 🔍 快速查找

### 我想知道...

- **如何部署？** → [START_HERE.md](START_HERE.md)
- **API 怎么调用？** → [API_EXAMPLES.md](API_EXAMPLES.md)
- **支持哪些功能？** → [README.md](README.md)
- **如何配置证书？** → [QUICKSTART.md](QUICKSTART.md)
- **遇到问题怎么办？** → [DEPLOYMENT.md](DEPLOYMENT.md) 的故障排查部分
- **上线前要做什么？** → [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md)

### 我要...

- **本地测试** → 运行 `dotnet run`，查看 [QUICKSTART.md](QUICKSTART.md)
- **部署到服务器** → 运行 `.\deploy.ps1`，查看 [DEPLOYMENT.md](DEPLOYMENT.md)
- **查看日志** → `docker logs -f payment-gateway`
- **测试 API** → 使用 [test-api.http](test-api.http)
- **集成到 App** → 参考 [API_EXAMPLES.md](API_EXAMPLES.md)

## 📞 技术支持

### 问题排查顺序

1. 查看对应文档的"故障排查"部分
2. 检查应用日志和 Docker 日志
3. 参考 [DEPLOYMENT.md](DEPLOYMENT.md)
4. 联系技术支持

### 常见问题

- **容器启动失败** → [DEPLOYMENT.md](DEPLOYMENT.md) - 故障排查
- **API 调用失败** → [API_EXAMPLES.md](API_EXAMPLES.md) - 注意事项
- **证书配置错误** → [QUICKSTART.md](QUICKSTART.md) - 第一步

## 🎯 项目信息

- **技术栈**: .NET 10 + Docker + Traefik
- **部署地址**: https://payment.qsgl.net
- **服务器**: api.qsgl.net
- **版本**: v1.0.0
- **更新日期**: 2026年1月6日

## 📊 文档统计

| 文档类型 | 数量 | 文件 |
|---------|------|------|
| 用户指南 | 3 | START_HERE, QUICKSTART, README |
| 技术文档 | 2 | DEPLOYMENT, API_EXAMPLES |
| 运维文档 | 2 | DEPLOYMENT_CHECKLIST, PROJECT_SUMMARY |
| 总计 | 7 | + 本索引文件 |

## 🔖 快速链接

- **立即开始**: [START_HERE.md](START_HERE.md) ⭐
- **API 文档**: [API_EXAMPLES.md](API_EXAMPLES.md)
- **部署脚本**: [deploy.ps1](deploy.ps1)
- **检查清单**: [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md)

---

**提示**: 如果你是第一次使用，建议从 [START_HERE.md](START_HERE.md) 开始！

**需要帮助？** 所有文档都包含详细说明和示例代码。
