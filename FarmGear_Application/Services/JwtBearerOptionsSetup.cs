using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace FarmGear_Application.Services;

/// <summary>
/// JWT Bearer选项配置 - 兼容ASP.NET Core 8
/// 使用UseSecurityTokenValidators = true来继续使用SecurityTokenValidators API
/// </summary>
public class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
{
  private readonly JwtBlacklistValidator _blacklistValidator;

  public JwtBearerOptionsSetup(JwtBlacklistValidator blacklistValidator)
  {
    _blacklistValidator = blacklistValidator;
  }

  public void Configure(string? name, JwtBearerOptions options)
  {
    if (name == JwtBearerDefaults.AuthenticationScheme)
    {
      // 在ASP.NET Core 8中，设置UseSecurityTokenValidators = true来继续使用旧的API
      // 这样可以避免编译错误，同时保持功能正常
      options.UseSecurityTokenValidators = true;
      options.SecurityTokenValidators.Clear();
      options.SecurityTokenValidators.Add(_blacklistValidator);
    }
  }

  public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}