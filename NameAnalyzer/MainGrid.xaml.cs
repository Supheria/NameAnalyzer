using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Parser.Helper;
using Utilities;
using Windows.Storage;
using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace NameAnalyzer;

public sealed partial class MainGrid : Grid
{
    public MainGrid()
    {
        InitializeComponent();
        CurrentContext.TitleBar = TitleBar;
        CurrentContext.TitleTextBlock = TitleTextBlock;
        _vm.NameInfoBlockSource = NameInfoBlock.Inlines;
    }
    private readonly MainViewModel _vm = new();

    private readonly string _parsingLogDir = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "hoi4 script parse exception.txt");
    private readonly string _warningLogDir = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "hoi4 script parse exception.txt");

    private async void SelectFileTapped(object sender, TappedRoutedEventArgs e)
    {
        if (await PickerHelper.PickSingleFileAsync() is not { } file)
            return;
        Analysis.Parse(file.Path, out var map, out var errorLog);
        _vm.LevelMap = map;
        TryShowExceptions(errorLog);
    }

    private async void SelectFolderTapped(object sender, TappedRoutedEventArgs e)
    {
        if (await PickerHelper.PickSingleFolderAsync() is not { } folder)
            return;
        Analysis.Parse(folder.Path, out var map, out var errorLog);
        _vm.LevelMap = map;
        TryShowExceptions(errorLog);
    }

    private async void TryShowExceptions(string errorLog)
    {
        _vm.MessageDialogText = errorLog;
        await File.WriteAllTextAsync(_parsingLogDir, errorLog);
        if (errorLog is not "")
            _ = await MessageDialog.ShowAsync();
    }

    private async void OpenParseExceptionLogTapped(object sender, TappedRoutedEventArgs e)
    {
        _ = await MessageDialog.ShowAsync();
    }

    private void OpenParsingLogInExplorer(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        PathTool.OpenFileOrFolderInShell(_parsingLogDir);
    }

    private async void InfoBarButtonTapped(object sender, RoutedEventArgs e)
    {
        await File.WriteAllTextAsync(_warningLogDir, _vm.WarningMessage);
        PathTool.OpenFileOrFolderInShell(_warningLogDir);
    }
}
