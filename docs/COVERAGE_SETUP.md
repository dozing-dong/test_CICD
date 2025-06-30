# 企业级代码覆盖率工具设置指南

## 🎯 概述

本项目已集成企业级代码覆盖率工具链，包括：

- **Coverlet**: Microsoft 推荐的 .NET 代码覆盖率收集器
- **ReportGenerator**: 生成美观的 HTML 覆盖率报告
- **Codecov**: 云端覆盖率分析和 PR 评论服务

## 🚀 快速开始

### 1. Codecov 设置

#### 步骤 1: 注册 Codecov
1. 访问 [Codecov.io](https://codecov.io)
2. 使用 GitHub 账户登录
3. 添加你的仓库

#### 步骤 2: 获取 Token
```bash
# 在 Codecov 仪表板中找到你的仓库
# 复制 Repository Upload Token
```

#### 步骤 3: 设置 GitHub Secrets
```bash
# 在 GitHub 仓库设置中添加 Secret:
# Name: CODECOV_TOKEN
# Value: <your-codecov-token>
```

### 2. 本地测试覆盖率

#### 安装工具
```powershell
# 安装全局工具
dotnet tool install --global dotnet-reportgenerator-globaltool
dotnet tool install --global coverlet.console
```

#### 运行覆盖率测试
```powershell
# 运行测试并生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# 生成 HTML 报告
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

#### 查看报告
```powershell
# 打开 HTML 报告
start coverage-report/index.html  # Windows
open coverage-report/index.html   # macOS
```

## 📊 覆盖率配置详解

### coverlet.runsettings 配置说明

```xml
<Configuration>
  <!-- 输出格式：Cobertura XML, OpenCover XML, JSON -->
  <Format>cobertura,opencover,json</Format>
  
  <!-- 排除测试程序集和系统库 -->
  <Exclude>[*.Tests]*,[*.Test]*,[xunit.*]*,[Microsoft.*]*</Exclude>
  
  <!-- 只包含主项目 -->
  <Include>[FarmGear_Application]*</Include>
  
  <!-- 排除自动生成的代码 -->
  <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute</ExcludeByAttribute>
  
  <!-- 排除迁移文件和构建输出 -->
  <ExcludeByFile>**/Migrations/**,**/bin/**,**/obj/**</ExcludeByFile>
</Configuration>
```

## 🔧 CI/CD 流程说明

### 覆盖率收集流程

1. **安装工具**: 自动安装 ReportGenerator 和 Coverlet
2. **运行测试**: 使用 Coverlet 收集覆盖率数据
3. **生成报告**: 创建 HTML 和 XML 格式报告
4. **上传到 Codecov**: 自动上传覆盖率数据
5. **PR 评论**: 在 Pull Request 中自动评论覆盖率变化

### 构建产物

| 产物类型 | 路径 | 用途 |
|---------|------|------|
| HTML 报告 | `TestResults/html-coverage-report/` | 可视化覆盖率报告 |
| Cobertura XML | `TestResults/**/coverage.cobertura.xml` | Codecov 上传 |
| JSON 摘要 | `TestResults/html-coverage-report/Summary.json` | PR 评论数据 |

## 📈 覆盖率指标说明

### 关键指标

- **Line Coverage (行覆盖率)**: 被执行的代码行百分比
- **Branch Coverage (分支覆盖率)**: 被执行的条件分支百分比
- **Method Coverage (方法覆盖率)**: 被调用的方法百分比

### 质量门槛建议

| 项目阶段 | 最低覆盖率 | 推荐覆盖率 |
|----------|------------|------------|
| 开发阶段 | 60% | 80% |
| 测试阶段 | 70% | 85% |
| 生产就绪 | 80% | 90%+ |

## 🎨 自定义配置

### 排除特定文件/目录

```xml
<!-- 在 coverlet.runsettings 中添加 -->
<ExcludeByFile>
  **/Migrations/**,
  **/Program.cs,
  **/Startup.cs,
  **/bin/**,
  **/obj/**
</ExcludeByFile>
```

### 排除特定类/方法

```csharp
// 使用特性排除
[ExcludeFromCodeCoverage]
public class ConfigurationClass
{
    // 这个类不会被覆盖率统计
}

[ExcludeFromCodeCoverage]
public void UtilityMethod()
{
    // 这个方法不会被覆盖率统计
}
```

## 🚨 故障排除

### 常见问题

#### 1. 覆盖率数据为空
```bash
# 检查测试是否成功运行
dotnet test --verbosity normal

# 检查 runsettings 文件路径
ls -la coverlet.runsettings
```

#### 2. Codecov 上传失败
```bash
# 检查 token 是否正确设置
echo $CODECOV_TOKEN

# 手动上传测试
curl -s https://codecov.io/bash | bash -s -- -t $CODECOV_TOKEN
```

#### 3. HTML 报告生成失败
```bash
# 检查 XML 文件是否存在
find TestResults -name "*.xml" -type f

# 手动生成报告
reportgenerator -reports:"TestResults/**/*.xml" -targetdir:"manual-report"
```

## 📝 最佳实践

### 1. 测试编写规范
- 为核心业务逻辑编写单元测试
- 为控制器编写集成测试
- 为复杂算法编写边界值测试

### 2. 覆盖率目标设定
- 设置合理的覆盖率目标（建议 80%+）
- 关注分支覆盖率，不仅仅是行覆盖率
- 定期审查低覆盖率的模块

### 3. 持续改进
- 在 PR 中监控覆盖率变化
- 定期清理无用的测试代码
- 重构时保持或提高覆盖率

## 🔗 相关链接

- [Coverlet 官方文档](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator 使用指南](https://github.com/danielpalme/ReportGenerator)
- [Codecov 集成指南](https://docs.codecov.com/docs)
- [.NET 测试最佳实践](https://docs.microsoft.com/en-us/dotnet/core/testing/) 