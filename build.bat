@echo off
chcp 65001 >nul
echo.
echo ========================================
echo   🐟 Moyu Windows 打包工具
echo ========================================
echo.

:: 检查 dotnet
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo ❌ 错误: 未找到 dotnet SDK
    echo    请先安装 .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [1/4] 清理旧文件...
if exist "publish" rmdir /s /q "publish"
if exist "installer" rmdir /s /q "installer"

echo [2/4] 还原依赖...
dotnet restore

echo [3/4] 编译项目...
dotnet build -c Release

echo [4/4] 打包为单文件可执行文件...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

echo.
echo ✅ 打包完成!
echo.
echo 输出文件:
dir /b publish
echo.
echo 📁 文件位于: %cd%\publish
echo.
echo ⚠️  注意: 需要将 moyu.db 与 Moyu.exe 放在同一目录下运行
echo.

:: 检查是否有 Inno Setup
where iscc >nul 2>nul
if %errorlevel% equ 0 (
    echo.
    echo 检测到 Inno Setup，正在生成安装程序...
    mkdir installer 2>nul
    iscc setup.iss
    echo.
    echo ✅ 安装程序已生成: installer\MoyuSetup_1.0.0.exe
) else (
    echo.
    echo 💡 提示: 如需生成安装程序，请安装 Inno Setup:
    echo    https://jrsoftware.org/isdl.php
    echo    安装后运行: iscc setup.iss
)

echo.
pause
