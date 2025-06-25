namespace FarmGear_Application.DTOs.Auth;

/// <summary>
/// 用户会话DTO
/// </summary>
public class UserSessionDto
{
  /// <summary>
  /// 用户ID
  /// </summary>
  public string UserId { get; set; } = string.Empty;

  /// <summary>
  /// 用户名
  /// </summary>
  public string Username { get; set; } = string.Empty;

  /// <summary>
  /// 邮箱
  /// </summary>
  public string Email { get; set; } = string.Empty;

  /// <summary>
  /// 用户角色
  /// </summary>
  public string Role { get; set; } = string.Empty;

  /// <summary>
  /// 登录时间
  /// </summary>
  public DateTime LoginTime { get; set; }

  /// <summary>
  /// 最后活动时间
  /// </summary>
  public DateTime LastActivityTime { get; set; }

  /// <summary>
  /// IP地址
  /// </summary>
  public string? IpAddress { get; set; }

  /// <summary>
  /// 用户代理
  /// </summary>
  public string? UserAgent { get; set; }

  /// <summary>
  /// 是否活跃
  /// </summary>
  public bool IsActive { get; set; } = true;
}