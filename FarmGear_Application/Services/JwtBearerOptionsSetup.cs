using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace FarmGear_Application.Services;

/// <summary>
/// JWT Bearer选项配置 - 使用ASP.NET Core 8新的TokenHandlers API
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
      // 使用新的TokenHandlers API替代过时的SecurityTokenValidators
      options.TokenHandlers.Clear();
      options.TokenHandlers.Add(_blacklistValidator);
    }
  }

  public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}