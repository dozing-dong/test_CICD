namespace FarmGear_Application.Constants;

/// <summary>
/// 用户角色常量
/// </summary>
public static class UserRoles
{
  /// <summary>
  /// 农民
  /// </summary>
  public const string Farmer = "Farmer";

  /// <summary>
  /// 设备提供者
  /// </summary>
  public const string Provider = "Provider";

  /// <summary>
  /// 官方人员
  /// </summary>
  public const string Official = "Official";

  /// <summary>
  /// 管理员
  /// </summary>
  public const string Admin = "Admin";

  /// <summary>
  /// 所有角色
  /// </summary>
  public static readonly string[] AllRoles = { Farmer, Provider, Official, Admin };
}