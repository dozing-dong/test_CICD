using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Auth;
using FarmGear_Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmGear_Application.Controllers;

/// <summary>
/// 会话管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionController : ControllerBase
{
  private readonly EnhancedJwtService _jwtService;
  private readonly ILogger<SessionController> _logger;

  public SessionController(
      EnhancedJwtService jwtService,
      ILogger<SessionController> logger)
  {
    _jwtService = jwtService;
    _logger = logger;
  }

  /// <summary>
  /// 获取当前用户会话信息
  /// </summary>
  /// <returns>用户会话信息</returns>
  /// <response code="200">获取成功</response>
  /// <response code="401">未授权</response>
  /// <response code="404">会话不存在</response>
  [HttpGet("current")]
  [ProducesResponseType(typeof(ApiResponse<UserSessionDto>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetCurrentSession()
  {
    try
    {
      var userId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return BadRequest(new ApiResponse<UserSessionDto>
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      var session = await _jwtService.GetUserSessionAsync(userId);
      if (session == null)
      {
        return NotFound(new ApiResponse<UserSessionDto>
        {
          Success = false,
          Message = "Session not found"
        });
      }

      return Ok(new ApiResponse<UserSessionDto>
      {
        Success = true,
        Data = session,
        Message = "Session retrieved successfully"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving current session");
      return StatusCode(500, new ApiResponse<UserSessionDto>
      {
        Success = false,
        Message = "An error occurred while retrieving session"
      });
    }
  }

  /// <summary>
  /// 获取当前用户权限信息
  /// </summary>
  /// <returns>用户权限列表</returns>
  /// <response code="200">获取成功</response>
  /// <response code="401">未授权</response>
  [HttpGet("permissions")]
  [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> GetCurrentPermissions()
  {
    try
    {
      var userId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return BadRequest(new ApiResponse<IEnumerable<string>>
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      var permissions = await _jwtService.GetUserPermissionsAsync(userId);
      return Ok(new ApiResponse<IEnumerable<string>>
      {
        Success = true,
        Data = permissions ?? Enumerable.Empty<string>(),
        Message = "Permissions retrieved successfully"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving current permissions");
      return StatusCode(500, new ApiResponse<IEnumerable<string>>
      {
        Success = false,
        Message = "An error occurred while retrieving permissions"
      });
    }
  }

  /// <summary>
  /// 刷新当前用户会话
  /// </summary>
  /// <returns>操作结果</returns>
  /// <response code="200">刷新成功</response>
  /// <response code="401">未授权</response>
  /// <response code="404">会话不存在</response>
  [HttpPost("refresh")]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> RefreshSession()
  {
    try
    {
      var userId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      var result = await _jwtService.RefreshUserSessionAsync(userId);
      if (!result)
      {
        return NotFound(new ApiResponse
        {
          Success = false,
          Message = "Session not found"
        });
      }

      return Ok(new ApiResponse
      {
        Success = true,
        Message = "Session refreshed successfully"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error refreshing session");
      return StatusCode(500, new ApiResponse
      {
        Success = false,
        Message = "An error occurred while refreshing session"
      });
    }
  }

  /// <summary>
  /// 注销当前用户会话
  /// </summary>
  /// <returns>操作结果</returns>
  /// <response code="200">注销成功</response>
  /// <response code="401">未授权</response>
  [HttpPost("logout")]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Logout()
  {
    try
    {
      var userId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      // 获取Authorization header中的token
      var authHeader = Request.Headers["Authorization"].FirstOrDefault();
      if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
      {
        var token = authHeader.Substring("Bearer ".Length);
        await _jwtService.InvalidateTokenAsync(token);
      }

      // 清除用户会话
      await _jwtService.ClearUserSessionsAsync(userId);

      return Ok(new ApiResponse
      {
        Success = true,
        Message = "Logged out successfully"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during logout");
      return StatusCode(500, new ApiResponse
      {
        Success = false,
        Message = "An error occurred during logout"
      });
    }
  }

  /// <summary>
  /// 检查用户是否有指定权限
  /// </summary>
  /// <param name="permission">权限名称</param>
  /// <returns>权限检查结果</returns>
  /// <response code="200">检查成功</response>
  /// <response code="401">未授权</response>
  [HttpGet("check-permission/{permission}")]
  [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> CheckPermission(string permission)
  {
    try
    {
      var userId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return BadRequest(new ApiResponse<bool>
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      var hasPermission = await _jwtService.HasPermissionAsync(userId, permission);
      return Ok(new ApiResponse<bool>
      {
        Success = true,
        Data = hasPermission,
        Message = $"Permission check completed for {permission}"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking permission {Permission}", permission);
      return StatusCode(500, new ApiResponse<bool>
      {
        Success = false,
        Message = "An error occurred while checking permission"
      });
    }
  }

  /// <summary>
  /// 管理员：获取指定用户的会话信息
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>用户会话信息</returns>
  /// <response code="200">获取成功</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  /// <response code="404">会话不存在</response>
  [HttpGet("admin/user/{userId}")]
  [Authorize(Roles = "Admin")]
  [ProducesResponseType(typeof(ApiResponse<UserSessionDto>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetUserSession(string userId)
  {
    try
    {
      var session = await _jwtService.GetUserSessionAsync(userId);
      if (session == null)
      {
        return NotFound(new ApiResponse<UserSessionDto>
        {
          Success = false,
          Message = "Session not found"
        });
      }

      return Ok(new ApiResponse<UserSessionDto>
      {
        Success = true,
        Data = session,
        Message = "User session retrieved successfully"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving user session for {UserId}", userId);
      return StatusCode(500, new ApiResponse<UserSessionDto>
      {
        Success = false,
        Message = "An error occurred while retrieving user session"
      });
    }
  }

  /// <summary>
  /// 管理员：清除指定用户的所有会话
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <returns>操作结果</returns>
  /// <response code="200">清除成功</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  [HttpDelete("admin/user/{userId}")]
  [Authorize(Roles = "Admin")]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  public async Task<IActionResult> ClearUserSessions(string userId)
  {
    try
    {
      var result = await _jwtService.ClearUserSessionsAsync(userId);
      if (!result)
      {
        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = "Failed to clear user sessions"
        });
      }

      return Ok(new ApiResponse
      {
        Success = true,
        Message = $"All sessions cleared for user {userId}"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error clearing sessions for user {UserId}", userId);
      return StatusCode(500, new ApiResponse
      {
        Success = false,
        Message = "An error occurred while clearing user sessions"
      });
    }
  }
}