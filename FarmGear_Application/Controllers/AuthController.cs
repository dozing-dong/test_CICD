using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Auth;
using FarmGear_Application.Services;
using FarmGear_Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;

namespace FarmGear_Application.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
  private readonly IAuthService _authService;
  private readonly ILogger<AuthController> _logger;
  private readonly UserManager<AppUser> _userManager;

  public AuthController(
    IAuthService authService,
    ILogger<AuthController> logger,
    UserManager<AppUser> userManager)
  {
    _authService = authService;
    _logger = logger;
    _userManager = userManager;
  }

  /// <summary>
  /// 用户注册
  /// </summary>
  /// <param name="request">注册请求</param>
  /// <returns>注册响应</returns>
  /// <response code="201">注册成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="409">用户名或邮箱已存在</response>
  /// <response code="500">服务器内部错误</response>
  [HttpPost("register")]
  [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> Register([FromBody] RegisterRequest request)
  {
    try
    {
      var response = await _authService.RegisterAsync(request);

      if (!response.Success)
      {
        // 根据错误类型返回不同的状态码
        if (response.Message.Contains("already exists"))
        {
          return Conflict(new ApiResponse
          {
            Success = false,
            Message = response.Message
          });
        }

        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = response.Message
        });
      }

      return CreatedAtAction(nameof(GetCurrentUser), new { }, response);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred during registration");
      return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
      {
        Success = false,
        Message = "An error occurred during registration"
      });
    }
  }

  /// <summary>
  /// 用户登录
  /// </summary>
  /// <param name="request">登录请求</param>
  /// <returns>登录响应</returns>
  /// <response code="200">登录成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">用户名或密码错误</response>
  /// <response code="403">账号未激活或邮箱未确认</response>
  /// <response code="500">服务器内部错误</response>
  [HttpPost("login")]
  [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> Login([FromBody] LoginRequest request)
  {
    try
    {
      // 获取客户端IP地址
      var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

      // 获取用户代理
      var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

      var response = await _authService.LoginAsync(request, ipAddress, userAgent);

      if (!response.Success)
      {
        // 根据错误类型返回不同的状态码
        if (response.Message.Contains("Invalid username or email") ||
            response.Message.Contains("Invalid password"))
        {
          return Unauthorized(new ApiResponse
          {
            Success = false,
            Message = response.Message
          });
        }

        if (response.Message.Contains("not confirmed") ||
            response.Message.Contains("not activated"))
        {
          return Forbid();
        }

        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = response.Message
        });
      }

      return Ok(response);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred during login");
      return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
      {
        Success = false,
        Message = "An error occurred during login"
      });
    }
  }

  /// <summary>
  /// 用户登出
  /// </summary>
  /// <returns>通用响应</returns>
  /// <response code="200">登出成功</response>
  /// <response code="401">未授权</response>
  [HttpPost("logout")]
  [Authorize]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  public IActionResult Logout()
  {
    // 由于使用JWT，服务器端不需要维护会话状态
    // 客户端需要自行清除Token
    return Ok(new ApiResponse
    {
      Success = true,
      Message = "Logout successful"
    });
  }

  /// <summary>
  /// 确认邮箱
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <param name="token">确认Token</param>
  /// <returns>通用响应</returns>
  /// <response code="200">邮箱确认成功</response>
  /// <response code="400">Token无效或过期</response>
  /// <response code="404">用户不存在</response>
  /// <response code="500">服务器内部错误</response>
  [HttpGet("confirm-email")]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
  {
    try
    {
      if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
      {
        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = "User ID and token are required"
        });
      }

      var response = await _authService.ConfirmEmailAsync(userId, token);

      if (!response.Success)
      {
        if (response.Message.Contains("User not found"))
        {
          return NotFound(new ApiResponse
          {
            Success = false,
            Message = response.Message
          });
        }

        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = response.Message
        });
      }

      return Ok(response);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred during email confirmation");
      return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
      {
        Success = false,
        Message = "An error occurred during email confirmation"
      });
    }
  }

  /// <summary>
  /// 获取当前登录用户信息
  /// </summary>
  /// <returns>用户信息</returns>
  /// <response code="200">获取成功</response>
  /// <response code="401">未授权</response>
  /// <response code="404">用户不存在</response>
  /// <response code="500">服务器内部错误</response>
  [HttpGet("me")]
  [Authorize]
  [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetCurrentUser()
  {
    try
    {
      // 从 Claims 中获取用户 ID
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return Unauthorized(new ApiResponse
        {
          Success = false,
          Message = "User not authenticated"
        });
      }

      // 获取用户信息
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null)
      {
        return NotFound(new ApiResponse
        {
          Success = false,
          Message = "User not found"
        });
      }

      // 获取用户角色
      var roles = await _userManager.GetRolesAsync(user);
      var role = roles.FirstOrDefault() ?? "User";

      // 构建返回的 DTO
      var userInfo = new UserInfoDto
      {
        Id = user.Id,
        Username = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        Role = role,
        EmailConfirmed = user.EmailConfirmed
      };

      return Ok(userInfo);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while getting current user info");
      return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
      {
        Success = false,
        Message = "An error occurred while getting user information"
      });
    }
  }
}