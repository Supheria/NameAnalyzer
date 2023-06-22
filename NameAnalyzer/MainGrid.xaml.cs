using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Parser.Helper;
using Utilities;
using Windows.Storage;
using WinUI3Utilities;

namespace NameAnalyzer;

public sealed partial class MainGrid : Grid
{
    public MainGrid()
    {
        InitializeComponent();
        CurrentContext.TitleBar = TitleBar;
        CurrentContext.TitleTextBlock = TitleTextBlock;
        _vm.NameInfoSourceFile = TextBlock.Inlines;
    }
    private readonly MainViewModel _vm = new();

    private readonly string _logDir = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "hoi4 script parse exception.txt");

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
        await File.WriteAllTextAsync(_logDir, errorLog);
        if (errorLog is not "")
            _ = await MessageDialog.ShowAsync();
    }

    private async void OpenParseExceptionLogTapped(object sender, TappedRoutedEventArgs e)
    {
        _ = await MessageDialog.ShowAsync();
    }

    private void OpenFileInExplorer(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        PathTool.OpenFileOrFolderInShell(_logDir);
    }

    private void SelectedNameChanged(object sender, SelectionChangedEventArgs e)
    {
        var names = sender.To<ComboBox>();
        if (names.SelectedIndex is -1)
            return;
        _vm.SetNameInfo();
    }

    private void SelectedPropertyNameChanged(object sender, SelectionChangedEventArgs e)
    {
        var propertyNames = sender.To<ListView>();
        if (propertyNames.SelectedIndex is -1)
            _vm.SetNameInfo();
        else
            _vm.SetPropertyNameInfo(propertyNames.SelectedValue.To<SubItem>().Name);
    }

    private void ListViewItemOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        _vm.SelectedLevel += 1;
        _vm.SelectedNameIndex = _vm.NamePickerSource.IndexOf(sender.To<ListViewItem>().GetTag<SubItem>().Name);
    }
}
