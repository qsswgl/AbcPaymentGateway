# 农行支付配置检查清单

## 问题现状
- 错误码：**2302**
- 错误信息：**商户服务器证书配置有误，请登录商户服务系统检查商户证书**
- 农行反馈：证书已在商户服务系统配置，需要检查项目配置参数

## Demo配置对照（TrustMerchant-Demo.properties）

### 1. 系统配置段
```properties
TrustPayConnectMethod = https          ✅ 已配置
TrustPayServerName = pay.abchina.com   ✅ 已配置  
TrustPayServerPort = 443               ✅ 已配置
TrustPayNewLine = 2                    ❓ 未配置（接口特性）
TrustPayTrxURL = /ebus/ReceiveMerchantTrxReqServlet  ✅ 已配置
TrustPayFileTrxURL = /ebussettle/ReceiveMerchantFileTrxReqServlet  ❌ 未配置
TrustPayIETrxURL = https://pay.abchina.com/ebus/ReceiveMerchantIERequestServlet  ✅ 已配置
MerchantErrorURL = https://pay.abchina.com/ebus/MerchantIEFailure.aspx  ❌ 使用的是本地地址
```

### 2. 证书配置段
```properties
TrustPayCertFile = .../TrustPay.cer    ✅ 已配置
MerchantID = 103881636900016           ✅ 已配置
MerchantCertFile = .../xxx.pfx         ✅ 已配置
MerchantCertPassword = xxxxxxxx        ✅ 已配置
TrustPayServerTimeout =                ❓ 已配置（30000ms）
PrintLog = true                        ✅ 已配置
```

## 可能的问题点

### 1. TrustPayNewLine = 2 参数缺失
- **说明**：这是"线上支付平台接口特性"参数
- **影响**：可能影响消息格式的换行符处理
- **当前状态**：未配置

### 2. MerchantErrorURL 使用本地地址
- **Demo配置**：`https://pay.abchina.com/ebus/MerchantIEFailure.aspx`
- **当前配置**：`http://localhost:5000/error`
- **影响**：可能导致农行验证失败

### 3. 证书序列号/主题验证
- **需要检查**：证书的CN是否与商户号完全匹配
- **当前证书信息**：需要验证

### 4. 可能的额外参数
根据新版文档可能需要的参数：
- 商户类型 (ECMerchantType)
- 证书序列号
- 证书指纹
- 特殊的请求头

## 下一步行动

### 立即检查
1. ✅ 添加 TrustPayNewLine 配置
2. ✅ 修改 MerchantErrorURL 为公网地址（或农行默认地址）
3. ✅ 验证证书信息（序列号、主题、有效期）
4. ✅ 检查是否需要在HTTP请求头添加特殊参数

### 如果仍然失败
1. 咨询农行获取正确的证书序列号格式要求
2. 确认证书是否需要在商户服务系统重新上传
3. 检查证书DN格式是否符合要求
