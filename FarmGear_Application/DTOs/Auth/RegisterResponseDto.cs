namespace FarmGear_Application.DTOs.Auth;

/// <summary>
/// 注册响应 DTO
/// </summary>
public class RegisterResponseDto : ApiResponse
{
  /// <summary>
  /// 用户ID（注册成功时返回）
  /// </summary>
  public string? UserId { get; set; }
}