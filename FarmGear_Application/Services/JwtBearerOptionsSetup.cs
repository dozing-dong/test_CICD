using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace FarmGear_Application.Services;

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
      // 启用 SecurityTokenValidators 以继续使用旧 API
      options.UseSecurityTokenValidators = true;
      options.SecurityTokenValidators.Clear();
      options.SecurityTokenValidators.Add(_blacklistValidator);
    }
  }

  public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}