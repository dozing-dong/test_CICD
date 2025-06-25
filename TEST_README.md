# FarmGear 测试环境说明

## 概述

本项目使用Docker容器来提供测试环境，确保测试的一致性和隔离性。

## 测试环境架构

### 容器服务
- **MySQL 8.0**: 测试数据库 (端口: 3307)
- **Redis 7**: 测试缓存 (端口: 6380)

### 端口映射
- MySQL: `localhost:3307` → `container:3306`
- Redis: `localhost:6380` → `container:6379`

## 快速开始

### 前置要求
- Docker Desktop
- .NET 8.0 SDK
- PowerShell (Windows)

### 运行测试

#### 方法1: 使用自动化脚本 (推荐)
```powershell
# 在项目根目录执行
.\run-tests.ps1
```

#### 方法2: 手动步骤
```powershell
# 1. 启动测试容器
docker-compose -f docker-compose.test.yml up -d

# 2. 等待容器启动完成
# 检查MySQL状态
docker exec farmgear-mysql-test mysqladmin ping -h localhost

# 检查Redis状态
docker exec farmgear-redis-test redis-cli ping

# 3. 运行测试
cd FarmGear_Application.Tests
dotnet test --verbosity normal

# 4. 清理容器
cd ..
docker-compose -f docker-compose.test.yml down -v
```

## 测试配置

### 数据库连接
- **服务器**: localhost:3307
- **数据库**: FarmGearTestDb
- **用户名**: testuser
- **密码**: test123456

### Redis连接
- **服务器**: localhost:6380
- **密码**: 无

## 测试类型

### 1. 单元测试
- 位置: `FarmGear_Application.Tests/Controllers/`
- 运行: `dotnet test --filter "Category=Unit"`

### 2. 集成测试
- 位置: `FarmGear_Application.Tests/Integration/`
- 运行: `dotnet test --filter "Category=Integration"`

### 3. 所有测试
- 运行: `dotnet test`

## 测试数据管理

### 数据库初始化
- 测试运行前自动创建数据库结构
- 自动应用EF Core迁移
- 自动初始化用户角色

### 数据隔离
- 每个测试类使用独立的数据库连接
- 测试完成后自动清理数据

## 故障排除

### 常见问题

#### 1. 容器启动失败
```powershell
# 检查Docker状态
docker info

# 清理所有容器
docker system prune -a
```

#### 2. 端口冲突
```powershell
# 检查端口占用
netstat -ano | findstr :3307
netstat -ano | findstr :6380

# 修改docker-compose.test.yml中的端口映射
```

#### 3. 数据库连接失败
```powershell
# 检查容器状态
docker ps

# 查看容器日志
docker logs farmgear-mysql-test
docker logs farmgear-redis-test
```

#### 4. 测试超时
- 增加等待时间
- 检查网络连接
- 确保Docker有足够资源

### 调试模式

#### 进入容器调试
```powershell
# MySQL容器
docker exec -it farmgear-mysql-test mysql -u testuser -p

# Redis容器
docker exec -it farmgear-redis-test redis-cli
```

#### 查看测试日志
```powershell
dotnet test --logger "console;verbosity=detailed"
```

## 持续集成

### GitHub Actions
```yaml
name: Test
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Start Docker containers
        run: docker-compose -f docker-compose.test.yml up -d
      - name: Wait for services
        run: |
          sleep 30
          docker exec farmgear-mysql-test mysqladmin ping -h localhost
          docker exec farmgear-redis-test redis-cli ping
      - name: Run tests
        run: dotnet test --verbosity normal
      - name: Cleanup
        run: docker-compose -f docker-compose.test.yml down -v
```

## 性能优化

### 容器优化
- 使用轻量级镜像
- 配置适当的内存限制
- 启用数据卷持久化

### 测试优化
- 并行运行测试
- 使用测试数据工厂
- 实现测试数据缓存

## 安全注意事项

### 测试环境安全
- 使用独立的测试数据库
- 避免使用生产环境凭据
- 定期清理测试数据

### 网络安全
- 测试容器不暴露到公网
- 使用内部网络通信
- 限制容器权限

## 维护

### 定期维护
- 更新Docker镜像
- 清理未使用的容器和镜像
- 检查磁盘空间使用

### 监控
- 监控容器资源使用
- 检查测试执行时间
- 分析测试失败原因 