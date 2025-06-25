namespace FarmGear_Application.DTOs.Payment;

/// <summary>
/// 创建支付意图请求
/// </summary>
public class CreatePaymentIntentRequest
{
  /// <summary>
  /// 订单ID
  /// </summary>
  public string OrderId { get; set; } = string.Empty;
}