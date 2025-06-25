using FarmGear_Application.Constants;
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Auth;
using FarmGear_Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
  private readonly UserManager<AppUser> _userManager;
  private readonly SignInManager<AppUser> _signInManager;
  private readonly EnhancedJwtService _enhancedJwtService;
  private readonly IEmailSender _emailSender;
  private readonly RoleManager<IdentityRole> _roleManager;

  public AuthService(
      UserManager<AppUser> userManager,
      SignInManager<AppUser> signInManager,
      EnhancedJwtService enhancedJwtService,
      IEmailSender emailSender,
      RoleManager<IdentityRole> roleManager)
  {
    _userManager = userManager;
    _signInManager = signInManager;
    _enhancedJwtService = enhancedJwtService;
    _emailSender = emailSender;
    _roleManager = roleManager;
  }

  /// <inheritdoc/>
  public async Task<RegisterResponseDto> RegisterAsync(RegisterRequest request)
  {
    // 验证角色是否存在
    if (!await _roleManager.RoleExistsAsync(request.Role))
    {
      return new RegisterResponseDto
      {
        Success = false,
        Message = $"Role '{request.Role}' does not exist"
      };
    }

    // 检查用户名是否已存在
    if (await _userManager.Users.AnyAsync(u => u.UserName == request.Username))
    {
      return new RegisterResponseDto
      {
        Success = false,
        Message = "Username already exists"
      };
    }

    // 检查邮箱是否已存在
    if (await _userManager.Users.AnyAsync(u => u.Email == request.Email))
    {
      return new RegisterResponseDto
      {
        Success = false,
        Message = "Email already exists"
      };
    }

    // 创建用户（不包含Role字段）
    var user = new AppUser
    {
      UserName = request.Username,
      Email = request.Email,
      FullName = request.FullName,
      EmailConfirmed = false,
      IsActive = false
    };

    var result = await _userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded)
    {
      return new RegisterResponseDto
      {
        Success = false,
        Message = string.Join(", ", result.Errors.Select(e => e.Description))
      };
    }

    // 分配角色
    var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
    if (!roleResult.Succeeded)
    {
      // 如果角色分配失败，删除已创建的用户
      await _userManager.DeleteAsync(user);
      return new RegisterResponseDto
      {
        Success = false,
        Message = $"Failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}"
      };
    }

    // 发送邮箱确认
    await SendEmailConfirmationLinkAsync(user);

    return new RegisterResponseDto
    {
      Success = true,
      Message = "Registration successful. Please check your email to confirm your account.",
      UserId = user.Id
    };
  }

  /// <inheritdoc/>
  public async Task<LoginResponseDto> LoginAsync(LoginRequest request)
  {
    return await LoginAsync(request, null, null);
  }

  /// <summary>
  /// 登录逻辑：支持用户名或邮箱，检查密码、邮箱是否验证、账号是否激活，成功则返回 JWT Token。
  /// </summary>
  /// <param name="request">包含用户名/邮箱和密码的登录请求体</param>
  /// <param name="ipAddress">IP地址</param>
  /// <param name="userAgent">用户代理</param>
  /// <returns>登录结果响应对象，包含状态、提示信息和 Token（若成功）</returns>
  public async Task<LoginResponseDto> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null)
  {
    // 1. 根据用户名或邮箱查找用户
    var user = await _userManager.Users
        .FirstOrDefaultAsync(u => u.UserName == request.UsernameOrEmail || u.Email == request.UsernameOrEmail);

    // 2. 如果用户不存在，返回失败提示
    if (user == null)
    {
      return new LoginResponseDto
      {
        Success = false,
        Message = "Invalid username or email"
      };
    }

    // 3. 检查邮箱是否已经验证
    if (!user.EmailConfirmed)
    {
      return new LoginResponseDto
      {
        Success = false,
        Message = "Email is not confirmed. Please check your inbox."
      };
    }

    // 4. 检查账号是否已激活
    if (!user.IsActive)
    {
      return new LoginResponseDto
      {
        Success = false,
        Message = "Account is not activated. Please check your email to confirm your account."
      };
    }

    // 5. 验证密码是否正确
    var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
    if (!result.Succeeded)
    {
      return new LoginResponseDto
      {
        Success = false,
        Message = "Invalid password"
      };
    }

    // 6. 更新用户的最后登录时间
    user.LastLoginAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    // 7. 生成 JWT Token 并缓存会话信息
    var token = await _enhancedJwtService.GenerateTokenWithSessionAsync(user, ipAddress, userAgent);

    // 8. 获取用户角色
    var roles = await _userManager.GetRolesAsync(user);
    var role = roles.FirstOrDefault() ?? "User";

    // 9. 构建用户信息
    var userInfo = new UserInfoDto
    {
      Id = user.Id,
      Username = user.UserName ?? string.Empty,
      Email = user.Email ?? string.Empty,
      Role = role,
      EmailConfirmed = user.EmailConfirmed
    };

    // 10. 返回登录成功响应
    return new LoginResponseDto
    {
      Success = true,
      Message = "Login successful",
      Token = token,
      UserInfo = userInfo
    };
  }

  /// <inheritdoc/>
  public async Task<ApiResponse> ConfirmEmailAsync(string userId, string token)
  {
    // 根据 userId 查询用户
    var user = await _userManager.FindByIdAsync(userId);
    if (user == null)
    {
      return new ApiResponse
      {
        Success = false,
        Message = "User not found"
      };
    }

    // 验证邮箱 token
    var result = await _userManager.ConfirmEmailAsync(user, token);
    if (!result.Succeeded)
    {
      return new ApiResponse
      {
        Success = false,
        Message = string.Join(", ", result.Errors.Select(e => e.Description))
      };
    }

    // 邮箱验证成功后，激活用户账号
    user.IsActive = true;
    await _userManager.UpdateAsync(user);

    return new ApiResponse
    {
      Success = true,
      Message = "Email confirmed successfully"
    };
  }

  /// <inheritdoc/>
  public async Task<ApiResponse> SendEmailConfirmationLinkAsync(AppUser user)
  {
    // 生成邮箱验证 token
    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

    // 构造邮箱中的确认链接
    var confirmationLink = $"https://your-domain.com/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

    // 发送邮件
    var emailSent = await _emailSender.SendEmailAsync(
        user.Email!,
        "Confirm your email",
        $"Please confirm your account by clicking <a href='{confirmationLink}'>here</a>.");

    return emailSent ?
        new ApiResponse { Success = true, Message = "Confirmation email sent successfully" } :
        new ApiResponse { Success = false, Message = "Failed to send confirmation email" };
  }

  /// <summary>
  /// 检查用户名是否已被使用
  /// </summary>
  public async Task<bool> IsUsernameTakenAsync(string username)
  {
    if (string.IsNullOrWhiteSpace(username))
    {
      return false;
    }

    var user = await _userManager.FindByNameAsync(username);
    return user != null;
  }

  /// <summary>
  /// 检查邮箱是否已被注册
  /// </summary>
  public async Task<bool> IsEmailTakenAsync(string email)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      return false;
    }

    var user = await _userManager.FindByEmailAsync(email);
    return user != null;
  }
}

/// <summary>
/// 邮件发送接口
/// </summary>
public interface IEmailSender
{
  Task<bool> SendEmailAsync(string email, string subject, string message);
}

/// <summary>
/// 邮件发送实现（用于测试）
/// </summary>
public class EmailSender : IEmailSender
{
  private readonly ILogger<EmailSender> _logger;

  public EmailSender(ILogger<EmailSender> logger)
  {
    _logger = logger;
  }

  public Task<bool> SendEmailAsync(string email, string subject, string message)
  {
    _logger.LogInformation("Email would be sent to {Email} with subject: {Subject}", email, subject);
    _logger.LogInformation("Email content: {Message}", message);
    return Task.FromResult(true); // 模拟发送成功
  }
}