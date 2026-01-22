# 农行一码多扫接口诊断报告

## 📋 基本信息

- **商户号**: 103881636900016
- **环境**: 生产环境 (pay.abchina.com:443)
- **接口**: 一码多扫线上扫码下单 (OLScanPayOrderReq)
- **测试时间**: 2026-01-17

## ✅ 已完成的工作

### 1. 接口开发
- ✅ Models创建 (`AbcScanPayModels.cs`)
- ✅ 服务实现 (`ProcessAbcScanPayAsync`, `BuildAbcScanPayRequestData`)
- ✅ 控制器 (`AbcPaymentController.cs`)
- ✅ Swagger文档更新

### 2. 技术实现
- ✅ V3.0.0 JSON格式报文
- ✅ SHA1withRSA数字签名
- ✅ Order对象结构（包含订单基本信息）
- ✅ OrderDetail数组（包含商品明细）
- ✅ GB18030编码支持

### 3. 错误处理优化
- ✅ 20+农行错误码映射
- ✅ 友好错误消息提示
- ✅ EUNKWN特殊处理
- ✅ ScanPayQRURL字段解析
- ✅ 详细日志记录

## ⚠️ 当前问题

### 错误现象
```json
{
  "isSuccess": false,
  "orderNo": "ORD20260117ScanPay005",
  "status": "UNKNOWN",
  "message": "交易结果未知，请稍后查询订单状态或联系客服确认 (EUNKWN)",
  "errorCode": "EUNKWN",
  "scanPayQRURL": ""
}
```

### 农行原始响应
```json
{
  "MSG": {
    "Message": {
      "Version": "V3.0.0",
      "Format": "JSON",
      "Common": {"Channel": "EBUS"},
      "Merchant": {
        "ECMerchantType": "EBUS",
        "MerchantID": ""  // ⚠️ 返回空字符串
      },
      "TrxResponse": {
        "ReturnCode": "EUNKWN",
        "ErrorMessage": "交易结果未知，请进行查证明确交易结果，No message available"
      }
    }
  }
}
```

### 请求报文示例
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
        "TrxType": "OLScanPayOrderReq",
        "Order": {
          "PayTypeID": "ImmediatePay",
          "OrderNo": "ORD20260117ScanPay005",
          "OrderAmount": "100.00",
          "OrderDate": "2026/01/17",
          "OrderTime": "10:10:46",
          "OrderDesc": "测试商品",
          "CurrencyCode": "156",
          "CommodityType": "0201",
          "InstallmentMark": "0",
          "ExpiredDate": "30"
        },
        "OrderDetail": [
          {
            "ProductName": "测试商品",
            "UnitPrice": "100.00",
            "Qty": "1",
            "ProductRemarks": "测试商品"
          }
        ],
        "PaymentType": "A",
        "PaymentLinkType": "1",
        "NotifyType": "1",
        "ResultNotifyURL": "https://payment.qsgl.net/api/payment/notify",
        "IsBreakAccount": "0"
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "XdkUR6BFk5c..."  // ✅ 签名已生成
  }
}
```

## 🔍 问题分析

### 可能原因

1. **商户一码多扫功能未开通** ⭐
   - 返回 `MerchantID=""` 表明商户信息未正确识别
   - 可能需要单独申请一码多扫功能

2. **PaymentType=A 参数不支持**
   - A = 支付方式合并（多种支付方式）
   - 可能需要特殊权限或配置

3. **测试环境与生产环境混用**
   - 当前使用生产环境 (pay.abchina.com)
   - 建议先在测试环境验证

4. **商户权限配置**
   - TrxType=OLScanPayOrderReq 可能需要额外开通
   - 证书权限可能不包含此交易类型

## 📞 联系农行技术支持清单

### 需确认的问题

1. **商户功能开通状态**
   ```
   商户号: 103881636900016
   - 是否已开通一码多扫功能？
   - TrxType=OLScanPayOrderReq 是否支持？
   - PaymentType=A（支付方式合并）是否需要单独申请？
   ```

2. **测试环境信息**
   ```
   - 测试环境服务器地址？
   - 测试环境商户号？
   - 测试环境证书？
   ```

3. **EUNKWN错误排查**
   ```
   - "No message available" 的具体含义？
   - 为什么返回的 MerchantID 为空？
   - 服务器端日志是否有更详细的错误信息？
   ```

## 🧪 建议的测试步骤

### 步骤1: 验证其他交易类型
测试已开通的其他交易类型（如微信支付、订单查询）验证环境配置是否正常。

### 步骤2: 切换到测试环境
```bash
# 修改 appsettings.json
{
  "AbcPayment": {
    "IsTestEnvironment": true,
    "ServerName": "测试环境地址",
    "ServerPort": "443",
    ...
  }
}
```

### 步骤3: 简化测试参数
尝试移除可选参数，只保留必填字段：
```json
{
  "orderNo": "TEST001",
  "amount": 0.01,
  "merchantId": "103881636900016",
  "goodsName": "测试",
  "notifyUrl": "https://payment.qsgl.net/api/payment/notify"
}
```

### 步骤4: 对比示例代码
与农行提供的Demo (OLScanPayOrderReq.aspx) 进行逐字段对比：
- dicOrder 字段是否完整
- dic (OrderDetail) 是否正确
- dicRequest 字段是否遗漏

## 📊 技术细节记录

### 签名验证
- ✅ 签名算法: SHA1withRSA
- ✅ 编码格式: GB18030
- ✅ 证书加载: 成功
- ✅ 签名生成: 正常（签名值已在请求中）

### 报文格式
- ✅ V3.0.0 JSON格式
- ✅ MSG包装结构
- ✅ Message嵌套结构
- ✅ Order对象（订单基本信息）
- ✅ OrderDetail数组（商品明细）

### 网络通信
- ✅ HTTPS连接: 正常
- ✅ 服务器响应: HTTP 200
- ✅ 响应格式: JSON
- ✅ 响应签名: 农行已签名

## 🎯 下一步行动计划

### 高优先级
1. 📞 联系农行技术支持，确认一码多扫功能开通状态
2. 🔍 获取测试环境配置信息
3. ✅ 验证商户号权限范围

### 中优先级
4. 🧪 切换到测试环境进行调试
5. 📝 对比农行Demo代码，查找参数差异
6. 🔄 测试其他支付方式（如PaymentType=1借记卡）

### 低优先级
7. 📚 更新接口文档
8. 🧹 代码优化和清理
9. 📊 添加监控和告警

## 💡 备用方案

如果一码多扫功能短期无法开通，可以考虑：
1. 使用单独的微信支付接口
2. 使用单独的支付宝接口
3. 前端引导用户选择支付方式
