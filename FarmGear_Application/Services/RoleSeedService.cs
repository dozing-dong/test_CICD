using FarmGear_Application.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FarmGear_Application.Services;

/// <summary>
/// 角色种子服务
/// </summary>
public class RoleSeedService
{
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly ILogger<RoleSeedService> _logger;

  public RoleSeedService(RoleManager<IdentityRole> roleManager, ILogger<RoleSeedService> logger)
  {
    _roleManager = roleManager;
    _logger = logger;
  }

  /// <summary>
  /// 初始化默认角色
  /// </summary>
  public async Task SeedRolesAsync()
  {
    foreach (var role in UserRoles.AllRoles)
    {
      if (!await _roleManager.RoleExistsAsync(role))
      {
        var result = await _roleManager.CreateAsync(new IdentityRole(role));
        if (result.Succeeded)
        {
          _logger.LogInformation("Role {Role} created successfully", role);
        }
        else
        {
          _logger.LogError("Failed to create role {Role}: {Errors}",
              role, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
      }
      else
      {
        _logger.LogInformation("Role {Role} already exists", role);
      }
    }
  }
}