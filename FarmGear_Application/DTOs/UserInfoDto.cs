using System.ComponentModel.DataAnnotations;

namespace FarmGear_Application.DTOs;

/// <summary>
/// 用户信息 DTO，用于返回当前登录用户的基本信息
/// </summary>
public class UserInfoDto
{
  /// <summary>
  /// 用户ID
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// 用户名
  /// </summary>
  public string Username { get; set; } = string.Empty;

  /// <summary>
  /// 电子邮箱
  /// </summary>
  [EmailAddress]
  public string Email { get; set; } = string.Empty;

  /// <summary>
  /// 用户角色
  /// </summary>
  public string Role { get; set; } = string.Empty;

  /// <summary>
  /// 邮箱是否已验证
  /// </summary>
  public bool EmailConfirmed { get; set; }
}