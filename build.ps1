# Moyu Windows 打包脚本
# 运行此脚本生成可执行文件

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

Write-Host "🐟 Moyu Windows 打包脚本" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# 检查 dotnet
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ 错误: 未找到 dotnet SDK，请先安装 .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

# 清理旧的发布文件
$publishDir = ".\publish"
if (Test-Path $publishDir) {
    Write-Host "🧹 清理旧的发布目录..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $publishDir
}

# 还原依赖
Write-Host "📦 还原依赖..." -ForegroundColor Yellow
dotnet restore

# 编译项目
Write-Host "🔨 编译项目..." -ForegroundColor Yellow
dotnet build -c $Configuration

# 发布单文件可执行文件
Write-Host "📦 打包为单文件可执行文件..." -ForegroundColor Yellow
dotnet publish -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishDir

# 复制数据库文件
Write-Host "📄 复制数据库文件..." -ForegroundColor Yellow
Copy-Item "moyu.db" "$publishDir\moyu.db"

# 显示结果
Write-Host ""
Write-Host "✅ 打包完成!" -ForegroundColor Green
Write-Host ""
Write-Host "输出目录: $publishDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "文件列表:" -ForegroundColor Cyan
Get-ChildItem $publishDir | Format-Table Name, @{Label="Size (MB)"; Expression={[math]::Round($_.Length/1MB, 2)}}

Write-Host ""
Write-Host "📋 使用说明:" -ForegroundColor Cyan
Write-Host "1. 将 publish 文件夹复制到目标电脑" -ForegroundColor White
Write-Host "2. 运行 Moyu.exe 即可使用" -ForegroundColor White
Write-Host ""
