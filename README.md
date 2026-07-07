# 📄 PDF 转 Word

<p align="center">
  <img src="src/PdfWordStudio/Resources/app_logo.png" width="128" height="128" alt="PDF 转 Word Logo">
</p>

<h2 align="center">现代化 PDF → Word 文档转换工具</h2>

<p align="center">
  <strong>Windows 10/11 · x86/x64 · 中文界面 · 自包含运行</strong>
</p>

<p align="center">
  <a href="#-功能特点">功能特点</a> ·
  <a href="#-快速开始">快速开始</a> ·
  <a href="#-技术架构">技术架构</a> ·
  <a href="#-本地构建">本地构建</a> ·
  <a href="#-下载安装">下载安装</a>
</p>  
<br>
采用 MVVM 架构，提供流畅美观的中文操作界面。

---

## ✨ 功能特点

- **🎯 一键转换** - 拖拽或点击添加 PDF 文件，一键转成 Word 文档
- **📚 批量处理** - 支持同时添加多个 PDF 文件，批量转换
- **📱 拖拽操作** - 支持文件拖拽输入，操作直观便捷
- **🎨 现代 UI** - Fluent Design 风格界面，圆角设计，柔和的配色方案
- **🛡️ 防呆设计** - 文件格式验证、大小检查、错误处理、冲突提示
- **📊 实时进度** - 实时显示转换进度、状态和统计信息
- **📂 独立输出** - 支持自定义输出目录，文件冲突自动重命名
- **🌐 中文环境** - 全中文界面，支持中文字体渲染

## 🚀 快速开始

### 下载安装

1. 从 [Releases](https://github.com/kexue-aihao/pdf-word-converter/releases) 下载最新安装包
2. 运行安装程序，按向导完成安装
3. 启动「PDF 转 Word」

### 使用方法

1. **添加文件**：拖拽 PDF 文件到窗口，或点击「添加文件」按钮
2. **设置输出**（可选）：点击「选择目录」自定义输出位置
3. **开始转换**：点击「开始转换」按钮
4. **查看结果**：转换完成的文件会自动生成 `.docx` 文件

## 🛠️ 技术架构

```
PdfWordStudio/
├── Models/              # 数据模型
│   ├── ConversionDirection.cs
│   ├── ConversionResult.cs
│   ├── ConversionTaskState.cs
│   └── OutputConflictPolicy.cs
├── ViewModels/          # 视图模型 (MVVM)
│   ├── ViewModelBase.cs
│   ├── MainWindowViewModel.cs
│   └── ConversionTaskItemViewModel.cs
├── Views/               # 视图层
│   └── MainWindow.xaml
├── Services/            # 业务服务
│   ├── PdfConversionService.cs   # PDF→Word 核心转换
│   ├── FileValidationService.cs  # 文件验证
│   └── OutputPathService.cs      # 输出路径管理
├── Converters/          # 值转换器
│   └── ValueConverters.cs
├── Resources/           # 样式资源
│   └── Theme.xaml
└── App.xaml             # 应用入口
```

## 📋 系统要求

- **操作系统**：Windows 10 (1809+) / Windows 11
- **架构**：x86 / x64
- **运行时**：.NET 10.0 Runtime（如使用非自包含版本）
- **依赖**：无需 Microsoft Office

## 🏗️ 本地构建

### 前置要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 或更高版本

### 构建命令

```bash
# 还原
dotnet restore

# 构建
dotnet build -c Release

# 发布 x64
dotnet publish src/PdfWordStudio/PdfWordStudio.csproj -c Release -r win-x64 -o build/win-x64

# 发布 x86
dotnet publish src/PdfWordStudio/PdfWordStudio.csproj -c Release -r win-x86 -o build/win-x86
```

或直接运行 `build.bat` 一键构建。

## 📦 制作安装包

使用 [Inno Setup](https://jrsoftware.org/isinfo.php) 编译安装脚本：

```bash
iscc installer/PdfWordStudio.iss
```

安装包将输出到 `installer/Output/` 目录。

## 🔧 核心 NuGet 包

| 包名 | 用途 | 版本 |
|---|---|---|
| PdfPig | PDF 文本提取 | 0.1.15 |
| DocumentFormat.OpenXml | Word 文档生成 | 3.5.1 |
| CommunityToolkit.Mvvm | MVVM 数据绑定 | 8.4.2 |

## 📄 开源协议

Copyright © 2026 PdfWordStudio. MIT License.
