using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using WinUI3Utilities;

namespace NameAnalyzer;

public sealed partial class MainWindow : Window
{
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
        if (await PickerHelper.PickSingleFileAsync() is { } file)
            _vm.Analyzer = new(file.Path, _logDir);
    }

    private async void SelectFolderTapped(object sender, TappedRoutedEventArgs e)
    {
        if (await PickerHelper.PickSingleFolderAsync() is { } folder)
            _vm.Analyzer = new(folder.Path, _logDir);
    }
}
