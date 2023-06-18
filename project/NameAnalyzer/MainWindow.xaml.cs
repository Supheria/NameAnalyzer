using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
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

    private async void SelectFileTapped(object sender, TappedRoutedEventArgs e)
    {
        if (await PickerHelper.PickSingleFileAsync() is { } file)
            _vm.Analyzer = new(file.Path);
    }

    private async void SelectFolderTapped(object sender, TappedRoutedEventArgs e)
    {
        if (await PickerHelper.PickSingleFolderAsync() is { } folder)
            _vm.Analyzer = new(folder.Path);
    }
}
