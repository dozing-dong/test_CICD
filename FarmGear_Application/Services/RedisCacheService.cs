using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FarmGear_Application.Services;

/// <summary>
/// Redis缓存服务
/// </summary>
public class RedisCacheService : IRedisCacheService
{
  private readonly IDatabase _db;
  private readonly ILogger<RedisCacheService> _logger;

  public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
  {
    _db = redis.GetDatabase();
    _logger = logger;
  }

  /// <summary>
  /// 缓存用户会话信息
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <param name="sessionData">会话数据</param>
  /// <param name="expiry">过期时间</param>
  /// <returns>是否缓存成功</returns>
  public async Task<bool> CacheUserSessionAsync(string userId, object sessionData, TimeSpan? expiry = null)
  {
    try
    {
      var key = $"session:user:{userId}";
      var value = JsonSerializer.Serialize(sessionData);
      var result = await _db.StringSetAsync(key, value, expiry ?? TimeSpan.FromHours(24));

      _logger.LogInformation("Cached user session for user {UserId}", userId);
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error caching user session for user {UserId}", userId);
      return false;
    }
  }

  /// <summary>
  /// 获取用户会话信息
  /// </summary>
  /// <typeparam name="T">会话数据类型</typeparam>
  /// <param name="userId">用户ID</param>
  /// <returns>会话数据</returns>
  public async Task<T?> GetUserSessionAsync<T>(string userId)
  {
    try
    {
      var key = $"session:user:{userId}";
      var value = await _db.StringGetAsync(key);

      if (value.HasValue)
      {
        var result = JsonSerializer.Deserialize<T>(value!);
        _logger.LogInformation("Retrieved user session for user {UserId}", userId);
        return result;
      }

      return default;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving user session for user {UserId}", userId);
      return default;
    }
  }

  /// <summary>
  /// 缓存JWT Token黑名单
  /// </summary>
  /// <param name="token">JWT Token</param>
  /// <param name="expiry">过期时间</param>
  /// <returns>是否缓存成功</returns>
  public async Task<bool> BlacklistTokenAsync(string token, TimeSpan? expiry = null)
  {
    try
    {
      var key = $"blacklist:token:{token}";
      var result = await _db.StringSetAsync(key, "1", expiry ?? TimeSpan.FromHours(24));

      _logger.LogInformation("Blacklisted token");
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error blacklisting token");
      return false;
    }
  }

  /// <summary>
  /// 检查Token是否在黑名单中
  /// </summary>
  /// <param name="token">JWT Token</param>
  /// <returns>是否在黑名单中</returns>
  public async Task<bool> IsTokenBlacklistedAsync(string token)
  {
    try
    {
      var key = $"blacklist:token:{token}";
      var exists = await _db.KeyExistsAsync(key);

      _logger.LogInformation("Checked token blacklist status: {IsBlacklisted}", exists);
      return exists;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking token blacklist");
      return false;
    }
  }

  /// <summary>
  /// 缓存用户权限信息
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <param name="permissions">权限列表</param>
  /// <param name="expiry">过期时间</param>
  /// <returns>是否缓存成功</returns>
  public async Task<bool> CacheUserPermissionsAsync(string userId, IEnumerable<string> permissions, TimeSpan? expiry = null)
  {
    try
    {
      var key = $"permissions:user:{userId}";
      var value = JsonSerializer.Serialize(permissions);
      var result = await _db.StringSetAsync(key, value, expiry ?? TimeSpan.FromHours(1));

      _logger.LogInformation("Cached permissions for user {UserId}", userId);
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error caching permissions for user {UserId}", userId);
      return false;
    }
  }

  /// <summary>
  /// 获取用户权限信息
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>权限列表</returns>
  public async Task<IEnumerable<string>?> GetUserPermissionsAsync(string userId)
  {
    try
    {
      var key = $"permissions:user:{userId}";
      var value = await _db.StringGetAsync(key);

      if (value.HasValue)
      {
        var result = JsonSerializer.Deserialize<IEnumerable<string>>(value!);
        _logger.LogInformation("Retrieved permissions for user {UserId}", userId);
        return result;
      }

      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving permissions for user {UserId}", userId);
      return null;
    }
  }

  /// <summary>
  /// 删除用户会话
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>是否删除成功</returns>
  public async Task<bool> RemoveUserSessionAsync(string userId)
  {
    try
    {
      var key = $"session:user:{userId}";
      var result = await _db.KeyDeleteAsync(key);

      _logger.LogInformation("Removed user session for user {UserId}", userId);
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing user session for user {UserId}", userId);
      return false;
    }
  }

  /// <summary>
  /// 删除用户权限缓存
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>是否删除成功</returns>
  public async Task<bool> RemoveUserPermissionsAsync(string userId)
  {
    try
    {
      var key = $"permissions:user:{userId}";
      var result = await _db.KeyDeleteAsync(key);

      _logger.LogInformation("Removed permissions cache for user {UserId}", userId);
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing permissions cache for user {UserId}", userId);
      return false;
    }
  }

  /// <summary>
  /// 设置缓存项
  /// </summary>
  /// <param name="key">缓存键</param>
  /// <param name="value">缓存值</param>
  /// <param name="expiry">过期时间</param>
  /// <returns>是否设置成功</returns>
  public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
  {
    try
    {
      var result = await _db.StringSetAsync(key, value, expiry);
      _logger.LogInformation("Set cache key: {Key}", key);
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error setting cache key: {Key}", key);
      return false;
    }
  }

  /// <summary>
  /// 获取缓存项
  /// </summary>
  /// <param name="key">缓存键</param>
  /// <returns>缓存值</returns>
  public async Task<string?> GetAsync(string key)
  {
    try
    {
      var value = await _db.StringGetAsync(key);
      if (value.HasValue)
      {
        _logger.LogInformation("Retrieved cache key: {Key}", key);
        return value;
      }
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting cache key: {Key}", key);
      return null;
    }
  }

  /// <summary>
  /// 删除缓存项
  /// </summary>
  /// <param name="key">缓存键</param>
  /// <returns>是否删除成功</returns>
  public async Task<bool> RemoveAsync(string key)
  {
    try
    {
      var result = await _db.KeyDeleteAsync(key);
      _logger.LogInformation("Removed cache key: {Key}", key);
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing cache key: {Key}", key);
      return false;
    }
  }
}