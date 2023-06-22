using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Parser.helper;
using WinUI3Utilities;

namespace NameAnalyzer;

public sealed partial class MainWindow : Window
{
    private bool DialogIsShowed { get; set; }

    public MainWindow()
    {
        CurrentContext.Window = this;
        _vm = new(async () =>
        {
            if (DialogIsShowed)
                return;
            DialogIsShowed = true;
            _ = await MessageDialog.ShowAsync();
            DialogIsShowed = false;
        });
        InitializeComponent();
        CurrentContext.TitleBar = TitleBar;
        CurrentContext.TitleTextBlock = TitleTextBlock;
    }

    private readonly MainViewModel _vm;

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
        _vm.ParseException = errorLog;
        await File.WriteAllTextAsync(_logDir, errorLog);
    }

    private void OpenParseExceptionLogTapped(object sender, TappedRoutedEventArgs e)
    {
        _vm.MessageDialogText = _vm.ParseException;
    }
    private void OpenMessageDialogTapped(object sender, TappedRoutedEventArgs e)
    {
        _vm.MessageDialogText = _vm.ErrorMessage;
    }

    private void OpenFileInExplorer(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = _logDir,
                UseShellExecute = true
            }
        };
        _ = process.Start();
    }

    private void SelectedNameChanged(object sender, SelectionChangedEventArgs e)
    {
        var names = sender.To<ComboBox>();
        if (names.SelectedIndex is -1)
            return;
        _vm.SetNameInfo();

        if (!_vm.ErrorSet.HasNewToShow)
            return;
        _vm.MessageDialogText += "\n" + _vm.ErrorSet;
        _vm.ErrorSet.HasNewToShow = false;
    }

    private void SelectedPropertyNameChanged(object sender, SelectionChangedEventArgs e)
    {
        // todo: ...
        var propertyNames = sender.To<ListView>();
        if (propertyNames.SelectedIndex is -1)
            _vm.SetNameInfo();
        else
            _vm.SetPropertyNameInfo(propertyNames.SelectedValue.ToString()!);

        if (!_vm.ErrorSet.HasNewToShow)
            return;
        _vm.MessageDialogText += "\n" + _vm.ErrorSet;
        _vm.ErrorSet.HasNewToShow = false;
    }
}
