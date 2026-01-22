# 农行页面支付接口开发与测试文档

## 📋 开发内容

### 新增文件

1. **Models/AbcPagePayModels.cs** - 页面支付请求/响应模型
2. **Controllers/AbcPaymentController.cs** - 新增 `pagepay` 接口
3. **Services/AbcPaymentService.cs** - 新增 `ProcessAbcPagePayAsync` 方法
4. **Scripts/Test-PagePay-Live.ps1** - 页面支付测试脚本

### 修改文件

1. **Models/PaymentResponse.cs** - 添加 `PaymentURL` 字段
2. **Services/AbcPaymentService.cs** - 添加解析 `PaymentURL` 逻辑

## 🎯 接口说明

### 接口路径
```
POST /api/payment/abc/pagepay
```

### 请求参数

```json
{
  "orderNo": "ORD20260119001",
  "amount": 10.00,
  "merchantId": "103881636900016",
  "goodsName": "结算单支付",
  "orderDesc": "结算单测试-2026年1月19日08:45",
  "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
  "merchantSuccessUrl": "https://payment.qsgl.net/success",
  "merchantErrorUrl": "https://payment.qsgl.net/fail",
  "payTypeID": "ImmediatePay",
  "paymentType": "A",
  "paymentLinkType": "1",
  "commodityType": "0201"
}
```

### 响应示例（成功）

```json
{
  "isSuccess": true,
  "orderNo": "ORD20260119001",
  "transactionId": "ABC202601190001",
  "paymentURL": "https://pay.abchina.com/ebus/PaymentLink?id=xxx",
  "amount": 10.00,
  "status": "SUCCESS",
  "message": "订单创建成功",
  "expireTime": "2026-01-19T12:00:00",
  "errorCode": "0000",
  "returnCode": "0000"
}
```

### 响应示例（失败）

```json
{
  "isSuccess": false,
  "orderNo": "ORD20260119001",
  "transactionId": "",
  "paymentURL": "",
  "amount": 10.00,
  "status": "FAILED",
  "message": "商户号不能为空",
  "errorCode": "PARAM_ERROR",
  "returnCode": "PARAM_ERROR"
}
```

## 🔑 关键字段说明

| 字段 | 必填 | 说明 |
|------|------|------|
| orderNo | ✅ | 商户订单号，唯一标识 |
| amount | ✅ | 支付金额（元） |
| merchantId | ✅ | 农行商户号 |
| goodsName | ✅ | 商品名称 |
| notifyUrl | ✅ | 支付回调通知地址 |
| merchantSuccessUrl | ✅ | 支付成功返回地址 |
| merchantErrorUrl | ✅ | 支付失败返回地址 |
| paymentURL | ❌ | **响应字段** - 支付页面URL |

## 🆚 页面支付 vs 扫码支付对比

| 特性 | 页面支付 (pagepay) | 扫码支付 (scanpay) |
|------|-------------------|-------------------|
| 接口类型 | OrderReq | OLScanPayOrderReq |
| 返回字段 | PaymentURL | ScanPayQRURL |
| 使用场景 | PC网站、H5页面跳转 | 扫二维码支付 |
| 用户体验 | 跳转到农行支付页 | 扫码后在APP内支付 |
| 返回URL | 需要merchantSuccessUrl/ErrorUrl | 不需要 |

## 📝 Swagger文档示例

接口已添加详细的Swagger文档注释，包括：

- 接口描述
- 请求/响应示例
- 参数说明
- 状态码说明

访问 `https://payment.qsgl.net/swagger` 查看完整API文档。

## 🧪 测试步骤

### 1. 手动测试（PowerShell）

```powershell
cd K:\payment\AbcPaymentGateway\Scripts
.\Test-PagePay-Live.ps1
```

### 2. 手动测试（Curl）

```bash
curl -X POST https://payment.qsgl.net/api/payment/abc/pagepay \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "TEST_PAGE_001",
    "amount": 10.00,
    "merchantId": "103881636900016",
    "goodsName": "测试商品",
    "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    "merchantSuccessUrl": "https://payment.qsgl.net/success",
    "merchantErrorUrl": "https://payment.qsgl.net/fail",
    "payTypeID": "ImmediatePay",
    "paymentType": "A",
    "paymentLinkType": "1",
    "commodityType": "0201"
  }'
```

### 3. 测试脚本功能

`Test-PagePay-Live.ps1` 脚本会自动：

1. ✅ 生成唯一订单号
2. ✅ 发送POST请求到API
3. ✅ 解析响应结果
4. ✅ 如果成功，自动生成PaymentURL的二维码
5. ✅ 在默认图片查看器中打开二维码
6. ✅ 保存完整响应到JSON文件

## 🖼️ 二维码生成

### PaymentURL二维码的作用

- 用户扫描二维码后直接跳转到农行支付页面
- 适用于移动端扫码支付场景
- 与扫码支付的ScanPayQRURL类似，但指向的是页面而不是支付SDK

### 二维码保存位置

```
K:\payment\AbcPaymentGateway\Scripts\QRCodes\ABC_PAGE_yyyyMMddHHmmss.png
```

## ⚠️ 当前状态与问题排查

### 部署状态

✅ **已完成：**
- 代码开发完成
- 模型定义完成
- 控制器接口完成
- 服务层逻辑完成
- 测试脚本准备完成

❌ **待解决：**
- 服务器部署后接口返回404
- 可能原因：
  1. Docker镜像构建时未包含最新代码
  2. 控制器路由未正确注册
  3. Traefik路由配置问题

### 排查建议

1. **检查容器是否使用最新镜像**
   ```bash
   ssh root@tx.qsgl.net "docker images | grep payment"
   ```

2. **检查swagger.json是否包含/api/payment/abc/pagepay**
   ```powershell
   Invoke-RestMethod -Uri "https://payment.qsgl.net/swagger.json" | ConvertTo-Json -Depth 10 | Select-String -Pattern "pagepay"
   ```

3. **重新构建并部署**
   ```powershell
   cd K:\payment\AbcPaymentGateway
   .\deploy-remote-build.ps1
   ```

4. **查看容器日志**
   ```bash
   ssh root@tx.qsgl.net "docker logs payment-gateway --tail 50"
   ```

## 📌 后续步骤

### 立即执行

1. ✅ 确认服务器部署最新版本
2. ✅ 测试接口是否可访问
3. ✅ 执行Test-PagePay-Live.ps1测试
4. ✅ 确认PaymentURL是否正确返回
5. ✅ 生成二维码并测试扫码跳转

### 生产环境准备

1. 更新TrustPay.cer证书（当前已过期）
2. 确认商户号103881636900016已开通页面支付功能
3. 配置正确的成功/失败返回URL
4. 完善支付回调处理逻辑
5. 添加订单查询接口
6. 完善异常处理和日志记录

## 📞 技术支持

如遇问题，请提供以下信息：

1. 订单号
2. 完整请求参数
3. 完整响应内容
4. 服务器日志（最近50行）
5. 错误截图

---

**文档生成时间：** 2026-01-19 09:40  
**接口版本：** V1.0  
**开发状态：** 代码完成，待部署测试
