using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfWordStudio.Models;
using PdfWordStudio.Services;

namespace PdfWordStudio.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PdfConversionService _conversionService;
    private readonly FileValidationService _validationService;
    private readonly OutputPathService _outputPathService;

    public MainWindowViewModel()
    {
        _validationService = new FileValidationService();
        _outputPathService = new OutputPathService();
        _conversionService = new PdfConversionService(_validationService, _outputPathService);

        ConversionTasks = [];
    }

    // ==================== 可观察属性 ====================

    /// <summary>转换任务列表</summary>
    public ObservableCollection<ConversionTaskItemViewModel> ConversionTasks { get; }

    [ObservableProperty]
    private bool _hasFiles;

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private bool _isDragOver;

    [ObservableProperty]
    private int _totalFileCount;

    [ObservableProperty]
    private string _statusText = "拖拽 PDF 文件到此处，或点击「添加文件」按钮";

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private string? _lastErrorMessage;

    [ObservableProperty]
    private int _successCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private bool _useCustomOutputDir;

    // ==================== 命令 ====================

    /// <summary>添加文件命令</summary>
    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择 PDF 文件",
            Filter = "PDF 文件 (*.pdf)|*.pdf|所有文件 (*.*)|*.*",
            Multiselect = true,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() == true)
        {
            AddFilePaths(dialog.FileNames);
        }
    }

    /// <summary>添加文件夹命令</summary>
    [RelayCommand]
    private void AddFolder()
    {
        // 使用 OpenFileDialog 模拟文件夹选择（用户可以导航到目标文件夹后点确定）
        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "请导航到目标文件夹后取消（或选择文件夹内的一个文件）",
            CheckFileExists = false,
            CheckPathExists = true,
            RestoreDirectory = true
        };

        if (fileDialog.ShowDialog() == true)
        {
            var dir = Path.GetDirectoryName(fileDialog.FileName);
            if (!string.IsNullOrEmpty(dir))
            {
                // 扫描该目录下的所有 PDF 文件
                var pdfFiles = Directory.GetFiles(dir, "*.pdf", SearchOption.TopDirectoryOnly);
                if (pdfFiles.Length > 0)
                {
                    AddFilePaths(pdfFiles);
                }
                else
                {
                    _ = MessageBox.Show("该文件夹中没有找到 PDF 文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }

    /// <summary>从拖拽添加文件</summary>
    public void OnFilesDropped(string[] filePaths)
    {
        AddFilePaths(filePaths);
    }

    /// <summary>移除选中文件</summary>
    [RelayCommand]
    private void RemoveSelected()
    {
        var toRemove = ConversionTasks.Where(t => t.IsSelected).ToList();
        foreach (var task in toRemove)
        {
            ConversionTasks.Remove(task);
        }
        UpdateStatus();
    }

    /// <summary>清空列表</summary>
    [RelayCommand]
    private void ClearAll()
    {
        if (ConversionTasks.Count == 0) return;

        var result = MessageBox.Show(
            "确定要清空所有文件吗？",
            "确认清空",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ConversionTasks.Clear();
            UpdateStatus();
        }
    }

    /// <summary>选择输出目录</summary>
    [RelayCommand]
    private void SelectOutputDirectory()
    {
        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择输出目录（导航到目标文件夹后，选择任意一个文件或直接取消）",
            Filter = "所有文件 (*.*)|*.*",
            CheckFileExists = false,
            CheckPathExists = true,
            RestoreDirectory = true
        };

        if (fileDialog.ShowDialog() == true)
        {
            var dir = Path.GetDirectoryName(fileDialog.FileName);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                OutputDirectory = dir;
                _outputPathService.CustomOutputDirectory = dir;
                UseCustomOutputDir = true;
                UpdateStatus();
            }
        }
        else
        {
            // 用户取消 - 不做任何操作
        }
    }

    /// <summary>重置输出目录（使用源文件目录）</summary>
    [RelayCommand]
    private void ResetOutputDirectory()
    {
        OutputDirectory = string.Empty;
        _outputPathService.CustomOutputDirectory = null;
        UseCustomOutputDir = false;
        UpdateStatus();
    }

    /// <summary>开始转换</summary>
    [RelayCommand]
    private async Task StartConversion()
    {
        if (IsConverting) return;
        if (ConversionTasks.Count == 0)
        {
            _ = MessageBox.Show("请先添加需要转换的 PDF 文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsConverting = true;
        SuccessCount = 0;
        FailedCount = 0;
        OverallProgress = 0;

        StatusText = "正在转换...";
        LastErrorMessage = null;

        var tasks = ConversionTasks.ToList();
        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            if (task.State == ConversionTaskState.Completed)
            {
                SuccessCount++;
                continue;
            }

            task.State = ConversionTaskState.Converting;
            task.Progress = 0;
            task.StatusMessage = "正在转换...";

            var result = await _conversionService.ConvertAsync(task.SourceFilePath);

            if (result.IsSuccess)
            {
                task.State = ConversionTaskState.Completed;
                task.Progress = 100;
                task.StatusMessage = "转换完成";
                task.OutputPath = result.OutputPath;
                SuccessCount++;
            }
            else
            {
                task.State = ConversionTaskState.Failed;
                task.StatusMessage = $"失败：{result.ErrorMessage}";
                task.Progress = 0;
                FailedCount++;
                LastErrorMessage = result.ErrorMessage;
            }

            OverallProgress = (double)(i + 1) / tasks.Count * 100;
        }

        IsConverting = false;
        OverallProgress = 100;

        // 转换结束提示
        var summary = $"转换完成！\n成功：{SuccessCount} 个\n失败：{FailedCount} 个";
        StatusText = summary;

        if (FailedCount == 0)
        {
            _ = MessageBox.Show(summary, "转换完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            _ = MessageBox.Show(summary, "转换完成（部分失败）", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ==================== 辅助方法 ====================

    private void AddFilePaths(string[] filePaths)
    {
        var validFiles = _validationService.ValidateFiles(filePaths, out var errors);

        foreach (var file in validFiles)
        {
            // 检查是否已存在
            if (ConversionTasks.Any(t => t.SourceFilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                continue;

            var outputPath = _outputPathService.GetOutputPath(file);
            var task = new ConversionTaskItemViewModel
            {
                SourceFileName = Path.GetFileName(file),
                SourceFilePath = file,
                OutputPath = outputPath,
                State = ConversionTaskState.Pending,
                StatusMessage = "等待转换",
                IsSelected = false
            };
            ConversionTasks.Add(task);
        }

        // 显示错误
        if (errors.Count > 0)
        {
            var errorMsg = string.Join("\n", errors.Take(5));
            if (errors.Count > 5)
                errorMsg += $"\n...及其他 {errors.Count - 5} 个错误";

            _ = MessageBox.Show(errorMsg, "文件验证提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // 状态更新
        if (validFiles.Count == 0 && errors.Count > 0)
        {
            StatusText = "添加的文件均无效，请检查文件格式";
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        HasFiles = ConversionTasks.Count > 0;
        TotalFileCount = ConversionTasks.Count;

        if (ConversionTasks.Count == 0)
        {
            StatusText = "拖拽 PDF 文件到此处，或点击「添加文件」按钮";
        }
        else
        {
            var pendingCount = ConversionTasks.Count(t => t.State is ConversionTaskState.Pending or ConversionTaskState.Converting);
            StatusText = $"共 {ConversionTasks.Count} 个文件，{SuccessCount} 成功，{FailedCount} 失败，{pendingCount} 待处理";
        }
    }
}
