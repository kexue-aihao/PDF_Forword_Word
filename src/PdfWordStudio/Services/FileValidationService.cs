using System.IO;
using System.Text;
using PdfWordStudio.Models;

namespace PdfWordStudio.Services;

/// <summary>
/// 文件验证服务 - 防呆设计核心组件
/// </summary>
public class FileValidationService
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf"];
    private const long MaxFileSize = 500 * 1024 * 1024; // 500 MB

    /// <summary>
    /// 验证单个文件是否为有效的 PDF
    /// </summary>
    public (bool IsValid, string? ErrorMessage) ValidateFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return (false, "文件路径不能为空。");

        if (!File.Exists(filePath))
            return (false, $"文件不存在：{filePath}");

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (false, $"不支持的文件格式 \"{extension}\"，请选择 PDF 文件。");

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
            return (false, $"文件 \"{fileInfo.Name}\" 是空文件，无法转换。");

        if (fileInfo.Length > MaxFileSize)
            return (false, $"文件 \"{fileInfo.Name}\" 超过 500MB 大小限制，请选择较小的文件。");

        // 尝试验证 PDF 头
        try
        {
            using var stream = File.OpenRead(filePath);
            var header = new byte[5];
            if (stream.Read(header, 0, 5) < 5)
                return (false, $"文件 \"{fileInfo.Name}\" 格式不正确（文件过小）。");

            var headerStr = System.Text.Encoding.ASCII.GetString(header);
            if (headerStr != "%PDF-")
                return (false, $"文件 \"{fileInfo.Name}\" 不是有效的 PDF 格式。");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, $"没有权限读取文件 \"{fileInfo.Name}\"，请检查文件权限。");
        }
        catch (IOException ex)
        {
            return (false, $"无法读取文件 \"{fileInfo.Name}\"：{ex.Message}");
        }

        return (true, null);
    }

    /// <summary>
    /// 批量验证文件，返回有效列表和错误消息
    /// </summary>
    public List<string> ValidateFiles(IEnumerable<string> filePaths, out List<string> errors)
    {
        errors = [];
        var validFiles = new List<string>();

        foreach (var filePath in filePaths)
        {
            var (isValid, errorMessage) = ValidateFile(filePath);
            if (isValid)
                validFiles.Add(filePath);
            else
                errors.Add(errorMessage ?? $"文件 \"{Path.GetFileName(filePath)}\" 验证失败。");
        }

        return validFiles;
    }

    /// <summary>
    /// 检查输出文件是否存在冲突
    /// </summary>
    public bool HasConflict(string outputPath)
    {
        return File.Exists(outputPath);
    }

    /// <summary>
    /// 根据策略处理冲突
    /// </summary>
    public string ResolveConflict(string outputPath, OutputConflictPolicy policy)
    {
        if (!File.Exists(outputPath))
            return outputPath;

        return policy switch
        {
            OutputConflictPolicy.Overwrite => outputPath,
            OutputConflictPolicy.AutoRename => GenerateUniquePath(outputPath),
            OutputConflictPolicy.Skip => outputPath, // 调用者需检查
            _ => outputPath
        };
    }

    private static string GenerateUniquePath(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);
        var counter = 1;

        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{name} ({counter}){ext}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }
}
