using FluentValidation;
using FarmGear_Application.DTOs.Equipment;

namespace FarmGear_Application.Validators.Equipment;

/// <summary>
/// 更新设备请求验证器
/// </summary>
public class UpdateEquipmentRequestValidator : AbstractValidator<UpdateEquipmentRequest>
{
    public UpdateEquipmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name length must be between 2 and 100 characters")
            .Matches("^[\u4e00-\u9fa5a-zA-Z0-9\\s-_]+$")
            .WithMessage("Name can only contain Chinese, English, numbers, spaces, underscores, and hyphens");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.DailyPrice)
            .GreaterThan(0).WithMessage("Daily price must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Daily price cannot exceed 10000");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid equipment status");
    }
}