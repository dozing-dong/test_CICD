# FarmGear Application Tests

这是FarmGear应用程序的独立xUnit测试项目，提供了完整的单元测试和集成测试覆盖。

## 项目结构

```
FarmGear_Application.Tests/
├── Controllers/                 # 控制器单元测试
│   └── EquipmentControllerTests.cs
├── Integration/                 # 集成测试
│   └── EquipmentControllerIntegrationTests.cs
├── Helpers/                     # 测试辅助类
│   └── TestDataFactory.cs
├── TestBase.cs                  # 测试基类
├── xunit.runner.json           # xUnit运行器配置
├── FarmGear_Application.Tests.csproj
└── README.md
```

## 依赖项

- **xUnit** - 测试框架
- **Moq** - Mock框架，用于模拟依赖项
- **FluentAssertions** - 流畅的断言库
- **Microsoft.AspNetCore.Mvc.Testing** - ASP.NET Core集成测试
- **coverlet.collector** - 代码覆盖率收集器

## 测试类别

### 1. 单元测试 (Unit Tests)

#### EquipmentControllerTests
对`EquipmentController`的所有公共方法进行测试：

- **CreateEquipment** - 创建设备的各种场景
- **GetEquipmentList** - 获取设备列表
- **GetEquipmentById** - 获取设备详情
- **GetMyEquipmentList** - 获取用户设备列表
- **UpdateEquipment** - 更新设备信息
- **DeleteEquipment** - 删除设备
- **异常处理** - 验证异常情况的处理

测试覆盖场景：
- ✅ 正常业务流程
- ✅ 参数验证
- ✅ 权限检查
- ✅ 错误处理
- ✅ 异常情况

### 2. 集成测试 (Integration Tests)

#### EquipmentControllerIntegrationTests
使用`WebApplicationFactory`进行端到端测试：

- 验证HTTP状态码
- 验证认证和授权
- 验证API响应格式

## 运行测试

### 使用.NET CLI

```bash
# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~EquipmentControllerTests"

# 运行测试并生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"

# 运行测试并输出详细信息
dotnet test --logger "console;verbosity=detailed"
```

### 使用Visual Studio

1. 打开**测试资源管理器** (Test Explorer)
2. 点击**运行所有测试**
3. 查看测试结果和覆盖率

### 使用JetBrains Rider

1. 右键点击测试项目或测试类
2. 选择**Run Tests**或**Debug Tests**
3. 查看测试结果窗口

## 测试配置

### xunit.runner.json
```json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4,
  "methodDisplay": "method",
  "methodDisplayOptions": "all"
}
```

## 辅助类说明

### TestBase
提供通用的测试辅助方法：
- `SetupUserContext()` - 设置控制器的用户上下文
- `GetTestEquipmentData()` - 获取测试设备数据

### TestDataFactory
提供测试数据创建方法：
- `CreateEquipmentRequest()` - 创建设备请求DTO
- `CreateUpdateEquipmentRequest()` - 更新设备请求DTO
- `CreateEquipmentViewDto()` - 创建设备视图DTO
- `CreateSuccessResponse()` / `CreateErrorResponse()` - 创建API响应

### TestAuthenticationHandler
用于集成测试的认证处理器，提供模拟的用户身份验证。

## Mock策略

使用Moq框架模拟以下依赖项：
- `IEquipmentService` - 设备服务接口
- `ILogger<EquipmentController>` - 日志记录器

## 测试数据

测试使用以下默认测试数据：
- **用户ID**: `test-user-id`
- **设备ID**: `test-equipment-id`
- **位置**: 北京（纬度: 39.9042, 经度: 116.4074）
- **设备类型**: Tractor
- **日租金**: 100.00元

## 最佳实践

1. **AAA模式** - 每个测试方法遵循Arrange-Act-Assert模式
2. **命名规范** - 使用`MethodName_Scenario_ExpectedResult`格式
3. **独立性** - 每个测试独立运行，不依赖其他测试
4. **Mock验证** - 验证Mock对象的调用次数和参数
5. **异常测试** - 确保异常情况得到正确处理

## 代码覆盖率

运行测试后可以生成代码覆盖率报告：

```bash
# 生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"

# 如果安装了reportgenerator工具
reportgenerator -reports:"**/*.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

## 持续集成

这些测试适用于CI/CD管道：

```yaml
# GitHub Actions示例
- name: Run Tests
  run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
  
- name: Upload coverage reports
  uses: codecov/codecov-action@v3
```

## 扩展测试

要添加新的测试：

1. 在相应目录创建测试类
2. 继承`TestBase`类（可选）
3. 使用`TestDataFactory`创建测试数据
4. 遵循现有的命名和结构约定

## 故障排除

### 常见问题

1. **认证问题** - 确保在需要认证的测试中正确设置用户上下文
2. **Mock设置** - 验证Mock对象的Setup是否正确
3. **异步测试** - 确保使用`await`关键字

### 调试技巧

1. 使用调试器逐步调试测试
2. 添加`Console.WriteLine()`输出中间状态
3. 检查Mock对象的`Verify()`调用

## 贡献指南

1. 为新功能添加相应的单元测试
2. 确保测试覆盖率不下降
3. 遵循现有的测试模式和命名约定
4. 更新文档说明新增的测试 