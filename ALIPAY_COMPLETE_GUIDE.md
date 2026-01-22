# 支付宝支付完整接口说明

## 概述

本文档描述农行综合收银台支付宝支付的完整接口功能，包括6种支付方式和2种辅助接口。

## 支付方式对比

| 支付方式 | PaymentLinkType | 接口路径 | 适用场景 | 用户操作 |
|---------|----------------|----------|---------|---------|
| **扫码支付（被扫）** | 2 | `/api/payment/alipay/qrcode` | PC网站、线下商户 | 用户扫商户二维码 |
| **WAP支付** | 1 | `/api/payment/alipay/wap` | 手机网站H5 | 跳转到支付宝APP/网页 |
| **PC网页支付** | 1 | `/api/payment/alipay/pc` | 电脑网站 | 跳转到支付宝网页 |
| **APP支付** | 3 | `/api/payment/alipay/app` | 移动APP | APP调起支付宝SDK |
| **付款码支付（主扫）** | 4 | `/api/payment/alipay/barcode` | 线下收银台、POS机 | 商户扫用户付款码 |
| **订单查询** | - | `/api/payment/alipay/query/{orderNo}` | 所有场景 | 查询订单状态 |
| **退款** | - | `/api/payment/alipay/refund` | 所有场景 | 订单退款 |

## 接口详细说明

### 1. 扫码支付（被扫模式）

**接口地址**: `POST /api/payment/alipay/qrcode`

**应用场景**: 
- PC网站收银台
- 线下商户展示二维码
- 自助终端机

**请求示例**:
```bash
curl -X POST "https://payment.qsgl.net/api/payment/alipay/qrcode" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "ORD20260115001",
    "amount": 100.00,
    "merchantId": "103881636900016",
    "goodsName": "测试商品",
    "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    "expiredDate": "30"
  }'
```

**响应示例**:
```json
{
  "isSuccess": true,
  "orderNo": "ORD20260115001",
  "transactionId": "ABC20260115123456001",
  "qrCodeUrl": "https://qr.alipay.com/bax00000000000000000",
  "amount": 100.00,
  "status": "PENDING",
  "message": "支付订单创建成功",
  "expireTime": "2026-01-15T15:30:00"
}
```

**农行参数**:
```json
{
  "TrxType": "EWalletPayReq",
  "PaymentType": "D",
  "PaymentLinkType": "2"
}
```

---

### 2. WAP支付（手机网页）

**接口地址**: `POST /api/payment/alipay/wap`

**应用场景**: 
- 手机浏览器网站
- 微信内嵌网页
- H5商城

**请求示例**:
```bash
curl -X POST "https://payment.qsgl.net/api/payment/alipay/wap" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "ORD20260115002",
    "amount": 88.88,
    "merchantId": "103881636900016",
    "goodsName": "商品购买",
    "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    "returnUrl": "https://example.com/payment/result",
    "quitUrl": "https://example.com"
  }'
```

**响应示例**:
```json
{
  "isSuccess": true,
  "orderNo": "ORD20260115002",
  "transactionId": "ABC20260115123456002",
  "paymentUrl": "https://openapi.alipay.com/gateway.do?...",
  "amount": 88.88,
  "status": "SUCCESS",
  "message": "支付订单创建成功"
}
```

**使用流程**:
1. 调用接口获取 `paymentUrl`
2. 在手机浏览器中跳转到 `paymentUrl`
3. 自动唤起支付宝APP或显示支付宝网页
4. 用户完成支付后，跳转到 `returnUrl`

**农行参数**:
```json
{
  "TrxType": "EWalletPayReq",
  "PaymentType": "D",
  "PaymentLinkType": "1",
  "ReturnURL": "https://example.com/payment/result",
  "QuitURL": "https://example.com"
}
```

---

### 3. PC网页支付（电脑网站）

**接口地址**: `POST /api/payment/alipay/pc`

**应用场景**: 
- 电商网站收银台
- PC端在线支付
- 桌面应用内嵌浏览器

**请求示例**:
```bash
curl -X POST "https://payment.qsgl.net/api/payment/alipay/pc" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "ORD20260115003",
    "amount": 288.00,
    "merchantId": "103881636900016",
    "goodsName": "电脑商品购买",
    "notifyUrl": "https://payment.qsgl.net/api/payment/notify",
    "returnUrl": "https://example.com/payment/result"
  }'
```

**响应示例**:
```json
{
  "isSuccess": true,
  "orderNo": "ORD20260115003",
  "transactionId": "ABC20260115123456003",
  "paymentUrl": "https://openapi.alipay.com/gateway.do?...",
  "amount": 288.00,
  "status": "SUCCESS",
  "message": "支付订单创建成功"
}
```

**农行参数**:
```json
{
  "TrxType": "EWalletPayReq",
  "PaymentType": "D",
  "PaymentLinkType": "1"
}
```

---

### 4. APP支付

**接口地址**: `POST /api/payment/alipay/app`

**应用场景**: 
- iOS/Android APP
- 移动应用内支付
- APP内购

**请求示例**:
```bash
curl -X POST "https://payment.qsgl.net/api/payment/alipay/app" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "ORD20260115004",
    "amount": 199.99,
    "merchantId": "103881636900016",
    "goodsName": "APP商品购买",
    "notifyUrl": "https://payment.qsgl.net/api/payment/notify"
  }'
```

**响应示例**:
```json
{
  "isSuccess": true,
  "orderNo": "ORD20260115004",
  "transactionId": "ABC20260115123456004",
  "orderString": "app_id=2021000000000000&biz_content=...",
  "amount": 199.99,
  "status": "PENDING",
  "message": "支付订单创建成功"
}
```

**APP端集成步骤**:
1. 调用接口获取 `orderString`
2. 将 `orderString` 传给支付宝SDK
3. 支付宝SDK调起支付宝APP
4. 用户完成支付后返回APP

**农行参数**:
```json
{
  "TrxType": "EWalletPayReq",
  "PaymentType": "D",
  "PaymentLinkType": "3"
}
```

---

### 5. 付款码支付（主扫模式）

**接口地址**: `POST /api/payment/alipay/barcode`

**应用场景**: 
- 线下收银台
- POS机扫码
- 扫码枪支付

**请求示例**:
```bash
curl -X POST "https://payment.qsgl.net/api/payment/alipay/barcode" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "ORD20260115005",
    "amount": 68.00,
    "merchantId": "103881636900016",
    "goodsName": "线下扫码支付",
    "authCode": "280123456789012345",
    "notifyUrl": "https://payment.qsgl.net/api/payment/notify"
  }'
```

**响应示例（成功）**:
```json
{
  "isSuccess": true,
  "orderNo": "ORD20260115005",
  "transactionId": "ABC20260115123456005",
  "thirdOrderNo": "2026011522001234567890123456",
  "amount": 68.00,
  "status": "SUCCESS",
  "message": "支付成功",
  "payTime": "2026-01-15T16:00:00"
}
```

**响应示例（用户支付中）**:
```json
{
  "isSuccess": false,
  "orderNo": "ORD20260115005",
  "status": "USERPAYING",
  "message": "用户支付中，请轮询查询",
  "errorCode": "1002"
}
```

**状态说明**:
- `SUCCESS`: 支付成功
- `USERPAYING`: 用户支付中（需要轮询查询，间隔2-5秒）
- `FAILED`: 支付失败

**农行参数**:
```json
{
  "TrxType": "EWalletPayReq",
  "PaymentType": "D",
  "PaymentLinkType": "4",
  "PayQRCode": "280123456789012345"
}
```

---

### 6. 订单查询

**接口地址**: `GET /api/payment/alipay/query/{orderNo}`

**应用场景**: 
- 查询订单状态
- 轮询付款码支付结果
- 对账系统

**请求示例**:
```bash
curl -X GET "https://payment.qsgl.net/api/payment/alipay/query/ORD20260115001" \
  -H "X-Merchant-Id: 103881636900016"
```

**响应示例**:
```json
{
  "isSuccess": true,
  "orderNo": "ORD20260115001",
  "transactionId": "ABC20260115123456001",
  "thirdOrderNo": "2026011522001234567890123456",
  "amount": 100.00,
  "status": "SUCCESS",
  "payTime": "2026-01-15T14:30:00",
  "message": "查询成功"
}
```

**订单状态**:
- `PENDING`: 待支付
- `SUCCESS`: 支付成功
- `FAILED`: 支付失败
- `CLOSED`: 已关闭
- `UNKNOWN`: 未知

**农行参数**:
```json
{
  "TrxType": "OrderQuery",
  "OrderNo": "ORD20260115001"
}
```

---

### 7. 退款

**接口地址**: `POST /api/payment/alipay/refund`

**应用场景**: 
- 订单退款
- 部分退款
- 售后退款

**请求示例**:
```bash
curl -X POST "https://payment.qsgl.net/api/payment/alipay/refund" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "ORD20260115001",
    "refundAmount": 50.00,
    "refundReason": "用户申请退款",
    "merchantId": "103881636900016"
  }'
```

**响应示例**:
```json
{
  "isSuccess": true,
  "orderNo": "ORD20260115001",
  "refundNo": "RF20260115123456001",
  "refundAmount": 50.00,
  "status": "SUCCESS",
  "message": "退款成功",
  "refundTime": "2026-01-15T15:00:00"
}
```

**退款状态**:
- `SUCCESS`: 退款成功
- `PROCESSING`: 处理中
- `FAILED`: 退款失败

**农行参数**:
```json
{
  "TrxType": "Refund",
  "OrderNo": "ORD20260115001",
  "RefundAmount": "50.00",
  "RefundReason": "用户申请退款"
}
```

---

## 技术参数说明

### 农行V3.0.0消息格式

所有支付宝接口都使用农行V3.0.0嵌套JSON格式：

```json
{
  "Message": {
    "Version": "V3.0.0",
    "Format": "JSON",
    "Merchant": {
      "ECMerchantType": "EBUS",
      "MerchantID": "103881636900016"
    },
    "TrxRequest": {
      "TrxType": "EWalletPayReq",
      "PaymentType": "D",
      "PaymentLinkType": "2",
      "OrderNo": "ORD20260115001",
      "OrderAmount": "100.00",
      "OrderTime": "20260115143000",
      "NotifyType": "1",
      "OrderDesc": "测试商品",
      "ProductName": "测试商品",
      "ResultNotifyURL": "https://payment.qsgl.net/api/payment/notify"
    }
  }
}
```

### PaymentLinkType 对照表

| 值 | 支付方式 | 说明 |
|----|---------|------|
| 1 | 主扫模式 | PC/WAP支付，用户跳转到支付宝页面 |
| 2 | 被扫模式 | 扫码支付，用户扫商户二维码 |
| 3 | APP支付 | APP调起支付宝SDK |
| 4 | 付款码支付 | 商户扫用户付款码 |

### 通用参数说明

| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| orderNo | string | 是 | 商户订单号，唯一标识 |
| amount | decimal | 是 | 支付金额，单位：元 |
| merchantId | string | 是 | 商户号 |
| goodsName | string | 是 | 商品名称/订单描述 |
| notifyUrl | string | 否 | 支付回调地址 |
| returnUrl | string | 否 | 支付完成返回地址（PC/WAP必填） |
| quitUrl | string | 否 | 用户退出返回地址 |

---

## 错误代码对照表

| 错误代码 | 说明 | 处理方式 |
|---------|------|---------|
| 0000 | 成功 | 正常流程 |
| 1002 | 用户支付中 | 轮询查询订单状态 |
| 2308 | 商户无可用支付方式 | 联系农行配置支付方式 |
| AP7346 | 缺少订单详情 | 检查OrderDesc字段 |
| PARAM_ERROR | 参数错误 | 检查请求参数 |
| AUTHCODE_INVALID | 付款码无效 | 重新扫描付款码 |
| ORDER_NOT_FOUND | 订单不存在 | 检查订单号 |

---

## 最佳实践

### 1. 付款码支付轮询

当返回 `USERPAYING` 状态时，需要轮询查询：

```javascript
async function pollPaymentStatus(orderNo) {
  const maxAttempts = 10;
  const interval = 3000; // 3秒
  
  for (let i = 0; i < maxAttempts; i++) {
    await sleep(interval);
    const result = await queryOrder(orderNo);
    
    if (result.status === 'SUCCESS') {
      return { success: true, result };
    } else if (result.status === 'FAILED') {
      return { success: false, result };
    }
  }
  
  return { success: false, message: '轮询超时' };
}
```

### 2. 二维码过期处理

扫码支付的二维码默认30分钟过期，建议：
- 前端倒计时显示
- 过期后自动刷新二维码
- 提供手动刷新按钮

### 3. 回调验证

所有支付方式都支持异步回调，必须验证：
- 签名验证
- 订单金额验证
- 订单状态验证
- 幂等性处理

---

## 测试建议

### 1. 扫码支付测试
```bash
# 生成二维码
curl -X POST "https://payment.qsgl.net/api/payment/alipay/qrcode" \
  -H "Content-Type: application/json" \
  -d '{"orderNo":"TEST001","amount":0.01,"merchantId":"103881636900016","goodsName":"测试"}'
```

### 2. 付款码支付测试
```bash
# 使用支付宝沙箱环境的测试付款码
curl -X POST "https://payment.qsgl.net/api/payment/alipay/barcode" \
  -H "Content-Type: application/json" \
  -d '{"orderNo":"TEST002","amount":0.01,"merchantId":"103881636900016","goodsName":"测试","authCode":"280123456789012345"}'
```

---

## 常见问题

**Q: PC支付和WAP支付有什么区别？**  
A: 两者技术实现相同（PaymentLinkType=1），区别在于：
- PC支付：电脑浏览器，跳转到支付宝网页扫码
- WAP支付：手机浏览器，自动唤起支付宝APP

**Q: 付款码支付为什么返回USERPAYING？**  
A: 用户需要输入密码或进行其他验证，此时应轮询查询订单状态。

**Q: 如何判断支付是否成功？**  
A: 优先使用异步回调通知，其次通过订单查询接口确认。

**Q: 退款是否实时到账？**  
A: 退款到账时间取决于支付宝，一般1-3个工作日。

---

## 更新日志

- **2026-01-15**: 完成所有支付宝支付方式的接口实现
- **2026-01-15**: 添加订单查询和退款接口
- **2026-01-15**: 更新Swagger文档

---

## 参考文档

- [农行综合收银台开发文档](https://bank.u51.com/ebus-two/docs/)
- [Swagger API 文档](https://payment.qsgl.net/swagger)
- [支付宝官方文档](https://opendocs.alipay.com/)
