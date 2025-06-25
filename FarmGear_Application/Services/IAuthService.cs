using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Auth;
using FarmGear_Application.Models;

namespace FarmGear_Application.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
  /// <summary>
  /// 用户注册
  /// </summary>
  /// <param name="request">注册请求</param>
  /// <returns>注册响应</returns>
  Task<RegisterResponseDto> RegisterAsync(RegisterRequest request);

  /// <summary>
  /// 用户登录
  /// </summary>
  /// <param name="request">登录请求</param>
  /// <returns>登录响应</returns>
  Task<LoginResponseDto> LoginAsync(LoginRequest request);

  /// <summary>
  /// 用户登录（带IP地址和用户代理信息）
  /// </summary>
  /// <param name="request">登录请求</param>
  /// <param name="ipAddress">IP地址</param>
  /// <param name="userAgent">用户代理</param>
  /// <returns>登录响应</returns>
  Task<LoginResponseDto> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);

  /// <summary>
  /// 确认邮箱
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <param name="token">确认Token</param>
  /// <returns>确认结果</returns>
  Task<ApiResponse> ConfirmEmailAsync(string userId, string token);

  /// <summary>
  /// 发送邮箱确认链接
  /// </summary>
  /// <param name="user">用户信息</param>
  /// <returns>发送结果</returns>
  Task<ApiResponse> SendEmailConfirmationLinkAsync(AppUser user);

  /// <summary>
  /// 检查用户名是否已被使用
  /// </summary>
  /// <param name="username">用户名</param>
  /// <returns>是否已被使用</returns>
  Task<bool> IsUsernameTakenAsync(string username);

  /// <summary>
  /// 检查邮箱是否已被注册
  /// </summary>
  /// <param name="email">邮箱</param>
  /// <returns>是否已被注册</returns>
  Task<bool> IsEmailTakenAsync(string email);
}