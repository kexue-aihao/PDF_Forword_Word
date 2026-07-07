using System.IO;

namespace PdfWordStudio.Services;

/// <summary>
/// 输出路径服务 - 自动管理输出文件名和目录
/// </summary>
public class OutputPathService
{
    private string? _customOutputDirectory;

    /// <summary>
    /// 自定义输出目录（若未设置则使用源文件目录）
    /// </summary>
    public string? CustomOutputDirectory
    {
        get => _customOutputDirectory;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
                _customOutputDirectory = value;
            else
                _customOutputDirectory = null;
        }
    }

    /// <summary>
    /// 为源文件生成输出路径
    /// </summary>
    public string GetOutputPath(string sourceFilePath)
    {
        var inputDir = Path.GetDirectoryName(sourceFilePath) ?? ".";
        var outputDir = _customOutputDirectory ?? inputDir;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);
        return Path.Combine(outputDir, $"{fileNameWithoutExt}.docx");
    }

    /// <summary>
    /// 确保输出目录存在
    /// </summary>
    public void EnsureOutputDirectory(string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}
