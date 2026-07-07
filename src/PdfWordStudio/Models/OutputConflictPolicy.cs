namespace PdfWordStudio.Models;

/// <summary>
/// 输出文件冲突策略
/// </summary>
public enum OutputConflictPolicy
{
    /// <summary>询问用户（默认）</summary>
    Ask,

    /// <summary>覆盖</summary>
    Overwrite,

    /// <summary>自动重命名</summary>
    AutoRename,

    /// <summary>跳过</summary>
    Skip
}
