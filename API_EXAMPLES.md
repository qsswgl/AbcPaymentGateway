# API 使用示例

本文档提供移动端（Android/iOS）调用农行支付网关 API 的示例代码。

## 基础信息

- **API 基础 URL**: `https://payment.qsgl.net/api/payment`
- **内容类型**: `application/json`
- **字符编码**: `UTF-8`

## 接口列表

### 1. 扫码支付

创建微信/支付宝扫码支付订单。

**接口**: `POST /api/payment/qrcode`

**请求参数**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| orderNo | string | 是 | 商户订单号（唯一） |
| orderAmount | string | 是 | 订单金额（单位：分） |
| orderDesc | string | 否 | 订单描述 |
| payQRCode | string | 是 | 用户支付二维码内容 |
| resultNotifyURL | string | 否 | 支付结果回调地址 |
| orderValidTime | string | 否 | 订单有效时间（yyyyMMddHHmmss） |

**请求示例**:

```json
{
  "orderNo": "ORDER202601060001",
  "orderAmount": "1000",
  "orderDesc": "购买商品",
  "payQRCode": "134567890123456789",
  "resultNotifyURL": "https://your-app.com/callback"
}
```

**响应示例**:

```json
{
  "responseCode": "0000",
  "responseMessage": "交易成功",
  "orderNo": "ORDER202601060001",
  "trxId": "2026010612345678",
  "payStatus": "SUCCESS",
  "isSuccess": true
}
```

### 2. 电子钱包支付

使用电子钱包进行支付。

**接口**: `POST /api/payment/ewallet`

**请求参数**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| orderNo | string | 是 | 商户订单号（唯一） |
| orderAmount | string | 是 | 订单金额（单位：分） |
| token | string | 是 | 用户授权Token |
| productName | string | 否 | 产品名称 |
| orderDesc | string | 否 | 订单描述 |
| resultNotifyURL | string | 否 | 支付结果回调地址 |

**请求示例**:

```json
{
  "orderNo": "ORDER202601060002",
  "orderAmount": "2000",
  "token": "USER_TOKEN_123456",
  "productName": "账户充值",
  "orderDesc": "充值200元",
  "resultNotifyURL": "https://your-app.com/callback"
}
```

### 3. 查询订单

查询订单支付状态。

**接口**: `GET /api/payment/query/{orderNo}`

**URL 参数**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| orderNo | string | 是 | 商户订单号 |

**请求示例**:

```
GET https://payment.qsgl.net/api/payment/query/ORDER202601060001
```

**响应示例**:

```json
{
  "responseCode": "0000",
  "responseMessage": "查询成功",
  "orderNo": "ORDER202601060001",
  "trxId": "2026010612345678",
  "payStatus": "SUCCESS",
  "isSuccess": true
}
```

### 4. 健康检查

检查服务是否正常运行。

**接口**: `GET /api/payment/health`

**响应示例**:

```json
{
  "status": "healthy",
  "timestamp": "2026-01-06T08:00:00Z",
  "service": "ABC Payment Gateway"
}
```

## 响应码说明

| 响应码 | 说明 |
|--------|------|
| 0000 | 成功 |
| 00 | 成功 |
| 9999 | 系统错误 |
| 9998 | 网络错误 |
| 9997 | 响应解析失败 |

## Android 示例代码

### 使用 Retrofit + Kotlin

```kotlin
// 1. 添加依赖 (build.gradle)
dependencies {
    implementation 'com.squareup.retrofit2:retrofit:2.9.0'
    implementation 'com.squareup.retrofit2:converter-gson:2.9.0'
    implementation 'com.squareup.okhttp3:logging-interceptor:4.11.0'
}

// 2. 定义数据模型
data class PaymentRequest(
    val orderNo: String,
    val orderAmount: String,
    val orderDesc: String? = null,
    val payQRCode: String? = null,
    val resultNotifyURL: String? = null
)

data class PaymentResponse(
    val responseCode: String,
    val responseMessage: String,
    val orderNo: String?,
    val trxId: String?,
    val payStatus: String?,
    val isSuccess: Boolean
)

// 3. 定义 API 接口
interface PaymentApi {
    @POST("payment/qrcode")
    suspend fun createQRCodePayment(
        @Body request: PaymentRequest
    ): PaymentResponse
    
    @POST("payment/ewallet")
    suspend fun createEWalletPayment(
        @Body request: PaymentRequest
    ): PaymentResponse
    
    @GET("payment/query/{orderNo}")
    suspend fun queryOrder(
        @Path("orderNo") orderNo: String
    ): PaymentResponse
    
    @GET("payment/health")
    suspend fun healthCheck(): Map<String, Any>
}

// 4. 创建 Retrofit 客户端
object PaymentClient {
    private const val BASE_URL = "https://payment.qsgl.net/api/"
    
    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    }
    
    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(loggingInterceptor)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .build()
    
    val api: PaymentApi by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(PaymentApi::class.java)
    }
}

// 5. 使用示例
class PaymentViewModel : ViewModel() {
    
    fun createPayment(orderNo: String, amount: String, qrCode: String) {
        viewModelScope.launch {
            try {
                val request = PaymentRequest(
                    orderNo = orderNo,
                    orderAmount = amount,
                    payQRCode = qrCode,
                    orderDesc = "商品购买",
                    resultNotifyURL = "https://your-app.com/callback"
                )
                
                val response = PaymentClient.api.createQRCodePayment(request)
                
                if (response.isSuccess) {
                    // 支付成功
                    Log.d("Payment", "支付成功: ${response.trxId}")
                } else {
                    // 支付失败
                    Log.e("Payment", "支付失败: ${response.responseMessage}")
                }
            } catch (e: Exception) {
                Log.e("Payment", "请求失败", e)
            }
        }
    }
    
    fun queryOrder(orderNo: String) {
        viewModelScope.launch {
            try {
                val response = PaymentClient.api.queryOrder(orderNo)
                Log.d("Payment", "订单状态: ${response.payStatus}")
            } catch (e: Exception) {
                Log.e("Payment", "查询失败", e)
            }
        }
    }
}
```

## iOS 示例代码

### 使用 URLSession + Swift

```swift
// 1. 定义数据模型
struct PaymentRequest: Codable {
    let orderNo: String
    let orderAmount: String
    let orderDesc: String?
    let payQRCode: String?
    let resultNotifyURL: String?
}

struct PaymentResponse: Codable {
    let responseCode: String
    let responseMessage: String
    let orderNo: String?
    let trxId: String?
    let payStatus: String?
    let isSuccess: Bool
}

// 2. 创建支付服务
class PaymentService {
    static let shared = PaymentService()
    private let baseURL = "https://payment.qsgl.net/api"
    
    private init() {}
    
    // 扫码支付
    func createQRCodePayment(
        orderNo: String,
        amount: String,
        qrCode: String,
        completion: @escaping (Result<PaymentResponse, Error>) -> Void
    ) {
        let url = URL(string: "\(baseURL)/payment/qrcode")!
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        
        let paymentRequest = PaymentRequest(
            orderNo: orderNo,
            orderAmount: amount,
            orderDesc: "商品购买",
            payQRCode: qrCode,
            resultNotifyURL: "https://your-app.com/callback"
        )
        
        do {
            request.httpBody = try JSONEncoder().encode(paymentRequest)
        } catch {
            completion(.failure(error))
            return
        }
        
        URLSession.shared.dataTask(with: request) { data, response, error in
            if let error = error {
                completion(.failure(error))
                return
            }
            
            guard let data = data else {
                completion(.failure(NSError(domain: "NoData", code: -1)))
                return
            }
            
            do {
                let paymentResponse = try JSONDecoder().decode(
                    PaymentResponse.self,
                    from: data
                )
                completion(.success(paymentResponse))
            } catch {
                completion(.failure(error))
            }
        }.resume()
    }
    
    // 查询订单
    func queryOrder(
        orderNo: String,
        completion: @escaping (Result<PaymentResponse, Error>) -> Void
    ) {
        let url = URL(string: "\(baseURL)/payment/query/\(orderNo)")!
        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        
        URLSession.shared.dataTask(with: request) { data, response, error in
            if let error = error {
                completion(.failure(error))
                return
            }
            
            guard let data = data else {
                completion(.failure(NSError(domain: "NoData", code: -1)))
                return
            }
            
            do {
                let paymentResponse = try JSONDecoder().decode(
                    PaymentResponse.self,
                    from: data
                )
                completion(.success(paymentResponse))
            } catch {
                completion(.failure(error))
            }
        }.resume()
    }
}

// 3. 使用示例
class PaymentViewController: UIViewController {
    
    func processPayment() {
        let orderNo = "ORDER\(Date().timeIntervalSince1970)"
        let amount = "1000" // 10元（单位：分）
        let qrCode = "134567890123456789"
        
        PaymentService.shared.createQRCodePayment(
            orderNo: orderNo,
            amount: amount,
            qrCode: qrCode
        ) { result in
            DispatchQueue.main.async {
                switch result {
                case .success(let response):
                    if response.isSuccess {
                        print("支付成功: \(response.trxId ?? "")")
                        self.showAlert("支付成功")
                    } else {
                        print("支付失败: \(response.responseMessage)")
                        self.showAlert("支付失败: \(response.responseMessage)")
                    }
                case .failure(let error):
                    print("请求失败: \(error)")
                    self.showAlert("网络错误")
                }
            }
        }
    }
    
    func showAlert(_ message: String) {
        let alert = UIAlertController(
            title: "提示",
            message: message,
            preferredStyle: .alert
        )
        alert.addAction(UIAlertAction(title: "确定", style: .default))
        present(alert, animated: true)
    }
}
```

## React Native 示例代码

```javascript
// PaymentService.js
const BASE_URL = 'https://payment.qsgl.net/api';

export const PaymentService = {
  // 创建扫码支付
  async createQRCodePayment(orderNo, amount, qrCode) {
    try {
      const response = await fetch(`${BASE_URL}/payment/qrcode`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          orderNo,
          orderAmount: amount,
          payQRCode: qrCode,
          orderDesc: '商品购买',
          resultNotifyURL: 'https://your-app.com/callback',
        }),
      });
      
      const data = await response.json();
      return data;
    } catch (error) {
      console.error('支付请求失败:', error);
      throw error;
    }
  },
  
  // 查询订单
  async queryOrder(orderNo) {
    try {
      const response = await fetch(
        `${BASE_URL}/payment/query/${orderNo}`,
        {
          method: 'GET',
        }
      );
      
      const data = await response.json();
      return data;
    } catch (error) {
      console.error('查询失败:', error);
      throw error;
    }
  },
};

// 使用示例
import React, { useState } from 'react';
import { View, Button, Alert } from 'react-native';
import { PaymentService } from './PaymentService';

const PaymentScreen = () => {
  const [loading, setLoading] = useState(false);
  
  const handlePayment = async () => {
    setLoading(true);
    try {
      const orderNo = `ORDER${Date.now()}`;
      const amount = '1000'; // 10元
      const qrCode = '134567890123456789';
      
      const result = await PaymentService.createQRCodePayment(
        orderNo,
        amount,
        qrCode
      );
      
      if (result.isSuccess) {
        Alert.alert('成功', '支付成功');
      } else {
        Alert.alert('失败', result.responseMessage);
      }
    } catch (error) {
      Alert.alert('错误', '网络请求失败');
    } finally {
      setLoading(false);
    }
  };
  
  return (
    <View>
      <Button
        title={loading ? '处理中...' : '立即支付'}
        onPress={handlePayment}
        disabled={loading}
      />
    </View>
  );
};

export default PaymentScreen;
```

## 注意事项

1. **订单号唯一性**: 确保每次支付的 `orderNo` 是唯一的
2. **金额单位**: 金额单位为"分"，100 = 1元
3. **超时处理**: 建议设置 30 秒的请求超时
4. **错误处理**: 务必处理网络错误和业务错误
5. **回调验证**: 支付回调需要验证签名（待实现）
6. **日志记录**: 记录关键操作日志便于排查问题

## 测试建议

1. 先在测试环境测试
2. 使用小额订单测试
3. 测试网络异常情况
4. 测试超时情况
5. 验证回调处理

## 技术支持

如有问题，请提供：
- 订单号
- 请求时间
- 错误信息
- 日志截图
