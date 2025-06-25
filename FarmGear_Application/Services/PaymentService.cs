using FarmGear_Application.Data;
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Payment;
using FarmGear_Application.Models;
using FarmGear_Application.Services.PaymentGateways;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Services;

/// <summary>
/// 支付服务实现
/// </summary>
public class PaymentService : IPaymentService
{
  private readonly ApplicationDbContext _context;
  private readonly UserManager<AppUser> _userManager;
  private readonly ILogger<PaymentService> _logger;
  private readonly AlipayService _alipay;

  public PaymentService(
      ApplicationDbContext context,
      UserManager<AppUser> userManager,
      ILogger<PaymentService> logger,
      AlipayService alipay)
  {
    _context = context;
    _userManager = userManager;
    _logger = logger;
    _alipay = alipay;
  }

  public async Task<ApiResponse<PaymentStatusResponse>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, string userId)
  {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      // 检查用户是否存在
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null)
      {
        return new ApiResponse<PaymentStatusResponse>
        {
          Success = false,
          Message = "User does not exist"
        };
      }

      // 检查订单是否存在且属于当前用户
      var order = await _context.Orders
          .Include(o => o.Equipment)
          .FirstOrDefaultAsync(o => o.Id == request.OrderId);

      if (order == null)
      {
        return new ApiResponse<PaymentStatusResponse>
        {
          Success = false,
          Message = "Order does not exist"
        };
      }

      if (order.RenterId != userId)
      {
        return new ApiResponse<PaymentStatusResponse>
        {
          Success = false,
          Message = "No permission to pay for this order"
        };
      }

      // 检查订单状态是否为已批准
      if (order.Status != OrderStatus.Accepted)
      {
        return new ApiResponse<PaymentStatusResponse>
        {
          Success = false,
          Message = "Order is not in accepted status"
        };
      }

      // 检查是否已有支付记录
      var existingPayment = await _context.PaymentRecords
          .FirstOrDefaultAsync(p => p.OrderId == request.OrderId);

      if (existingPayment != null)
      {
        return new ApiResponse<PaymentStatusResponse>
        {
          Success = false,
          Message = "Payment already exists for this order"
        };
      }

      // 创建支付记录
      var paymentRecord = new PaymentRecord
      {
        OrderId = request.OrderId,
        UserId = userId,
        Amount = order.TotalAmount,
        Status = PaymentStatus.Pending,
        CreatedAt = DateTime.UtcNow
      };

      _context.PaymentRecords.Add(paymentRecord);
      await _context.SaveChangesAsync();

      // 生成支付宝支付URL
      var paymentUrl = _alipay.GeneratePaymentUrl(
          paymentRecord.Id,
          paymentRecord.Amount,
          $"FarmGear Order {order.Id}");

      await transaction.CommitAsync();

      var response = await MapToPaymentStatusResponseAsync(paymentRecord);
      response.PaymentUrl = paymentUrl;

      return new ApiResponse<PaymentStatusResponse>
      {
        Success = true,
        Message = "Payment intent created successfully",
        Data = response
      };
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "An error occurred while creating payment intent");
      return new ApiResponse<PaymentStatusResponse>
      {
        Success = false,
        Message = "An error occurred while creating payment intent"
      };
    }
  }

  public async Task<ApiResponse<PaymentStatusResponse>> GetPaymentStatusAsync(string orderId, string userId)
  {
    try
    {
      // 检查订单是否存在且属于当前用户
      var order = await _context.Orders
          .FirstOrDefaultAsync(o => o.Id == orderId);

      if (order == null)
      {
        return new ApiResponse<PaymentStatusResponse>
        {
          Success = false,
          Message = "Order does not exist"
        };
      }

      if (order.RenterId != userId)
      {
        return new ApiResponse<PaymentStatusResponse>
        {
          Success = false,
          Message = "No permission to view payment status for this order"
        };
      }

      // 获取支付记录
      var paymentRecord = await _context.PaymentRecords
          .FirstOrDefaultAsync(p => p.OrderId == orderId);

      if (paymentRecord == null)
      {
        return new ApiResponse<PaymentStatusResponse>
        {
          Success = false,
          Message = "No payment record found for this order"
        };
      }

      return new ApiResponse<PaymentStatusResponse>
      {
        Success = true,
        Data = await MapToPaymentStatusResponseAsync(paymentRecord)
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while getting payment status");
      return new ApiResponse<PaymentStatusResponse>
      {
        Success = false,
        Message = "An error occurred while getting payment status"
      };
    }
  }

  public async Task<ApiResponse<PaymentStatusResponse>> CompletePaymentAsync(string orderId, string userId)
  {
    try
    {
      // 开启事务
      using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        // 检查订单是否存在且属于当前用户
        var order = await _context.Orders
            .Include(o => o.Equipment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
          return new ApiResponse<PaymentStatusResponse>
          {
            Success = false,
            Message = "Order does not exist"
          };
        }

        if (order.RenterId != userId)
        {
          return new ApiResponse<PaymentStatusResponse>
          {
            Success = false,
            Message = "No permission to complete payment for this order"
          };
        }

        // 获取支付记录
        var paymentRecord = await _context.PaymentRecords
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (paymentRecord == null)
        {
          return new ApiResponse<PaymentStatusResponse>
          {
            Success = false,
            Message = "No payment record found for this order"
          };
        }

        if (paymentRecord.Status != PaymentStatus.Pending)
        {
          return new ApiResponse<PaymentStatusResponse>
          {
            Success = false,
            Message = "Payment is not in pending status"
          };
        }

        // 更新支付记录
        paymentRecord.Status = PaymentStatus.Paid;
        paymentRecord.PaidAt = DateTime.UtcNow;

        // 更新订单状态
        order.Status = OrderStatus.Completed;

        // 更新设备状态
        if (order.Equipment != null)
        {
          order.Equipment.Status = EquipmentStatus.Rented;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApiResponse<PaymentStatusResponse>
        {
          Success = true,
          Message = "Payment completed successfully",
          Data = await MapToPaymentStatusResponseAsync(paymentRecord)
        };
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        throw;
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while completing payment");
      return new ApiResponse<PaymentStatusResponse>
      {
        Success = false,
        Message = "An error occurred while completing payment"
      };
    }
  }

  public async Task<ApiResponse<PaymentStatusResponse>> MarkPaymentAsSucceededAsync(string paymentId)
  {
    try
    {
      using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        var paymentRecord = await _context.PaymentRecords
            .Include(p => p.Order)
                .ThenInclude(o => o!.Equipment)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (paymentRecord == null)
        {
          return new ApiResponse<PaymentStatusResponse>
          {
            Success = false,
            Message = "Payment record not found"
          };
        }

        // 幂等性检查：如果已经是已支付状态，直接返回成功
        if (paymentRecord.Status == PaymentStatus.Paid)
        {
          return new ApiResponse<PaymentStatusResponse>
          {
            Success = true,
            Message = "Payment already completed",
            Data = await MapToPaymentStatusResponseAsync(paymentRecord)
          };
        }

        // 更新支付记录
        paymentRecord.Status = PaymentStatus.Paid;
        paymentRecord.PaidAt = DateTime.UtcNow;

        // 更新订单状态
        if (paymentRecord.Order?.Equipment != null)
        {
          paymentRecord.Order.Status = OrderStatus.Completed;
          paymentRecord.Order.Equipment.Status = EquipmentStatus.Rented;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApiResponse<PaymentStatusResponse>
        {
          Success = true,
          Message = "Payment marked as succeeded",
          Data = await MapToPaymentStatusResponseAsync(paymentRecord)
        };
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        throw;
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while marking payment as succeeded");
      return new ApiResponse<PaymentStatusResponse>
      {
        Success = false,
        Message = "An error occurred while marking payment as succeeded"
      };
    }
  }

  private async Task<PaymentStatusResponse> MapToPaymentStatusResponseAsync(PaymentRecord paymentRecord)
  {
    var user = await _userManager.FindByIdAsync(paymentRecord.UserId);

    return new PaymentStatusResponse
    {
      Id = paymentRecord.Id,
      OrderId = paymentRecord.OrderId,
      Amount = paymentRecord.Amount,
      Status = paymentRecord.Status,
      PaidAt = paymentRecord.PaidAt,
      CreatedAt = paymentRecord.CreatedAt,
      UserId = paymentRecord.UserId,
      UserName = user?.UserName ?? string.Empty
    };
  }
}