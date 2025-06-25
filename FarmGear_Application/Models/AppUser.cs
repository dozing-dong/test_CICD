using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmGear_Application.Models;

/// <summary>
/// 应用程序用户类，继承自IdentityUser
/// </summary>
public class AppUser : IdentityUser
{
  /// <summary>
  /// 用户全名
  /// </summary>
  public string FullName { get; set; } = string.Empty;

  /// <summary>
  /// 创建时间
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// 最后登录时间
  /// </summary>
  public DateTime? LastLoginAt { get; set; }

  /// <summary>
  /// 是否激活
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// 纬度
  /// </summary>
  [Column(TypeName = "decimal(10,6)")]
  public decimal? Lat { get; set; }

  /// <summary>
  /// 经度
  /// </summary>
  [Column(TypeName = "decimal(10,6)")]
  public decimal? Lng { get; set; }
}