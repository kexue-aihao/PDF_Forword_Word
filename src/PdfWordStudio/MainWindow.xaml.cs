using System.Windows;
using System.Windows.Input;
using PdfWordStudio.ViewModels;

namespace PdfWordStudio;

/// <summary>
/// 主窗口 - 现代中文界面 PDF 转 Word 工具
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // ==================== 窗口控制 ====================

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ==================== 拖拽支持 ====================

    /// <summary>
    /// 拖拽进入 - 视觉反馈
    /// </summary>
    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            if (DataContext is MainWindowViewModel vm)
                vm.IsDragOver = true;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    /// <summary>
    /// 拖拽离开 - 恢复状态
    /// </summary>
    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsDragOver = false;
        e.Handled = true;
    }

    /// <summary>
    /// 拖拽放下 - 添加文件
    /// </summary>
    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (DataContext is MainWindowViewModel vm)
            {
                vm.IsDragOver = false;
                vm.OnFilesDropped(files);
            }
        }
        e.Handled = true;
    }
}
