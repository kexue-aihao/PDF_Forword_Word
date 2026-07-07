@echo off
chcp 65001 >nul
echo ========================================
echo   PdfWordStudio - 构建脚本
echo ========================================
echo.

:: 设置 .NET SDK 版本
set DOTNET_VERSION=10.0.301

:: 清理
echo [1/4] 清理旧构建...
if exist build rmdir /s /q build >nul 2>&1

:: 还原
echo [2/4] 还原 NuGet 包...
dotnet restore

:: 构建 Release
echo [3/4] 构建 Release 版本...
dotnet build -c Release --no-restore

:: 发布 x64
echo [4a/4] 发布 x64 版本...
dotnet publish src/PdfWordStudio/PdfWordStudio.csproj -c Release -r win-x64 --self-contained false -o build/win-x64 --no-build

:: 发布 x86
echo [4b/4] 发布 x86 版本...
dotnet publish src/PdfWordStudio/PdfWordStudio.csproj -c Release -r win-x86 --self-contained false -o build/win-x86 --no-build

echo.
echo ========================================
echo   构建完成！
echo   x64: build\win-x64\PdfWordStudio.exe
echo   x86: build\win-x86\PdfWordStudio.exe
echo ========================================
pause
