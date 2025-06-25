namespace FarmGear_Application.Services;

/// <summary>
/// Redis缓存服务接口
/// </summary>
public interface IRedisCacheService
{
  /// <summary>
  /// 缓存用户会话信息
  /// </summary>
  Task<bool> CacheUserSessionAsync(string userId, object sessionData, TimeSpan? expiry = null);

  /// <summary>
  /// 获取用户会话信息
  /// </summary>
  Task<T?> GetUserSessionAsync<T>(string userId);

  /// <summary>
  /// 缓存JWT Token黑名单
  /// </summary>
  Task<bool> BlacklistTokenAsync(string token, TimeSpan? expiry = null);

  /// <summary>
  /// 检查Token是否在黑名单中
  /// </summary>
  Task<bool> IsTokenBlacklistedAsync(string token);

  /// <summary>
  /// 缓存用户权限信息
  /// </summary>
  Task<bool> CacheUserPermissionsAsync(string userId, IEnumerable<string> permissions, TimeSpan? expiry = null);

  /// <summary>
  /// 获取用户权限信息
  /// </summary>
  Task<IEnumerable<string>?> GetUserPermissionsAsync(string userId);

  /// <summary>
  /// 删除用户会话
  /// </summary>
  Task<bool> RemoveUserSessionAsync(string userId);

  /// <summary>
  /// 删除用户权限缓存
  /// </summary>
  Task<bool> RemoveUserPermissionsAsync(string userId);

  /// <summary>
  /// 设置缓存
  /// </summary>
  Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null);

  /// <summary>
  /// 获取缓存
  /// </summary>
  Task<string?> GetAsync(string key);

  /// <summary>
  /// 删除缓存
  /// </summary>
  Task<bool> RemoveAsync(string key);
}