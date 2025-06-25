namespace FarmGear_Application.DTOs.Auth;

/// <summary>
/// 登录响应 DTO
/// </summary>
public class LoginResponseDto : ApiResponse
{
  /// <summary>
  /// JWT Token（登录成功时返回）
  /// </summary>
  public string? Token { get; set; }

  /// <summary>
  /// 用户信息（登录成功时返回）
  /// </summary>
  public UserInfoDto? UserInfo { get; set; }
}