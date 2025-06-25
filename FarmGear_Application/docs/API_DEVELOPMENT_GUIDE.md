# FarmGear API 开发指南

## 目录
1. [认证与授权](#认证与授权)
2. [现有 API 接口](#现有-api-接口)
3. [开发注意事项](#开发注意事项)
4. [最佳实践](#最佳实践)

## 认证与授权

### JWT 认证
- 所有需要认证的接口都需要在请求头中携带 JWT Token
- Token 格式：`Authorization: Bearer <your-token>`
- Token 通过登录接口获取
- Token 过期后需要重新登录

### 用户角色
系统支持以下角色：
- Farmer（农户）
- Provider（供应商）
- Admin（管理员）
- Official（官方人员）

## 现有 API 接口

### 认证相关接口

#### 1. 用户注册
- 路径：`POST /api/auth/register`
- 功能：注册新用户
- 请求体：`RegisterRequest`
  ```json
  {
    "username": "string",
    "email": "string",
    "password": "string",
    "confirmPassword": "string",
    "fullName": "string",
    "role": "Farmer|Provider|Admin|Official"
  }
  ```
- 返回：`RegisterResponse`
  ```json
  {
    "success": true,
    "message": "string",
    "userId": "string"
  }
  ```

#### 2. 用户登录
- 路径：`POST /api/auth/login`
- 功能：用户登录并获取 JWT Token
- 请求体：`LoginRequest`
  ```json
  {
    "usernameOrEmail": "string",
    "password": "string",
    "rememberMe": boolean
  }
  ```
- 返回：`LoginResponse`
  ```json
  {
    "success": true,
    "message": "string",
    "token": "string"
  }
  ```

#### 3. 用户登出
- 路径：`POST /api/auth/logout`
- 功能：用户登出（客户端需要清除 Token）
- 需要认证：是
- 返回：`ApiResponse`

#### 4. 确认邮箱
- 路径：`GET /api/auth/confirm-email`
- 功能：确认用户邮箱
- 参数：
  - userId: 用户ID
  - token: 确认Token
- 返回：`ApiResponse`

#### 5. 获取当前用户信息
- 路径：`GET /api/auth/me`
- 功能：获取当前登录用户的详细信息
- 需要认证：是
- 返回：`UserInfoDto`
  ```json
  {
    "id": "string",
    "username": "string",
    "email": "string",
    "role": "string",
    "emailConfirmed": boolean
  }
  ```

### 设备相关接口

#### 1. 创建设备
- 路径：`POST /api/equipment`
- 功能：创建新设备
- 需要认证：是（需要 Provider 或 Official 角色）
- 请求体：`CreateEquipmentRequest`
  ```json
  {
    "name": "string",
    "description": "string",
    "dailyPrice": number,
    "latitude": number,
    "longitude": number
  }
  ```
- 返回：`ApiResponse<EquipmentViewDto>`

#### 2. 获取设备列表
- 路径：`GET /api/equipment`
- 功能：获取所有设备列表，支持分页和筛选
- 查询参数：`EquipmentQueryParameters`
  - pageNumber: 页码
  - pageSize: 每页数量
  - searchTerm: 搜索关键词
  - minPrice: 最低价格
  - maxPrice: 最高价格
  - status: 设备状态
  - sortBy: 排序字段
  - sortOrder: 排序方向
- 返回：`ApiResponse<PaginatedList<EquipmentViewDto>>`

#### 3. 获取设备详情
- 路径：`GET /api/equipment/{id}`
- 功能：获取指定设备的详细信息
- 参数：id（设备ID）
- 返回：`ApiResponse<EquipmentViewDto>`

#### 4. 获取我的设备列表
- 路径：`GET /api/equipment/my-equipment`
- 功能：获取当前登录用户拥有的设备列表，支持分页和筛选
- 需要认证：是
- 查询参数：与获取设备列表相同
- 返回：`ApiResponse<PaginatedList<EquipmentViewDto>>`

#### 5. 更新设备
- 路径：`PUT /api/equipment/{id}`
- 功能：更新指定设备的信息
- 需要认证：是（需要是设备所有者或管理员）
- 参数：id（设备ID）
- 请求体：`UpdateEquipmentRequest`
  ```json
  {
    "name": "string",
    "description": "string",
    "dailyPrice": number,
    "latitude": number,
    "longitude": number,
    "status": "Available|Rented|Maintenance|Offline"
  }
  ```
- 返回：`ApiResponse<EquipmentViewDto>`

#### 6. 删除设备
- 路径：`DELETE /api/equipment/{id}`
- 功能：删除指定设备
- 需要认证：是（需要是设备所有者或管理员）
- 参数：id（设备ID）
- 返回：`ApiResponse`

### 订单相关接口

#### 1. 创建订单
- 路径：`POST /api/order`
- 功能：创建新的租赁订单
- 需要认证：是（需要 Farmer 角色）
- 请求体：`CreateOrderRequest`
  ```json
  {
    "equipmentId": "string",
    "startDate": "2024-03-20T00:00:00Z",
    "endDate": "2024-03-25T00:00:00Z"
  }
  ```
- 返回：`ApiResponse<OrderViewDto>`
  ```json
  {
    "success": true,
    "data": {
      "id": "string",
      "equipmentId": "string",
      "equipmentName": "string",
      "renterId": "string",
      "renterName": "string",
      "startDate": "2024-03-20T00:00:00Z",
      "endDate": "2024-03-25T00:00:00Z",
      "totalAmount": 1000.00,
      "status": "Pending",
      "createdAt": "2024-03-19T10:00:00Z",
      "updatedAt": "2024-03-19T10:00:00Z"
    }
  }
  ```

#### 2. 获取订单列表
- 路径：`GET /api/order`
- 功能：获取订单列表，支持分页和筛选
- 需要认证：是
- 查询参数：`OrderQueryParameters`
  - pageNumber: 页码（默认1）
  - pageSize: 每页数量（默认10）
  - status: 订单状态
  - startDateFrom: 开始日期范围（起始）
  - startDateTo: 开始日期范围（结束）
  - endDateFrom: 结束日期范围（起始）
  - endDateTo: 结束日期范围（结束）
  - minTotalAmount: 最低总金额
  - maxTotalAmount: 最高总金额
  - sortBy: 排序字段（createdAt/startDate/endDate/totalAmount/status）
  - isAscending: 是否升序
- 返回：`ApiResponse<PaginatedList<OrderViewDto>>`

#### 3. 获取订单详情
- 路径：`GET /api/order/{id}`
- 功能：获取指定订单的详细信息
- 需要认证：是（需要是订单的租客、设备所有者或管理员）
- 参数：id（订单ID）
- 返回：`ApiResponse<OrderViewDto>`

#### 4. 更新订单状态
- 路径：`PUT /api/order/{id}/status`
- 功能：更新订单状态（接受/拒绝/完成）
- 需要认证：是（需要是设备所有者或管理员）
- 参数：id（订单ID）
- 请求体：
  ```json
  {
    "status": "Accepted|Rejected|Completed"
  }
  ```
- 返回：`ApiResponse<OrderViewDto>`

#### 5. 取消订单
- 路径：`PUT /api/order/{id}/cancel`
- 功能：取消订单
- 需要认证：是（需要是订单的租客、设备所有者或管理员）
- 参数：id（订单ID）
- 返回：`ApiResponse<OrderViewDto>`

#### 订单状态说明
订单状态流转规则：
- Pending（待处理）：初始状态
  - 可转换为：Accepted（已接受）、Rejected（已拒绝）
- Accepted（已接受）
  - 可转换为：Completed（已完成）、Cancelled（已取消）
- Rejected（已拒绝）：终态
- Completed（已完成）：终态
- Cancelled（已取消）：终态

#### 订单权限说明
1. 创建订单：需要 Farmer 角色
2. 查看订单：
   - 管理员可以查看所有订单
   - 普通用户只能查看自己的订单（作为租客或设备所有者）
3. 更新订单状态：
   - 只有设备所有者或管理员可以更新订单状态
4. 取消订单：
   - 订单的租客可以取消
   - 设备所有者可以取消
   - 管理员可以取消

### 支付相关接口

#### 1. 创建支付意图
- 路径：`POST /api/payment/intent`
- 功能：为订单创建支付意图，生成支付URL
- 需要认证：是（需要是订单的租客）
- 请求体：`CreatePaymentIntentRequest`
  ```json
  {
    "orderId": "string"
  }
  ```
- 返回：`ApiResponse<PaymentStatusResponse>`
  ```json
  {
    "success": true,
    "message": "string",
    "data": {
      "id": "string",
      "orderId": "string",
      "amount": 1000.00,
      "status": "Pending",
      "paidAt": null,
      "createdAt": "2024-03-19T10:00:00Z",
      "userId": "string",
      "userName": "string",
      "paymentUrl": "https://openapi.alipay.com/gateway.do?..."
    }
  }
  ```
- 错误响应：
  - 400: 请求参数错误
  - 401: 未授权
  - 403: 无权限（非订单租客）
  - 404: 订单不存在
  - 409: 订单已有支付记录

#### 2. 获取支付状态
- 路径：`GET /api/payment/status/{orderId}`
- 功能：获取指定订单的支付状态
- 需要认证：是（需要是订单的租客）
- 参数：orderId（订单ID）
- 返回：`ApiResponse<PaymentStatusResponse>`
- 错误响应：
  - 401: 未授权
  - 403: 无权限（非订单租客）
  - 404: 订单或支付记录不存在

#### 3. 模拟支付完成（仅用于测试）
- 路径：`POST /api/payment/complete/{orderId}`
- 功能：模拟支付完成，用于测试环境
- 需要认证：是（需要是订单的租客）
- 参数：orderId（订单ID）
- 返回：`ApiResponse<PaymentStatusResponse>`
- 错误响应：
  - 400: 请求参数错误
  - 401: 未授权
  - 403: 无权限（非订单租客）
  - 404: 订单或支付记录不存在
  - 409: 支付状态不正确

#### 4. 支付宝支付回调
- 路径：`POST /api/payment/callback`
- 功能：处理支付宝支付结果通知
- 需要认证：否（支付宝服务器调用）
- 请求体：支付宝回调参数（Form表单）
- 返回：
  - 成功：`"success"`
  - 失败：`"fail"`
- 注意事项：
  1. 此接口为支付宝服务器异步通知接口
  2. 需要配置支付宝回调地址（NotifyUrl）
  3. 需要验证签名确保通知来源可靠
  4. 需要处理重复通知
  5. 需要实现幂等性处理

#### 支付状态说明
支付状态流转规则：
- Pending（待支付）：初始状态
  - 可转换为：Paid（支付成功）、Failed（支付失败）
- Paid（支付成功）：终态
- Failed（支付失败）：终态

#### 支付权限说明
1. 创建支付意图：
   - 只有订单的租客可以创建支付意图
   - 订单必须处于已接受（Accepted）状态
2. 查看支付状态：
   - 只有订单的租客可以查看支付状态
3. 支付回调处理：
   - 由支付宝服务器调用
   - 需要验证签名确保安全性
   - 需要实现幂等性处理避免重复处理

#### 支付宝集成说明
1. 配置要求：
   - 需要在 appsettings.json 中配置支付宝相关参数
   - 需要配置正确的回调地址（NotifyUrl）
   - 需要配置正确的网关地址（GatewayUrl）
2. 开发注意事项：
   - 当前实现为示例代码，需要替换为支付宝官方SDK
   - 需要安装 AlipaySDKNet.Standard NuGet包
   - 需要实现完整的签名验证
   - 需要处理各种异常情况
   - 需要实现订单金额验证
   - 需要实现重复通知处理
   - 需要实现幂等性处理
   - 需要添加详细的日志记录
   - 需要实现异常重试机制

## 开发注意事项

### 1. 认证相关
- 所有需要认证的接口必须添加 `[Authorize]` 特性
- 获取当前用户ID：`User.FindFirst("sub")?.Value`
- 获取当前用户角色：使用 `UserManager<AppUser>.GetRolesAsync()`
- 检查用户权限：使用 `[Authorize(Roles = "RoleName")]` 特性

### 2. 数据验证
- 使用 FluentValidation 进行请求验证
- 所有 DTO 类都应该有对应的验证器
- 验证器注册在 `Program.cs` 中

### 3. 错误处理
- 使用统一的 `ApiResponse` 格式返回错误信息
- 所有异常都应该被捕获并记录日志
- HTTP 状态码使用规范：
  - 200：成功
  - 400：请求参数错误
  - 401：未认证
  - 403：无权限
  - 404：资源不存在
  - 500：服务器错误

### 4. 日志记录
- 使用 `ILogger<T>` 记录关键操作和错误
- 日志级别使用规范：
  - Information：普通操作日志
  - Warning：需要注意但不影响系统运行的问题
  - Error：需要立即处理的错误
  - Critical：系统级错误

## 最佳实践

### 1. 新接口开发
- 遵循 RESTful API 设计规范
- 使用适当的 HTTP 方法（GET、POST、PUT、DELETE）
- 使用 DTO 进行数据传输，不要直接暴露数据库实体
- 添加适当的 XML 文档注释
- 使用 `[ProducesResponseType]` 特性标注可能的响应类型

### 2. 安全性
- 所有用户输入必须经过验证
- 敏感数据（如密码）必须加密存储
- 使用 HTTPS 进行传输
- 实现适当的速率限制防止暴力攻击
- 定期检查并更新依赖包

### 3. 性能优化
- 使用异步方法（async/await）
- 合理使用缓存
- 避免 N+1 查询问题
- 使用分页处理大量数据

### 4. 代码组织
- 遵循 SOLID 原则
- 使用依赖注入
- 保持控制器简洁，业务逻辑放在服务层
- 使用仓储模式访问数据库
- 使用适当的异常处理策略

## 示例代码

### 添加新的受保护接口
```csharp
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly ILogger<ExampleController> _logger;
    private readonly UserManager<AppUser> _userManager;

    public ExampleController(
        ILogger<ExampleController> logger,
        UserManager<AppUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet("protected-resource")]
    [Authorize] // 需要认证
    [Authorize(Roles = "Admin")] // 需要特定角色
    public async Task<IActionResult> GetProtectedResource()
    {
        try
        {
            // 获取当前用户ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse 
                { 
                    Success = false, 
                    Message = "User not authenticated" 
                });
            }

            // 业务逻辑...

            return Ok(new ApiResponse { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while accessing protected resource");
            return StatusCode(500, new ApiResponse 
            { 
                Success = false, 
                Message = "An error occurred" 
            });
        }
    }
}
``` 