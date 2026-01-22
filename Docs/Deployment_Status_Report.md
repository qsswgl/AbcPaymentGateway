# 农行页面支付接口部署状态报告

**生成时间**: 2026年1月19日 09:50  
**系统版本**: .NET 10.0 + Docker  
**部署环境**: 生产服务器 (https://payment.qsgl.net)

---

## 📋 执行摘要

✅ **部署状态**: 成功部署  
✅ **接口状态**: 可访问（已修复404错误）  
⚠️ **交易状态**: 返回 EUNKWN（农行权限/配置待确认）  
✅ **证书状态**: 商户证书加载正常  

---

## 🏗️ 部署架构

### 系统组件
```
┌─────────────────────────────────────────┐
│  Traefik (反向代理 & SSL终止)           │
│  https://payment.qsgl.net               │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  Docker Container: payment-gateway      │
│  - 镜像: payment-gateway-jit:latest     │
│  - 镜像ID: 92b2f518c4d9                 │
│  - 构建时间: 2026-01-19 09:45:25        │
│  - 状态: Running (healthy)              │
│  - 内部端口: 8080                       │
└─────────────────────────────────────────┘
```

### 证书配置
- **商户证书**: `/app/cert/103881636900016.pfx`
  - 有效期: 至 2031/01/05 10:56:49 ✅
  - 主题: CN=EBUS.merchant.103881636900016.103881636900016.0000
  - 序列号: 7B97CA10275A16B1CEF3

- **TrustPay证书**: `/app/cert/prod/TrustPay.cer`
  - 有效期: 至 2023/08/11 13:38:49 ⚠️ **已过期**
  - 主题: CN=MainServer.0001

---

## 📦 已部署接口

### 页面支付下单接口

**接口路径**: `POST /api/payment/abc/pagepay`

**请求示例**:
```json
{
  "orderNo": "TEST202601190945001",
  "amount": 10.00,
  "merchantId": "103881636900016",
  "goodsName": "测试商品",
  "notifyUrl": "https://payment.qsgl.net/api/payment/abc/notify",
  "merchantSuccessUrl": "https://payment.qsgl.net/success",
  "merchantErrorUrl": "https://payment.qsgl.net/error"
}
```

**当前响应**:
```json
{
  "isSuccess": false,
  "orderNo": "TEST202601190945001",
  "transactionId": "",
  "paymentURL": "",
  "amount": 10.00,
  "status": "UNKNOWN",
  "message": "交易结果未知，请查询订单状态确认 (EUNKWN)",
  "expireTime": "2026-01-19T10:16:04.216905+08:00",
  "errorCode": "EUNKWN",
  "returnCode": "EUNKWN"
}
```

---

## 🔍 农行接口交互详情

### 发送到农行的请求

**目标URL**: `https://pay.abchina.com:443/ebus/ReceiveMerchantTrxReqServlet`

**请求格式**: JSON (MSG封装)

**关键字段**:
```json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",
      "Format": "JSON",
      "Merchant": {
        "ECMerchantType": "EBUS",
        "MerchantID": "103881636900016"
      },
      "TrxRequest": {
        "TrxType": "PayReq",  // ✅ 已修正（原为 OrderReq）
        "Order": {
          "PayTypeID": "ImmediatePay",
          "OrderNo": "TEST202601190945001",
          "OrderAmount": "10.00",
          "OrderDate": "2026/01/19",
          "OrderTime": "09:46:03",
          "CurrencyCode": "156",
          "CommodityType": "0201",
          "ExpiredDate": "30"
        },
        "OrderDetail": [...],
        "PaymentType": "A",  // A=支付方式合并（借记卡+贷记卡）
        "PaymentLinkType": "1",  // 1=电脑网络
        "NotifyType": "1",
        "ResultNotifyURL": "https://payment.qsgl.net/api/payment/abc/notify",
        "MerchantSuccessURL": "https://payment.qsgl.net/success",
        "MerchantErrorURL": "https://payment.qsgl.net/error"
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "ER0jRmvKC7QwI1eK7r2U0+ukEhA5j2SK..." // 签名正常生成
  }
}
```

### 农行返回的响应

**HTTP状态**: 200 OK ✅

**响应内容**:
```json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",
      "Format": "JSON",
      "Common": {
        "Channel": "EBUS"
      },
      "Merchant": {
        "ECMerchantType": "EBUS",
        "MerchantID": ""
      },
      "TrxResponse": {
        "ReturnCode": "EUNKWN",
        "ErrorMessage": "交易结果未知，请进行查证明确交易结果，No message available"
      }
    }
  }
}
```

**关键发现**:
- ✅ 农行服务器正常响应（HTTP 200）
- ✅ 签名验证通过（否则会返回 APE400）
- ✅ 商户信息识别成功（否则会返回 APE002）
- ⚠️ 返回 EUNKWN 且提示 "No message available"

---

## 🔧 部署过程详情

### 构建日志
```
构建时间: 2026-01-19 09:45:13 - 09:45:25
构建耗时: 约12秒
镜像大小: 123MB (Alpine Linux基础镜像)
```

### 关键修改
1. **TrxType修正**: `OrderReq` → `PayReq`
2. **Dockerfile修正**: `dotnet/runtime:10.0` → `dotnet/aspnet:10.0-alpine`
3. **证书挂载**: 确认生产证书正确挂载

### 容器健康状态
```bash
# 容器运行时间: 约5分钟
# 状态: Up, health: healthy
# 监听端口: 8080 (内部)
# 外部访问: https://payment.qsgl.net (Traefik反向代理)
```

---

## 🐛 问题分析

### EUNKWN 错误原因推测

根据农行返回的错误信息分析：

#### 可能原因1️⃣: 商户配置问题
- **症状**: "No message available" 提示
- **可能性**: 商户虽然开通了权限，但缺少必要的配置参数
- **建议**: 向ABC银行确认以下配置是否完整：
  - [ ] 页面支付（PayReq）权限是否激活
  - [ ] MerchantSuccessURL/MerchantErrorURL 是否需要预先登记
  - [ ] 是否需要配置特定的 CommodityType（当前使用 0201）

#### 可能原因2️⃣: 测试环境不匹配
- **症状**: 使用生产环境URL但可能是测试商户
- **当前配置**: `https://pay.abchina.com:443` (生产环境)
- **商户号**: `103881636900016`
- **建议**: 确认商户号与环境URL的匹配关系

#### 可能原因3️⃣: 缺少必填字段
- **症状**: 农行无法识别请求内容
- **已发送字段**: Order, OrderDetail, PaymentType, NotifyType, URLs
- **建议**: 向ABC银行确认 PayReq 是否需要额外字段：
  - ReceiveAccount（收款账号）
  - ReceiveAccName（收款户名）
  - VerifyFlag（实名验证标识）

#### 可能原因4️⃣: TrustPay证书过期
- **症状**: 证书已于 2023/08/11 过期
- **影响**: 可能影响响应验签
- **建议**: 向ABC银行申请新的 TrustPay.cer 证书

---

## 📊 对比测试结果

### 一码多扫接口（OLScanPayOrderReq）
- **状态**: 同样返回 EUNKWN
- **农行确认**: ep241 已开通，支持一码多扫
- **推测**: 问题可能不在权限，而在其他配置

### 扫码支付接口（ScanPayOrderReq）
- **状态**: 未测试
- **建议**: 可作为对照组测试

---

## ✅ 已确认的正常功能

1. ✅ Docker 镜像构建成功
2. ✅ 容器启动并健康运行
3. ✅ SSL/HTTPS 配置正常
4. ✅ 商户证书加载成功
5. ✅ 接口路由正常（不再404）
6. ✅ 请求签名生成正常
7. ✅ 农行服务器连接正常
8. ✅ HTTP 通信成功（200 OK）

---

## 🎯 待确认事项（需ABC银行协助）

### 高优先级
1. **商户页面支付配置确认**
   - [ ] PayReq 交易类型是否已为商户 103881636900016 开通
   - [ ] 是否需要在农行后台配置回调URL白名单
   - [ ] CommodityType "0201"（虚拟商品）是否允许

2. **证书问题**
   - [ ] TrustPay.cer 已过期（2023-08-11），是否影响交易
   - [ ] 是否需要更新验签证书

3. **必填字段确认**
   - [ ] PayReq 的完整必填字段列表
   - [ ] ReceiveAccount 和 ReceiveAccName 是否必填
   - [ ] 是否需要 VerifyFlag 实名验证

### 中优先级
4. **环境配置**
   - [ ] 确认商户 103881636900016 应使用的环境（测试/生产）
   - [ ] 确认正确的接入URL

5. **返回字段**
   - [ ] PaymentURL 应该在什么情况下返回
   - [ ] EUNKWN 的具体含义（是权限问题还是配置问题）

---

## 📝 日志记录

### 完整交互日志（2026-01-19 09:46:03）

```
[INFO] 农行页面支付下单请求: OrderNo=TEST202601190945001, Amount=10.00
[INFO] 开始农行页面支付下单: OrderNo=TEST202601190945001, Amount=10.00
[INFO] 发送请求到: https://pay.abchina.com:443/ebus/ReceiveMerchantTrxReqServlet
[INFO] 发送MSG格式 (JSON): {...完整JSON见上文...}
[INFO] HTTP Request: POST https://pay.abchina.com/ebus/ReceiveMerchantTrxReqServlet
[INFO] HTTP Response: 200 OK (218.4508ms)
[INFO] 收到农行响应: {"MSG":{"Message":{...ReturnCode":"EUNKWN"...}}}
[INFO] 解析农行响应成功: ReturnCode=EUNKWN
[WARN] 交易结果未知(EUNKWN)，建议客户端查询订单状态: OrderNo=TEST202601190945001
```

---

## 🚀 下一步行动计划

### 立即执行
1. ✅ 生成诊断脚本测试不同交易类型
2. ✅ 整理入参/出参文档提供给ABC银行
3. ⏳ 向ABC银行反馈当前状态，提供完整请求报文

### 待ABC银行反馈后
4. ⏳ 根据ABC银行要求调整必填字段
5. ⏳ 更新 TrustPay.cer 证书（如需要）
6. ⏳ 调整商户配置（如需要）
7. ⏳ 重新测试并验证 PaymentURL 返回

---

## 📞 联系信息

**技术支持**: support@qsgl.net  
**服务地址**: https://payment.qsgl.net  
**API文档**: https://payment.qsgl.net/swagger  
**日志查看**: `ssh root@tx.qsgl.net 'docker logs payment-gateway'`

---

**报告生成**: GitHub Copilot  
**最后更新**: 2026年1月19日 09:50
