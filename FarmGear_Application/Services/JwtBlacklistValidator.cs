using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FarmGear_Application.Services;

/// <summary>
/// JWT黑名单验证器 - 符合ASP.NET Core标准认证架构
/// </summary>
public class JwtBlacklistValidator : ISecurityTokenValidator
{
  private readonly IRedisCacheService _cacheService;
  private readonly ILogger<JwtBlacklistValidator> _logger;
  private readonly JwtSecurityTokenHandler _tokenHandler;

  public JwtBlacklistValidator(
      IRedisCacheService cacheService,
      ILogger<JwtBlacklistValidator> logger)
  {
    _cacheService = cacheService;
    _logger = logger;
    _tokenHandler = new JwtSecurityTokenHandler();
  }

  public bool CanValidateToken => true;

  public int MaximumTokenSizeInBytes { get; set; } = TokenValidationParameters.DefaultMaximumTokenSizeInBytes;

  public bool CanReadToken(string securityToken)
  {
    return _tokenHandler.CanReadToken(securityToken);
  }

  public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
  {
    try
    {
      // 首先检查Token是否在黑名单中
      if (_cacheService.IsTokenBlacklistedAsync(securityToken).Result)
      {
        _logger.LogWarning("Token is blacklisted: {TokenId}", GetTokenId(securityToken));
        throw new SecurityTokenException("Token has been invalidated");
      }

      // 使用标准的JWT验证
      var principal = _tokenHandler.ValidateToken(securityToken, validationParameters, out validatedToken);

      // 验证成功后，更新用户会话活动时间
      _ = Task.Run(() =>
      {
        try
        {
          var userId = principal.FindFirst("sub")?.Value;
          if (!string.IsNullOrEmpty(userId))
          {
            // 这里可以添加会话更新逻辑
            _logger.LogDebug("Token validated successfully for user {UserId}", userId);
          }
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error updating session activity");
        }
      });

      return principal;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Token validation failed");
      throw;
    }
  }

  private string GetTokenId(string token)
  {
    try
    {
      var jwtToken = _tokenHandler.ReadJwtToken(token);
      return jwtToken.Id ?? "unknown";
    }
    catch
    {
      return "invalid";
    }
  }
}