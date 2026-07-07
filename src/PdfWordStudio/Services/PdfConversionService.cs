using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PdfWordStudio.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using PageSize = DocumentFormat.OpenXml.Wordprocessing.PageSize;

namespace PdfWordStudio.Services;

/// <summary>
/// PDF 转 Word 核心转换服务
/// 使用 PdfPig 解析 PDF + OpenXML 生成 Word 文档
/// 支持中文字符、智能段落检测、标题识别、列表检测
/// </summary>
public class PdfConversionService
{
    private readonly FileValidationService _validationService;
    private readonly OutputPathService _outputPathService;
    private readonly IProgress<ConversionProgressInfo>? _progress;

    public PdfConversionService(
        FileValidationService? validationService = null,
        OutputPathService? outputPathService = null,
        IProgress<ConversionProgressInfo>? progress = null)
    {
        _validationService = validationService ?? new FileValidationService();
        _outputPathService = outputPathService ?? new OutputPathService();
        _progress = progress;
    }

    /// <summary>
    /// 同步转换单个 PDF 文件到 Word
    /// </summary>
    public ConversionResult Convert(string sourceFilePath, string? outputPath = null)
    {
        var result = new ConversionResult
        {
            SourcePath = sourceFilePath,
            OutputPath = outputPath ?? _outputPathService.GetOutputPath(sourceFilePath)
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var (isValid, errorMsg) = _validationService.ValidateFile(sourceFilePath);
            if (!isValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = errorMsg;
                return result;
            }

            _outputPathService.EnsureOutputDirectory(result.OutputPath);
            _progress?.Report(new ConversionProgressInfo(5, "正在解析 PDF 文件..."));

            // 解析 PDF 页面块
            var blocks = ExtractPageBlocks(sourceFilePath, out int totalPages);
            result.PageCount = totalPages;

            if (blocks.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "未能从 PDF 中提取到可转换的文本内容。" +
                    "该 PDF 可能为扫描件（图片格式）或加密文件。\n\n" +
                    "建议：\n" +
                    "• 如果是扫描件，请使用 OCR 工具预处理\n" +
                    "• 检查 PDF 是否有密码保护\n" +
                    "• 尝试使用 Adobe Acrobat 导出";
                return result;
            }

            _progress?.Report(new ConversionProgressInfo(30, "正在分析文档结构..."));

            // 智能合并为段落
            var paragraphs = MergeToParagraphs(blocks);

            _progress?.Report(new ConversionProgressInfo(50, "正在生成 Word 文档..."));

            // 生成 Word
            CreateWordDocument(result.OutputPath, paragraphs);

            _progress?.Report(new ConversionProgressInfo(100, "转换完成"));

            stopwatch.Stop();
            result.IsSuccess = true;
            result.Duration = stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = $"转换失败：{ex.Message}";
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// 异步转换单个 PDF 文件到 Word
    /// </summary>
    public Task<ConversionResult> ConvertAsync(string sourceFilePath, string? outputPath = null)
    {
        return Task.Run(() => Convert(sourceFilePath, outputPath));
    }

    // ---------- PDF 文本提取 ----------

    /// <summary>
    /// 从 PDF 提取文本行
    /// </summary>
    private static List<TextBlock> ExtractPageBlocks(string filePath, out int totalPages)
    {
        var blocks = new List<TextBlock>();

        using var document = PdfDocument.Open(filePath);
        totalPages = document.NumberOfPages;

        foreach (var page in document.GetPages())
        {
            var pageText = page.Text;

            if (string.IsNullOrWhiteSpace(pageText))
            {
                // 空白页，加一个空行分隔
                if (blocks.Count > 0)
                    blocks.Add(new TextBlock { IsEmptyLine = true });
                continue;
            }

            // 按行拆分
            var lines = pageText.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    if (blocks.Count > 0 && !blocks[^1].IsEmptyLine)
                        blocks.Add(new TextBlock { IsEmptyLine = true });
                    continue;
                }

                // 判断是否居中（缩进比例）
                var leadingSpaces = line.Length - line.TrimStart().Length;
                var totalWidth = Math.Max(trimmed.Length, 1);

                blocks.Add(new TextBlock
                {
                    Text = trimmed,
                    PageNumber = page.Number,
                    IsCentered = leadingSpaces > totalWidth * 0.3,
                    IndentLevel = leadingSpaces / 2,
                    IsEmptyLine = false
                });
            }

            // 页面间的空行分隔
            if (blocks.Count > 0 && !blocks[^1].IsEmptyLine)
                blocks.Add(new TextBlock { IsEmptyLine = true });
        }

        // 清理连续空行
        var cleaned = new List<TextBlock>();
        bool lastWasEmpty = false;
        foreach (var block in blocks)
        {
            if (block.IsEmptyLine)
            {
                if (!lastWasEmpty && cleaned.Count > 0)
                    cleaned.Add(block);
                lastWasEmpty = true;
            }
            else
            {
                cleaned.Add(block);
                lastWasEmpty = false;
            }
        }

        return cleaned;
    }

    /// <summary>
    /// 智能合并文本行为段落
    /// </summary>
    private static List<PdfParagraph> MergeToParagraphs(List<TextBlock> blocks)
    {
        var paragraphs = new List<PdfParagraph>();
        var currentLines = new List<string>();
        double currentFontSize = 0;
        bool currentBold = false;

        foreach (var block in blocks)
        {
            if (block.IsEmptyLine)
            {
                // 空行结束当前段落
                FlushCurrentParagraph();
                continue;
            }

            if (currentLines.Count == 0)
            {
                currentLines.Add(block.Text);
                currentFontSize = block.FontSize;
                currentBold = block.IsBold;
                continue;
            }

            // 判断是否为新段落
            bool isNewParagraph = false;

            // 1. 字体大小变化 > 2pt
            if (Math.Abs(block.FontSize - currentFontSize) > 2 && block.FontSize > 0)
                isNewParagraph = true;

            // 2. 居中对齐行（可能是标题）
            if (block.IsCentered && currentLines.Count > 0)
                isNewParagraph = true;

            // 3. 缩进变化
            if (block.IndentLevel > 1 && currentLines.Count > 0)
                isNewParagraph = true;

            // 4. 短行 + 大写/数字开头（可能是标题或列表项）
            if (block.Text.Length < 50 && currentLines.Count > 0)
            {
                var firstChar = block.Text.TrimStart().FirstOrDefault();
                if (firstChar != '\0' && (char.IsUpper(firstChar) || char.IsDigit(firstChar) ||
                    firstChar == '第' || firstChar == '一' || firstChar == '二' || firstChar == '三' ||
                    firstChar == '●' || firstChar == '•' || firstChar == '-' || firstChar == '*' ||
                    firstChar == '■'))
                    isNewParagraph = true;
            }

            if (isNewParagraph)
            {
                FlushCurrentParagraph();
                currentLines.Add(block.Text);
                currentFontSize = block.FontSize;
                currentBold = block.IsBold;
            }
            else
            {
                currentLines.Add(block.Text);
            }
        }

        // 处理最后一段
        FlushCurrentParagraph();

        return paragraphs;

        void FlushCurrentParagraph()
        {
            if (currentLines.Count == 0) return;

            var text = string.Join("", currentLines);
            text = Regex.Replace(text, @"\s{2,}", " "); // 合并多余空格
            text = text.Trim();

            if (!string.IsNullOrWhiteSpace(text))
            {
                paragraphs.Add(new PdfParagraph
                {
                    Text = text,
                    FontSize = currentFontSize,
                    IsBold = currentBold
                });
            }

            currentLines.Clear();
            currentFontSize = 0;
            currentBold = false;
        }
    }

    // ---------- Word 文档生成 ----------

    /// <summary>
    /// 使用 OpenXML 创建格式丰富的 Word 文档
    /// </summary>
    private static void CreateWordDocument(string outputPath, List<PdfParagraph> paragraphs)
    {
        using var wordDocument = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);

        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // 页面设置 - A4
        body.AppendChild(new SectionProperties(
            new PageSize { Width = 11906, Height = 16838, Orient = PageOrientationValues.Portrait },
            new PageMargin
            {
                Top = 1134,
                Right = 1134,
                Bottom = 1134,
                Left = 1134,
                Header = 0,
                Footer = 0
            }
        ));

        // 文档标题（文件名）
        var fileName = Path.GetFileNameWithoutExtension(outputPath);
        body.AppendChild(CreateStyledParagraph(fileName, new RunProperties
        {
            Bold = new Bold(),
            FontSize = new FontSize { Val = "40" },
            Color = new Color { Val = "1F2937" },
            RunFonts = new RunFonts { Ascii = "微软雅黑", HighAnsi = "微软雅黑", EastAsia = "微软雅黑" }
        }, new ParagraphProperties
        {
            Justification = new Justification { Val = JustificationValues.Center },
            SpacingBetweenLines = new SpacingBetweenLines { After = "400" }
        }));

        // 分隔线
        body.AppendChild(CreateHorizontalRule());

        // 添加正文段落
        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph.Text)) continue;

            var text = paragraph.Text;

            // 判断是否标题
            bool isHeading = text.Length < 80 &&
                (AllCharsAreChinese(text.Take(20)) ||
                 HasTitlePattern(text));

            // 判断是否列表项
            bool isListItem = text.Length < 200 &&
                (text.StartsWith("•") || text.StartsWith("-") || text.StartsWith("●") ||
                 text.StartsWith("■") || Regex.IsMatch(text, @"^\d+[\.、)]") ||
                 Regex.IsMatch(text, @"^[①-⑩]"));

            // 判断是否代码块
            bool isCodeBlock = text.Length > 50 &&
                !AllCharsAreChinese(text) &&
                ContainsSpecialChars(text);

            ParagraphProperties paraProps = new ParagraphProperties();

            if (isHeading)
            {
                // 标题样式
                paraProps.Append(new Justification { Val = JustificationValues.Left });
                paraProps.Append(new SpacingBetweenLines { Before = "300", After = "200" });

                var runProps = new RunProperties
                {
                    Bold = new Bold(),
                    FontSize = new FontSize { Val = "32" },
                    Color = new Color { Val = "1E40AF" },
                    RunFonts = new RunFonts { Ascii = "微软雅黑", HighAnsi = "微软雅黑", EastAsia = "微软雅黑" }
                };

                body.AppendChild(CreateStyledParagraph(text, runProps, paraProps));
            }
            else if (isListItem)
            {
                // 列表项样式
                paraProps.Append(new Justification { Val = JustificationValues.Left });
                paraProps.Append(new Indentation { Left = "567", Hanging = "283" });
                paraProps.Append(new SpacingBetweenLines { Before = "60", After = "60" });

                var runProps = new RunProperties
                {
                    FontSize = new FontSize { Val = "24" },
                    RunFonts = new RunFonts { Ascii = "微软雅黑", HighAnsi = "微软雅黑", EastAsia = "微软雅黑" }
                };

                body.AppendChild(CreateStyledParagraph(text, runProps, paraProps));
            }
            else if (isCodeBlock)
            {
                // 代码块样式
                paraProps.Append(new Justification { Val = JustificationValues.Left });
                paraProps.Append(new Indentation { Left = "567" });
                paraProps.Append(new SpacingBetweenLines { Before = "60", After = "60" });

                var shd = new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F3F4F6" };
                paraProps.Append(shd);

                var runProps = new RunProperties
                {
                    FontSize = new FontSize { Val = "20" },
                    Color = new Color { Val = "374151" },
                    RunFonts = new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas", EastAsia = "微软雅黑" }
                };

                body.AppendChild(CreateStyledParagraph(text, runProps, paraProps));
            }
            else
            {
                // 正文样式 - 两端对齐 + 首行缩进
                paraProps.Append(new Justification { Val = JustificationValues.Both });
                paraProps.Append(new Indentation { FirstLine = "567" });
                paraProps.Append(new SpacingBetweenLines { After = "80", Line = "380", LineRule = LineSpacingRuleValues.Auto });

                var runProps = new RunProperties
                {
                    FontSize = new FontSize { Val = "22" },
                    RunFonts = new RunFonts { Ascii = "微软雅黑", HighAnsi = "微软雅黑", EastAsia = "微软雅黑" }
                };

                // 大号字体段落（可能是副标题）
                if (paragraph.FontSize > 16)
                {
                    runProps.FontSize = new FontSize { Val = "28" };
                }

                body.AppendChild(CreateStyledParagraph(text, runProps, paraProps));
            }
        }

        mainPart.Document.Save();
    }

    /// <summary>
    /// 创建格式化的段落
    /// </summary>
    private static Paragraph CreateStyledParagraph(string text, RunProperties runProps, ParagraphProperties? paraProps = null)
    {
        var para = new Paragraph();

        if (paraProps != null)
            para.AppendChild(paraProps);

        var run = new Run();
        run.AppendChild(runProps.CloneNode(true));

        // 处理特殊字符
        text = text.Replace("\r", "").Replace("\n", " ");
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        para.AppendChild(run);
        return para;
    }

    /// <summary>
    /// 创建分隔线
    /// </summary>
    private static Paragraph CreateHorizontalRule()
    {
        var para = new Paragraph();
        var paraProps = new ParagraphProperties
        {
            ParagraphBorders = new ParagraphBorders
            {
                BottomBorder = new BottomBorder
                {
                    Val = BorderValues.Single,
                    Color = "E5E7EB",
                    Size = 6,
                    Space = 1
                }
            },
            SpacingBetweenLines = new SpacingBetweenLines { After = "200" }
        };
        para.AppendChild(paraProps);
        return para;
    }

    // ---------- 辅助方法 ----------

    /// <summary>
    /// 判断字符串是否全为中文
    /// </summary>
    private static bool AllCharsAreChinese(IEnumerable<char> chars, int threshold = 5)
    {
        int count = 0;
        foreach (var c in chars)
        {
            var unicode = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicode == UnicodeCategory.OtherLetter)
                count++;
            else if (char.IsLetter(c))
                continue;
        }
        return count >= threshold;
    }

    /// <summary>
    /// 检测是否标题模式
    /// </summary>
    private static bool HasTitlePattern(string text)
    {
        if (text.Length > 100) return false;

        // 中文标题模式
        if (Regex.IsMatch(text, @"^第[一二三四五六七八九十百千]+[章节条]" + "$", RegexOptions.Multiline))
            return true;

        if (Regex.IsMatch(text, @"^[\d]+[\.、].{1,50}$"))
            return true;

        // 不包含句号、逗号的短句
        if (text.Length < 40 && !text.Contains("。") && !text.Contains("，"))
            return true;

        return false;
    }

    /// <summary>
    /// 检测是否包含代码特征
    /// </summary>
    private static bool ContainsSpecialChars(string text)
    {
        var special = new[] { '{', '}', '(', ')', ';', '<', '>', '=', '[', ']', '&', '|', '\\' };
        return text.Count(c => special.Contains(c)) > 4 ||
               text.Count(c => c == ' ') > 20;
    }
}

/// <summary>
/// 转换进度信息
/// </summary>
public record ConversionProgressInfo(int Percent, string Message);

/// <summary>
/// PDF 文本块（单行）
/// </summary>
internal class TextBlock
{
    public string Text { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public double FontSize { get; set; }
    public bool IsBold { get; set; }
    public bool IsCentered { get; set; }
    public int IndentLevel { get; set; }
    public (double x, double y, double width, double height) BoundingBox { get; set; }
    public bool IsEmptyLine { get; set; }
}

/// <summary>
/// PDF 段落数据（用于传递给 Word 生成）
/// </summary>
public class PdfParagraph
{
    public string Text { get; set; } = string.Empty;
    public double FontSize { get; set; }
    public bool IsBold { get; set; }
}
