# ABC银行官方Demo分析报告

**分析时间**: 2026年1月19日 10:30  
**Demo位置**: K:\payment\综合收银台接口包新版  
**关键发现**: PayReq的完整参数列表

---

## 🔍 关键发现

### 1. PayReq 交易类型确认

在官方demo的 `MerchantPayment.jsp` 中明确使用：
```java
eBusMerchantCommonRequest.dicRequest.put("TrxType", "PayReq");
```

✅ **确认**: 页面支付的TrxType是 `PayReq`（不是OrderReq）

---

## 2. 发现缺失的字段

### ⚠️ 重要发现：ReceiveAccount 和 ReceiveAccName

在官方demo中，**PayReq请求包含以下我们未使用的字段**：

```java
// MerchantPayment.jsp 第8-10行
eBusMerchantCommonRequest.dicRequest.put("ReceiveAccount", request.getParameter("ReceiveAccount"));      //设定收款方账号
eBusMerchantCommonRequest.dicRequest.put("ReceiveAccName", request.getParameter("ReceiveAccName"));      //设定收款方户名
```

在HTML表单中（MerchantPayment.html 第114-121行）：
```html
<div class="field">
    <label>指定商户收款账户账号</label>
    <input type="text" name="ReceiveAccount" value="">
</div>
<div class="field">
    <label>指定商户收款账户户名</label>
    <input type="text" name="ReceiveAccName" value="">
</div>
```

**注意**: Demo中这两个字段的默认值是**空字符串**（可选字段）

---

## 3. 完整的PayReq参数列表对比

### 官方Demo使用的参数

#### TrxRequest 级别字段：
```java
// 我们已使用 ✅
- PaymentType          // 支付类型 (A=借贷记卡合并) ✅
- PaymentLinkType      // 支付渠道 (1=电脑网络) ✅
- NotifyType           // 通知方式 (0=仅页面, 1=页面+服务器) ✅
- ResultNotifyURL      // 通知URL ✅
- IsBreakAccount       // 是否分账 (0=否, 1=是) ✅

// 我们**未使用**的字段 ⚠️
- ReceiveAccount       // 收款账号 (可选，demo中为空) ❌
- ReceiveAccName       // 收款户名 (可选，demo中为空) ❌
- MerchantRemarks      // 附言 (可选) ❌
- SplitAccTemplate     // 分账模板号 (IsBreakAccount=1时必填) ❌
- VerifyFlag           // 实名验证标识 (可选) ❌
- VerifyType           // 证件类型 (VerifyFlag=1时必填) ❌
- VerifyNo             // 证件号码 (VerifyFlag=1时必填) ❌
```

#### Order 对象字段：
```java
// 我们已使用 ✅
- PayTypeID            // 交易类型 (ImmediatePay) ✅
- OrderDate            // 订单日期 ✅
- OrderTime            // 订单时间 ✅
- OrderNo              // 订单号 ✅
- CurrencyCode         // 货币代码 (156) ✅
- OrderAmount          // 订单金额 ✅
- OrderDesc            // 订单描述 ✅
- CommodityType        // 商品类型 ✅
- BuyIP                // 客户IP ✅
- ExpiredDate          // 订单保存时间(天) ✅
- InstallmentMark      // 分期标识 ✅

// 我们未使用的字段 ⚠️
- orderTimeoutDate     // 订单超时时间 (格式: yyyyMMddHHmmss) ❌
- SubsidyAmount        // 补贴金额 ❌
- Fee                  // 手续费金额 ❌
- AccountNo            // 支付账户 ❌
- OrderURL             // 订单地址 ❌
- ReceiverAddress      // 收货地址 (我们已使用) ✅
- InstallmentCode      // 分期代码 (分期时必填) ❌
- InstallmentNum       // 分期期数 (分期时必填) ❌
- OrderItems           // 订单明细（demo中动态生成） ❌
- SplitAccInfoItems    // 分账信息（平台商户） ❌
```

---

## 4. Demo中的默认值

### 常用默认值：
- **CommodityType**: "0101" (支付账户充值)
  - 我们使用的是 "0201" (虚拟类) ⚠️
  - **建议**: 改为 "0101"

- **BuyIP**: "127.0.0.1"（demo示例）
  - 建议使用真实客户IP

- **PaymentType**: "A"（农行借贷记卡/一码多扫）✅ 我们已使用

- **PaymentLinkType**: "1"（电脑网络接入）✅ 我们已使用

- **NotifyType**: "0"（仅页面跳转）
  - 我们使用的是 "1"（页面+服务器）✅

---

## 5. 关键差异点

### ⚠️ 可能导致EUNKWN的差异：

| 字段 | Demo值 | 我们的值 | 风险等级 |
|------|--------|----------|---------|
| CommodityType | "0101" | "0201" | 🔴 高 |
| ReceiveAccount | "" (空) | **未发送** | 🟡 中 |
| ReceiveAccName | "" (空) | **未发送** | 🟡 中 |
| MerchantRemarks | "" (空) | **未发送** | 🟢 低 |
| orderTimeoutDate | "20171231000000" | **未发送** | 🟢 低 |

**分析**：
1. **CommodityType差异最可疑** - Demo用"0101"（充值），我们用"0201"（虚拟商品）
2. **ReceiveAccount/ReceiveAccName** - Demo中虽然为空但**有发送**，我们**完全未发送**

---

## 6. Demo的工作流程

```
用户填写表单 (MerchantPayment.html)
        ↓
POST提交到 MerchantPayment.jsp
        ↓
使用 EBusMerchantCommonRequest 类
        ↓
调用 postRequest() 发送到农行
        ↓
返回 JSON 响应
```

### 使用的核心类：
- `com.abc.pay.client.ebus.common.EBusMerchantCommonRequest`
- 来自jar包：`TrustPayCBPClient.jar`

---

## 7. 建议的修正措施

### 🔴 高优先级（立即修改）：

1. **修改 CommodityType**
   ```csharp
   // 从
   CommodityType = "0201"  // 虚拟类
   // 改为
   CommodityType = "0101"  // 支付账户充值
   ```

2. **添加 ReceiveAccount 和 ReceiveAccName**
   ```csharp
   // 即使为空也要发送
   ["ReceiveAccount"] = "",
   ["ReceiveAccName"] = ""
   ```

### 🟡 中优先级（建议添加）：

3. **添加 MerchantRemarks**
   ```csharp
   ["MerchantRemarks"] = ""
   ```

4. **添加 orderTimeoutDate**
   ```csharp
   // Order对象中
   ["orderTimeoutDate"] = DateTime.Now.AddDays(1).ToString("yyyyMMddHHmmss")
   ```

---

## 8. 更新后的完整请求示例

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
        "TrxType": "PayReq",
        
        // 添加缺失字段 ⭐
        "ReceiveAccount": "",
        "ReceiveAccName": "",
        "MerchantRemarks": "",
        
        "PaymentType": "A",
        "PaymentLinkType": "1",
        "NotifyType": "1",
        "ResultNotifyURL": "https://payment.qsgl.net/api/payment/abc/notify",
        "IsBreakAccount": "0",
        
        "Order": {
          "PayTypeID": "ImmediatePay",
          "OrderNo": "PAY20260119103001",
          "OrderAmount": "10.00",
          "OrderDate": "2026/01/19",
          "OrderTime": "10:30:00",
          "OrderDesc": "测试商品",
          "CurrencyCode": "156",
          "CommodityType": "0101",  // 修改为0101 ⭐
          "InstallmentMark": "0",
          "ExpiredDate": "30",
          "BuyIP": "真实IP地址",
          
          // 添加可选字段
          "orderTimeoutDate": "20260120103000"  // ⭐
        },
        
        // OrderDetail保持不变
        "OrderDetail": [...]
      }
    },
    "Signature-Algorithm": "SHA1withRSA",
    "Signature": "..."
  }
}
```

---

## 9. Demo文件结构

```
K:\payment\综合收银台接口包新版\
├── Web\
│   ├── index.jsp                    # 主导航页
│   ├── Merchant.html                # 旧版导航
│   └── Order\
│       ├── MerchantPayment.html     # 页面支付表单 ⭐
│       ├── MerchantPayment.jsp      # 页面支付处理 ⭐
│       ├── OLScanPayOrderReq.html   # 一码多扫表单
│       ├── OLScanPayOrderReq.jsp    # 一码多扫处理
│       ├── WeiXinOrderRequest.html  # 微信支付表单
│       ├── WeiXinOrderRequest.jsp   # 微信支付处理
│       └── AlipayRequest.*          # 支付宝支付
├── WEB-INF\
│   └── lib\
│       └── TrustPayCBPClient.jar    # ABC银行SDK ⭐
├── cert\                             # 证书目录
├── css\                              # 样式文件
└── js\                               # JavaScript文件
```

---

## 10. 下一步行动

### 立即执行：

1. ✅ 修改代码添加缺失字段
2. ✅ 修改 CommodityType 为 "0101"
3. ✅ 重新部署测试
4. ✅ 生成新的测试报告

### 如果仍然失败：

5. ⏳ 向ABC银行反馈：
   - "已参照官方demo调整所有参数"
   - "CommodityType已从0201改为0101"
   - "已添加ReceiveAccount/ReceiveAccName字段（虽然为空）"
   - "请确认是否还有其他配置问题"

---

## 11. Demo运行说明

**注意**: 此demo需要Tomcat服务器运行（JSP应用）

**如需运行demo**:
1. 安装Tomcat 8.5+
2. 将整个目录部署到Tomcat的webapps下
3. 配置商户证书到cert目录
4. 访问 http://localhost:8080/[appname]/Web/index.jsp

**替代方案**:
我们已从demo中提取了所有参数配置，可直接更新我们的.NET代码，无需运行JSP demo。

---

**分析完成时间**: 2026年1月19日 10:30  
**分析工具**: GitHub Copilot  
**建议**: 立即更新代码并重新测试
