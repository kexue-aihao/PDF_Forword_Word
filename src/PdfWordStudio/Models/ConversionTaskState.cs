namespace PdfWordStudio.Models;

/// <summary>
/// 单个转换任务的状态
/// </summary>
public enum ConversionTaskState
{
    /// <summary>等待转换</summary>
    Pending,

    /// <summary>正在转换</summary>
    Converting,

    /// <summary>转换成功</summary>
    Completed,

    /// <summary>转换失败</summary>
    Failed
}
