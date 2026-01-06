using System.Text.Json.Serialization;
using AbcPaymentGateway.Models;

namespace AbcPaymentGateway;

/// <summary>
/// JSON 序列化上下文（支持 Native AOT）
/// </summary>
[JsonSerializable(typeof(PaymentRequest))]
[JsonSerializable(typeof(PaymentResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(object))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
