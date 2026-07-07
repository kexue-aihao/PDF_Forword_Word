namespace PdfWordStudio.Models;

/// <summary>
/// 转换结果
/// </summary>
public class ConversionResult
{
    /// <summary>源文件路径</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>输出文件路径</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>错误信息（如有）</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>转换耗时</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>处理的页数</summary>
    public int PageCount { get; set; }
}
