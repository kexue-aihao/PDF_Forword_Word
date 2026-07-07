using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfWordStudio.Models;

namespace PdfWordStudio.ViewModels;

/// <summary>
/// 单个转换任务项 ViewModel
/// </summary>
public partial class ConversionTaskItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _sourceFileName = string.Empty;

    [ObservableProperty]
    private string _sourceFilePath = string.Empty;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private ConversionTaskState _state = ConversionTaskState.Pending;

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private string _statusMessage = "等待转换";

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 文件大小（格式化后）
    /// </summary>
    public string FileSize => GetFileSize();

    private string GetFileSize()
    {
        try
        {
            var fileInfo = new FileInfo(SourceFilePath);
            return fileInfo.Length switch
            {
                < 1024 => $"{fileInfo.Length} B",
                < 1024 * 1024 => $"{fileInfo.Length / 1024.0:F1} KB",
                _ => $"{fileInfo.Length / (1024.0 * 1024.0):F1} MB"
            };
        }
        catch
        {
            return "未知";
        }
    }
}
