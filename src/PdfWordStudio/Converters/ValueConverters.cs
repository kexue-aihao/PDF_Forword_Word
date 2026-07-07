using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PdfWordStudio.Models;

namespace PdfWordStudio.Converters;

/// <summary>
/// 任务状态 → 图标转换器
/// </summary>
public class TaskStateToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConversionTaskState state)
        {
            return state switch
            {
                ConversionTaskState.Pending => "⏳",
                ConversionTaskState.Converting => "🔄",
                ConversionTaskState.Completed => "✅",
                ConversionTaskState.Failed => "❌",
                _ => "⏳"
            };
        }
        return "⏳";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// 任务状态 → 背景色转换器
/// </summary>
public class TaskStateToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConversionTaskState state)
        {
            return state switch
            {
                ConversionTaskState.Pending => new SolidColorBrush(Color.FromRgb(156, 163, 175)), // Gray
                ConversionTaskState.Converting => new SolidColorBrush(Color.FromRgb(59, 130, 246)), // Blue
                ConversionTaskState.Completed => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Green
                ConversionTaskState.Failed => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red
                _ => new SolidColorBrush(Color.FromRgb(156, 163, 175))
            };
        }
        return new SolidColorBrush(Color.FromRgb(156, 163, 175));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// 布尔值 → 可见性转换器
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>
/// 布尔值取反转换器（用于 Collapsed/Visible 切换）
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility v && v != Visibility.Visible;
}
