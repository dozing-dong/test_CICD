#!/usr/bin/env pwsh

<#
.SYNOPSIS
    本地代码覆盖率测试脚本
.DESCRIPTION
    运行测试并生成代码覆盖率报告，支持 HTML 和 XML 格式输出
.PARAMETER Clean
    清理之前的测试结果
.PARAMETER SkipInstall
    跳过工具安装步骤
.PARAMETER OpenReport
    生成报告后自动打开 HTML 报告
.EXAMPLE
    .\run-coverage.ps1
    .\run-coverage.ps1 -Clean -OpenReport
#>

param(
    [switch]$Clean,
    [switch]$SkipInstall,
    [switch]$OpenReport
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Info($message) {
    Write-ColorOutput Cyan "ℹ️  $message"
}

function Write-Success($message) {
    Write-ColorOutput Green "✅ $message"
}

function Write-Warning($message) {
    Write-ColorOutput Yellow "⚠️  $message"
}

function Write-Error($message) {
    Write-ColorOutput Red "❌ $message"
}

# 主函数
function Main {
    Write-Info "🚀 启动代码覆盖率测试..."
    
    # 检查是否在项目根目录
    if (-not (Test-Path "FarmGear_Application.sln")) {
        Write-Error "请在项目根目录运行此脚本"
        exit 1
    }
    
    # 清理旧的测试结果
    if ($Clean) {
        Write-Info "🧹 清理旧的测试结果..."
        Remove-ItemSafely "TestResults"
        Remove-ItemSafely "coverage-report"
        Remove-ItemSafely "FarmGear_Application.Tests/TestResults"
    }
    
    # 安装必要的工具
    if (-not $SkipInstall) {
        Install-Tools
    }
    
    # 恢复依赖
    Write-Info "📦 恢复项目依赖..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "依赖恢复失败"
        exit 1
    }
    
    # 构建项目
    Write-Info "🔨 构建项目..."
    dotnet build --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "项目构建失败"
        exit 1
    }
    
    # 运行测试并收集覆盖率
    Run-CoverageTests
    
    # 生成 HTML 报告
    Generate-HtmlReport
    
    # 显示覆盖率摘要
    Show-CoverageSummary
    
    # 打开报告
    if ($OpenReport) {
        Open-CoverageReport
    }
    
    Write-Success "🎉 代码覆盖率测试完成！"
}

function Remove-ItemSafely($path) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
        Write-Info "已删除: $path"
    }
}

function Install-Tools {
    Write-Info "🔧 检查并安装覆盖率工具..."
    
    # 检查 ReportGenerator
    $reportGen = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
    if (-not $reportGen) {
        Write-Info "安装 ReportGenerator..."
        dotnet tool install --global dotnet-reportgenerator-globaltool
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "ReportGenerator 安装失败，尝试更新..."
            dotnet tool update --global dotnet-reportgenerator-globaltool
        }
    } else {
        Write-Success "ReportGenerator 已安装"
    }
    
    # 检查 Coverlet
    $coverlet = Get-Command "coverlet" -ErrorAction SilentlyContinue
    if (-not $coverlet) {
        Write-Info "安装 Coverlet Console..."
        dotnet tool install --global coverlet.console
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Coverlet 安装失败，尝试更新..."
            dotnet tool update --global coverlet.console
        }
    } else {
        Write-Success "Coverlet 已安装"
    }
}

function Run-CoverageTests {
    Write-Info "🧪 运行测试并收集代码覆盖率..."
    
    # 创建输出目录
    New-Item -ItemType Directory -Force -Path "TestResults" | Out-Null
    
    # 运行测试
    $testCommand = @(
        "test"
        "--no-build"
        "--verbosity", "normal"
        "--logger", "trx;LogFileName=test-results.trx"
        "--results-directory", "./TestResults"
        "--collect:XPlat Code Coverage"
        "--settings", "coverlet.runsettings"
    )
    
    Write-Info "执行命令: dotnet $($testCommand -join ' ')"
    
    & dotnet @testCommand
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "测试运行有问题，但继续生成覆盖率报告..."
    } else {
        Write-Success "测试运行完成"
    }
    
    # 检查覆盖率文件
    $coverageFiles = Get-ChildItem -Path "TestResults" -Filter "*.xml" -Recurse
    if ($coverageFiles.Count -eq 0) {
        Write-Error "未找到覆盖率文件！"
        exit 1
    }
    
    Write-Success "找到 $($coverageFiles.Count) 个覆盖率文件"
    $coverageFiles | ForEach-Object { Write-Info "  - $($_.FullName)" }
}

function Generate-HtmlReport {
    Write-Info "📊 生成 HTML 覆盖率报告..."
    
    # 查找所有覆盖率 XML 文件
    $xmlFiles = Get-ChildItem -Path "TestResults" -Filter "*.xml" -Recurse | Where-Object { $_.Name -match "coverage|cobertura" }
    
    if ($xmlFiles.Count -eq 0) {
        # 如果没找到特定的覆盖率文件，使用所有 XML 文件
        $xmlFiles = Get-ChildItem -Path "TestResults" -Filter "*.xml" -Recurse
    }
    
    if ($xmlFiles.Count -eq 0) {
        Write-Error "未找到可用的覆盖率 XML 文件"
        return
    }
    
    $reportTypes = @(
        "Html",
        "HtmlSummary", 
        "JsonSummary",
        "Cobertura",
        "TextSummary"
    )
    
    $reportCommand = @(
        "-reports:$($xmlFiles.FullName -join ';')"
        "-targetdir:coverage-report"
        "-reporttypes:$($reportTypes -join ';')"
        "-verbosity:Info"
        "-title:FarmGear Application Coverage Report"
    )
    
    Write-Info "执行命令: reportgenerator $($reportCommand -join ' ')"
    
    & reportgenerator @reportCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "HTML 报告生成成功: coverage-report/index.html"
    } else {
        Write-Error "HTML 报告生成失败"
    }
}

function Show-CoverageSummary {
    Write-Info "📈 覆盖率摘要:"
    
    # 尝试读取 JSON 摘要
    $summaryFile = "coverage-report/Summary.json"
    if (Test-Path $summaryFile) {
        try {
            $summary = Get-Content $summaryFile | ConvertFrom-Json
            $coverage = $summary.summary
            
            Write-Host ""
            Write-ColorOutput Green "┌─────────────────────────────────────┐"
            Write-ColorOutput Green "│          📊 覆盖率报告              │"
            Write-ColorOutput Green "├─────────────────────────────────────┤"
            Write-ColorOutput Cyan   "│ 行覆盖率     : $($coverage.linecoverage)%".PadRight(35) + "│"
            Write-ColorOutput Cyan   "│ 分支覆盖率   : $($coverage.branchcoverage)%".PadRight(35) + "│"
            Write-ColorOutput Cyan   "│ 方法覆盖率   : $($coverage.methodcoverage)%".PadRight(35) + "│"
            Write-ColorOutput Green "└─────────────────────────────────────┘"
            Write-Host ""
            
            # 评估覆盖率质量
            $lineRate = [double]$coverage.linecoverage
            if ($lineRate -ge 80) {
                Write-ColorOutput Green "🎉 覆盖率优秀 (≥80%)"
            } elseif ($lineRate -ge 60) {
                Write-ColorOutput Yellow "⚠️  覆盖率良好 (≥60%)"
            } else {
                Write-ColorOutput Red "❌ 覆盖率需要改进 (<60%)"
            }
            
        } catch {
            Write-Warning "无法解析覆盖率摘要文件"
        }
    } else {
        Write-Warning "未找到覆盖率摘要文件"
    }
    
    # 显示文本摘要（如果存在）
    $textSummaryFile = "coverage-report/Summary.txt"
    if (Test-Path $textSummaryFile) {
        Write-Host ""
        Write-Info "详细覆盖率信息:"
        Get-Content $textSummaryFile | Write-Host
    }
}

function Open-CoverageReport {
    $reportFile = "coverage-report/index.html"
    if (Test-Path $reportFile) {
        Write-Info "🌐 打开覆盖率报告..."
        
        if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
            Start-Process $reportFile
        } elseif ($IsMacOS) {
            & open $reportFile
        } elseif ($IsLinux) {
            & xdg-open $reportFile
        }
    } else {
        Write-Warning "覆盖率报告文件未找到: $reportFile"
    }
}

# 运行主函数
try {
    Main
} catch {
    Write-Error "脚本执行失败: $($_.Exception.Message)"
    exit 1
} 