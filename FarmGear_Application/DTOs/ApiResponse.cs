namespace FarmGear_Application.DTOs;

/// <summary>
/// 通用 API 响应 DTO
/// </summary>
public class ApiResponse
{
  /// <summary>
  /// 是否成功
  /// </summary>
  public bool Success { get; set; }

  /// <summary>
  /// 消息
  /// </summary>
  public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 带数据的通用 API 响应 DTO
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T> : ApiResponse
{
  /// <summary>
  /// 响应数据
  /// </summary>
  public T? Data { get; set; }
}