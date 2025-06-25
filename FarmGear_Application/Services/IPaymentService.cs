using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Payment;

namespace FarmGear_Application.Services;

/// <summary>
/// 支付服务接口
/// </summary>
public interface IPaymentService
{
  /// <summary>
  /// 创建支付意图
  /// </summary>
  /// <param name="request">创建支付意图请求</param>
  /// <param name="userId">用户ID</param>
  /// <returns>支付状态响应</returns>
  Task<ApiResponse<PaymentStatusResponse>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, string userId);

  /// <summary>
  /// 获取支付状态
  /// </summary>
  /// <param name="orderId">订单ID</param>
  /// <param name="userId">用户ID</param>
  /// <returns>支付状态响应</returns>
  Task<ApiResponse<PaymentStatusResponse>> GetPaymentStatusAsync(string orderId, string userId);

  /// <summary>
  /// 模拟支付完成
  /// </summary>
  /// <param name="orderId">订单ID</param>
  /// <param name="userId">用户ID</param>
  /// <returns>支付状态响应</returns>
  Task<ApiResponse<PaymentStatusResponse>> CompletePaymentAsync(string orderId, string userId);

  /// <summary>
  /// 标记支付为成功
  /// </summary>
  /// <param name="paymentId">支付记录ID</param>
  /// <returns>支付状态响应</returns>
  Task<ApiResponse<PaymentStatusResponse>> MarkPaymentAsSucceededAsync(string paymentId);
}