# 测试运行脚本
# 启动Docker容器并运行集成测试

Write-Host "Starting test environment..." -ForegroundColor Green

# 停止并删除可能存在的容器
Write-Host "Cleaning up existing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.test.yml down -v

# 启动测试容器
Write-Host "Starting test containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.test.yml up -d

# 等待MySQL容器启动
Write-Host "Waiting for MySQL to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$mysqlReady = $false

while ($attempt -lt $maxAttempts -and -not $mysqlReady) {
    $attempt++
    Write-Host "Attempt $attempt/$maxAttempts - Checking MySQL connection..."
    
    try {
        $result = docker exec farmgear-mysql-test mysqladmin ping -h localhost --silent
        if ($LASTEXITCODE -eq 0) {
            $mysqlReady = $true
            Write-Host "MySQL is ready!" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "MySQL not ready yet, waiting..." -ForegroundColor Yellow
    }
    
    if (-not $mysqlReady) {
        Start-Sleep -Seconds 2
    }
}

if (-not $mysqlReady) {
    Write-Host "MySQL failed to start within expected time" -ForegroundColor Red
    docker-compose -f docker-compose.test.yml down -v
    exit 1
}

# 等待Redis容器启动
Write-Host "Waiting for Redis to be ready..." -ForegroundColor Yellow
$redisReady = $false
$attempt = 0

while ($attempt -lt $maxAttempts -and -not $redisReady) {
    $attempt++
    Write-Host "Attempt $attempt/$maxAttempts - Checking Redis connection..."
    
    try {
        $result = docker exec farmgear-redis-test redis-cli ping
        if ($result -eq "PONG") {
            $redisReady = $true
            Write-Host "Redis is ready!" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "Redis not ready yet, waiting..." -ForegroundColor Yellow
    }
    
    if (-not $redisReady) {
        Start-Sleep -Seconds 2
    }
}

if (-not $redisReady) {
    Write-Host "Redis failed to start within expected time" -ForegroundColor Red
    docker-compose -f docker-compose.test.yml down -v
    exit 1
}

# 运行测试
Write-Host "Running tests..." -ForegroundColor Green
cd FarmGear_Application.Tests
dotnet test --verbosity normal

# 保存测试结果
$testExitCode = $LASTEXITCODE

# 清理容器
Write-Host "Cleaning up test containers..." -ForegroundColor Yellow
cd ..
docker-compose -f docker-compose.test.yml down -v

# 输出测试结果
if ($testExitCode -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
} else {
    Write-Host "Some tests failed!" -ForegroundColor Red
}

exit $testExitCode 