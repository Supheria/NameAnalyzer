using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ABI.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using WinUI3Utilities;

namespace NameAnalyzer;

[INotifyPropertyChanged]
public sealed partial class MainWindow : Window
{
    [ObservableProperty] private Visibility _exceptionVisibility = Visibility.Collapsed;

    public MainWindow()
    {
        CurrentContext.Window = this;
        InitializeComponent();
        CurrentContext.TitleBar = TitleBar;
        CurrentContext.TitleTextBlock = TitleTextBlock;
    }

    private readonly MainViewModel _vm = new();

    private readonly string _logDir = ApplicationData.Current.LocalCacheFolder.Path;

    private async void SelectFileTapped(object sender, TappedRoutedEventArgs e)
    {
        if (await PickerHelper.PickSingleFileAsync() is not { } file) 
            return;
        _vm.Analyzer = new(file.Path, _logDir);
        TryShowExceptions();
    }

    private async void SelectFolderTapped(object sender, TappedRoutedEventArgs e)
    {
        if (await PickerHelper.PickSingleFolderAsync() is not { } folder)
            return;
        _vm.Analyzer = new(folder.Path, _logDir);
        TryShowExceptions();
    }

    private void OpenExceptionLogTapped(object sender, TappedRoutedEventArgs e)
    {
        OpenFileInExplorer(_vm.Analyzer.ExceptionLogPath);
    }
    private async void TryShowExceptions()
    {
        ExceptionVisibility = Visibility.Collapsed;
        if (!File.Exists(_vm.Analyzer.ExceptionLogPath))
            return;
        StringBuilder sb = new();
        using var reader = new StreamReader(_vm.Analyzer.ExceptionLogPath);
        var line = await reader.ReadLineAsync();
        while (line != null)
        {
            sb.Append($"{line}\n");
            line = await reader.ReadLineAsync();
        }
        if (sb.Length == 0)
            return;

        ExceptionVisibility = Visibility.Visible;
        ContentDialog exception = new()
        {
            XamlRoot = this.Content.XamlRoot,
            CanBeScrollAnchor = true,
            Title = "Parse Occurs Exception",
            Content = sb.ToString(),
            PrimaryButtonText = "Open exception log",
            CloseButtonText = "Close"
        };
        switch (await exception.ShowAsync())
        {
            case ContentDialogResult.Primary:
                OpenFileInExplorer(_vm.Analyzer.ExceptionLogPath);
                break;
            case ContentDialogResult.None:
                break;
            case ContentDialogResult.Secondary:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void OpenFileInExplorer(string filePath)
    {
        using Process pc = new();
        pc.StartInfo.FileName = "cmd.exe";

        pc.StartInfo.CreateNoWindow = true;
        pc.StartInfo.RedirectStandardError = true;

        pc.StartInfo.RedirectStandardInput = true;
        pc.StartInfo.RedirectStandardOutput = true;
        pc.StartInfo.UseShellExecute = false;
        pc.Start();

        pc.StandardInput.WriteLine("explorer " + $"\"{filePath}\"");
        pc.StandardInput.WriteLine("exit");
        pc.StandardInput.AutoFlush = true;

        pc.WaitForExit();
        pc.Close();
    }
}
