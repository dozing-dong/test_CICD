using FarmGear_Application.DTOs.Orders;
using FluentValidation;

namespace FarmGear_Application.Validators.Orders;

/// <summary>
/// 创建订单请求验证器
/// </summary>
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
  public CreateOrderRequestValidator()
  {
    RuleFor(x => x.EquipmentId)
        .NotEmpty().WithMessage("Equipment ID is required");

    RuleFor(x => x.StartDate)
        .NotEmpty().WithMessage("Start date is required")
        .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Start date must be today or later");

    RuleFor(x => x.EndDate)
        .NotEmpty().WithMessage("End date is required")
        .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date")
        .LessThanOrEqualTo(x => x.StartDate.AddMonths(3)).WithMessage("Rental period cannot exceed 3 months");
  }
}