using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FarmGear_Application.Configuration;
using FarmGear_Application.Models;
using FarmGear_Application.DTOs.Auth;

namespace FarmGear_Application.Services;

/// <summary>
/// 增强的JWT服务，支持会话管理和权限缓存
/// </summary>
public class EnhancedJwtService
{
  private readonly IRedisCacheService _cacheService;
  private readonly ILogger<EnhancedJwtService> _logger;
  private readonly JwtSettings _jwtSettings;
  private readonly UserManager<AppUser> _userManager;

  public EnhancedJwtService(
      IRedisCacheService cacheService,
      ILogger<EnhancedJwtService> logger,
      IOptions<JwtSettings> jwtSettings,
      UserManager<AppUser> userManager)
  {
    _cacheService = cacheService;
    _logger = logger;
    _jwtSettings = jwtSettings.Value;
    _userManager = userManager;
  }

  /// <summary>
  /// 生成JWT Token并缓存用户会话
  /// </summary>
  /// <param name="user">用户信息</param>
  /// <param name="ipAddress">IP地址</param>
  /// <param name="userAgent">用户代理</param>
  /// <returns>JWT Token字符串</returns>
  public async Task<string> GenerateTokenWithSessionAsync(AppUser user, string? ipAddress = null, string? userAgent = null)
  {
    // 获取用户角色
    var roles = await _userManager.GetRolesAsync(user);
    var role = roles.FirstOrDefault() ?? "User";

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

    // 添加所有角色到claims
    foreach (var userRole in roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, userRole));
    }

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
        signingCredentials: credentials
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    // 缓存用户会话信息
    var sessionData = new UserSessionDto
    {
      UserId = user.Id,
      Username = user.UserName ?? string.Empty,
      Email = user.Email ?? string.Empty,
      Role = role,
      LoginTime = DateTime.UtcNow,
      LastActivityTime = DateTime.UtcNow,
      IpAddress = ipAddress,
      UserAgent = userAgent,
      IsActive = true
    };

    await _cacheService.CacheUserSessionAsync(user.Id, sessionData, TimeSpan.FromMinutes(_jwtSettings.ExpiryInMinutes));

    // 缓存用户权限
    await _cacheService.CacheUserPermissionsAsync(user.Id, roles, TimeSpan.FromMinutes(_jwtSettings.ExpiryInMinutes));

    _logger.LogInformation("Generated token and cached session for user {UserId}", user.Id);

    return tokenString;
  }

  /// <summary>
  /// 验证Token并更新会话活动时间
  /// </summary>
  /// <param name="token">JWT Token</param>
  /// <param name="ipAddress">IP地址</param>
  /// <returns>验证结果</returns>
  public async Task<bool> ValidateTokenAndUpdateSessionAsync(string token, string? ipAddress = null)
  {
    try
    {
      // 检查Token是否在黑名单中
      if (await _cacheService.IsTokenBlacklistedAsync(token))
      {
        _logger.LogWarning("Token is blacklisted");
        return false;
      }

      // 解析Token
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

      tokenHandler.ValidateToken(token, new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = _jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = _jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
      }, out SecurityToken validatedToken);

      var jwtToken = (JwtSecurityToken)validatedToken;
      var userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;

      // 更新用户会话活动时间
      var session = await _cacheService.GetUserSessionAsync<UserSessionDto>(userId);
      if (session != null)
      {
        session.LastActivityTime = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(ipAddress))
        {
          session.IpAddress = ipAddress;
        }

        await _cacheService.CacheUserSessionAsync(userId, session, TimeSpan.FromMinutes(_jwtSettings.ExpiryInMinutes));
        _logger.LogInformation("Updated session activity for user {UserId}", userId);
      }

      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error validating token");
      return false;
    }
  }

  /// <summary>
  /// 使Token失效（加入黑名单）
  /// </summary>
  /// <param name="token">JWT Token</param>
  /// <returns>是否成功</returns>
  public async Task<bool> InvalidateTokenAsync(string token)
  {
    try
    {
      // 将Token加入黑名单
      var result = await _cacheService.BlacklistTokenAsync(token, TimeSpan.FromMinutes(_jwtSettings.ExpiryInMinutes));

      if (result)
      {
        // 解析Token获取用户ID
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;

        // 清除用户会话和权限缓存
        await _cacheService.RemoveUserSessionAsync(userId);
        await _cacheService.RemoveUserPermissionsAsync(userId);

        _logger.LogInformation("Invalidated token for user {UserId}", userId);
      }

      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error invalidating token");
      return false;
    }
  }

  /// <summary>
  /// 获取用户会话信息
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>用户会话信息</returns>
  public async Task<UserSessionDto?> GetUserSessionAsync(string userId)
  {
    return await _cacheService.GetUserSessionAsync<UserSessionDto>(userId);
  }

  /// <summary>
  /// 获取用户权限信息
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>用户权限列表</returns>
  public async Task<IEnumerable<string>?> GetUserPermissionsAsync(string userId)
  {
    return await _cacheService.GetUserPermissionsAsync(userId);
  }

  /// <summary>
  /// 清除用户所有会话
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>是否成功</returns>
  public async Task<bool> ClearUserSessionsAsync(string userId)
  {
    try
    {
      await _cacheService.RemoveUserSessionAsync(userId);
      await _cacheService.RemoveUserPermissionsAsync(userId);

      _logger.LogInformation("Cleared all sessions for user {UserId}", userId);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error clearing sessions for user {UserId}", userId);
      return false;
    }
  }

  /// <summary>
  /// 检查用户是否有指定权限
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <param name="permission">权限</param>
  /// <returns>是否有权限</returns>
  public async Task<bool> HasPermissionAsync(string userId, string permission)
  {
    try
    {
      var permissions = await _cacheService.GetUserPermissionsAsync(userId);
      return permissions?.Contains(permission) ?? false;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking permission for user {UserId}", userId);
      return false;
    }
  }

  /// <summary>
  /// 刷新用户会话
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>是否成功</returns>
  public async Task<bool> RefreshUserSessionAsync(string userId)
  {
    try
    {
      var session = await _cacheService.GetUserSessionAsync<UserSessionDto>(userId);
      if (session != null)
      {
        session.LastActivityTime = DateTime.UtcNow;
        await _cacheService.CacheUserSessionAsync(userId, session, TimeSpan.FromMinutes(_jwtSettings.ExpiryInMinutes));

        _logger.LogInformation("Refreshed session for user {UserId}", userId);
        return true;
      }

      return false;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error refreshing session for user {UserId}", userId);
      return false;
    }
  }
}