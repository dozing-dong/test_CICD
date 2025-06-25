using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Payment;
using FarmGear_Application.Services;
using FarmGear_Application.Services.PaymentGateways;
using FarmGear_Application.Data;
using FarmGear_Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Controllers;

/// <summary>
/// 支付控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
  private readonly IPaymentService _paymentService;
  private readonly AlipayService _alipay;
  private readonly ILogger<PaymentController> _logger;
  private readonly ApplicationDbContext _context;
  private readonly IOrderService _orderService;

  public PaymentController(
      IPaymentService paymentService,
      AlipayService alipay,
      ILogger<PaymentController> logger,
      ApplicationDbContext context,
      IOrderService orderService)
  {
    _paymentService = paymentService;
    _alipay = alipay;
    _logger = logger;
    _context = context;
    _orderService = orderService;
  }

  /// <summary>
  /// 创建支付意图
  /// </summary>
  /// <param name="request">创建支付意图请求</param>
  /// <returns>支付状态响应</returns>
  /// <response code="200">创建成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  /// <response code="404">订单不存在</response>
  /// <response code="409">订单已有支付记录</response>
  [HttpPost("intent")]
  [ProducesResponseType(typeof(ApiResponse<PaymentStatusResponse>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
  public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
  {
    var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
    {
      return BadRequest(new ApiResponse
      {
        Success = false,
        Message = "Failed to get user information"
      });
    }

    var result = await _paymentService.CreatePaymentIntentAsync(request, userId);

    return result.Success switch
    {
      true => Ok(result),
      false => result.Message switch
      {
        "User does not exist" => NotFound(result),
        "Order does not exist" => NotFound(result),
        "No permission to pay for this order" => Forbid(),
        "Order is not in accepted status" => BadRequest(result),
        "Payment already exists for this order" => Conflict(result),
        _ => BadRequest(result)
      }
    };
  }

  /// <summary>
  /// 获取支付状态
  /// </summary>
  /// <param name="orderId">订单ID</param>
  /// <returns>支付状态响应</returns>
  /// <response code="200">获取成功</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  /// <response code="404">订单或支付记录不存在</response>
  [HttpGet("status/{orderId}")]
  [ProducesResponseType(typeof(ApiResponse<PaymentStatusResponse>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetPaymentStatus(string orderId)
  {
    var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
    {
      return BadRequest(new ApiResponse
      {
        Success = false,
        Message = "Failed to get user information"
      });
    }

    var result = await _paymentService.GetPaymentStatusAsync(orderId, userId);

    return result.Success switch
    {
      true => Ok(result),
      false => result.Message switch
      {
        "Order does not exist" => NotFound(result),
        "No permission to view payment status for this order" => Forbid(),
        "No payment record found for this order" => NotFound(result),
        _ => BadRequest(result)
      }
    };
  }

  /// <summary>
  /// 模拟支付完成（仅用于测试）
  /// </summary>
  /// <param name="orderId">订单ID</param>
  /// <returns>支付状态响应</returns>
  /// <response code="200">支付成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  /// <response code="404">订单或支付记录不存在</response>
  /// <response code="409">支付状态不正确</response>
  [HttpPost("complete/{orderId}")]
  [ProducesResponseType(typeof(ApiResponse<PaymentStatusResponse>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
  public async Task<IActionResult> CompletePayment(string orderId)
  {
    var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
    {
      return BadRequest(new ApiResponse
      {
        Success = false,
        Message = "Failed to get user information"
      });
    }

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      var result = await _paymentService.CompletePaymentAsync(orderId, userId);

      if (!result.Success)
      {
        await transaction.RollbackAsync();
        return result.Message switch
        {
          "Order does not exist" => NotFound(result),
          "No permission to complete payment for this order" => Forbid(),
          "No payment record found for this order" => NotFound(result),
          "Payment is not in pending status" => Conflict(result),
          _ => BadRequest(result)
        };
      }

      // 更新订单状态为已完成
      var orderResult = await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Completed, userId, false);
      if (!orderResult.Success)
      {
        await transaction.RollbackAsync();
        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = "Failed to update order status after payment completion"
        });
      }

      await transaction.CommitAsync();
      return Ok(result);
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "Error completing payment for order {OrderId}", orderId);
      return BadRequest(new ApiResponse
      {
        Success = false,
        Message = "An error occurred while completing the payment"
      });
    }
  }

  /// <summary>
  /// 支付宝支付回调
  /// </summary>
  /// <returns>处理结果</returns>
  /// <remarks>
  /// TODO: 此实现为示例代码，需要替换为支付宝官方SDK实现
  /// 1. 安装 NuGet 包: AlipaySDKNet.Standard
  /// 2. 使用 AlipaySDKNet.Standard 中的 AlipayClient 和 AlipayTradePagePayRequest 等类
  /// 3. 参考 AlipayService.cs 中的官方实现示例
  /// 
  /// 当前实现仅用于演示回调处理流程，包括：
  /// - 验证签名
  /// - 检查交易状态
  /// - 更新支付记录
  /// - 返回处理结果
  /// 
  /// 实际生产环境中，需要：
  /// 1. 使用官方SDK验证签名
  /// 2. 验证订单金额
  /// 3. 处理重复通知
  /// 4. 实现幂等性处理
  /// 5. 添加更详细的日志记录
  /// 6. 实现异常重试机制
  /// </remarks>
  [HttpPost("callback")]
  [AllowAnonymous]
  public async Task<IActionResult> AlipayCallback()
  {
    try
    {
      // TODO: 使用支付宝官方SDK验证签名
      // 当前为示例实现，实际需要使用 AlipaySignature.RSASignCheckContent 方法
      var form = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());

      // 验证签名
      if (!_alipay.VerifySignature(form))
      {
        _logger.LogWarning("Invalid Alipay callback signature");
        return Content("fail");
      }

      // TODO: 使用支付宝官方SDK验证订单金额
      // 当前仅获取参数，实际需要验证订单金额是否匹配
      if (!form.TryGetValue("out_trade_no", out var paymentId) ||
          !form.TryGetValue("trade_status", out var tradeStatus) ||
          !form.TryGetValue("total_amount", out var totalAmount))
      {
        _logger.LogWarning("Missing required parameters in Alipay callback");
        return Content("fail");
      }

      // TODO: 使用支付宝官方SDK验证交易状态
      // 当前仅简单判断状态字符串，实际需要使用官方SDK的状态枚举
      if (tradeStatus != "TRADE_SUCCESS")
      {
        _logger.LogInformation("Payment not successful, trade status: {TradeStatus}", tradeStatus);
        return Content("success"); // 非成功状态也返回 success，避免支付宝重复通知
      }

      // TODO: 添加订单金额验证
      // TODO: 添加重复通知处理
      // TODO: 实现更完善的幂等性处理
      var result = await _paymentService.MarkPaymentAsSucceededAsync(paymentId);
      if (!result.Success)
      {
        _logger.LogError("Failed to mark payment as succeeded: {Message}", result.Message);
        return Content("fail");
      }

      return Content("success");
    }
    catch (Exception ex)
    {
      // TODO: 实现异常重试机制
      // TODO: 添加更详细的错误日志
      _logger.LogError(ex, "Error processing Alipay callback");
      return Content("fail");
    }
  }
}
