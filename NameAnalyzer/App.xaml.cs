using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace NameAnalyzer;

public partial class App : Application
{
    public App()
    {
        // 应用标题
        CurrentContext.Title = nameof(NameAnalyzer);
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = new MainWindow();
        // 窗口初始大小
        AppHelper.Initialize(new()
        {
            Size = new(900, 1200),
            UnhandledExceptionHandler = UnhandledExceptionHandler
        });
        CurrentContext.App.Resources["NavigationViewContentMargin"] = new Thickness(0);
    }

    private Task UnhandledExceptionHandler(Exception arg)
    {
        
        return Task.CompletedTask;
    }
}
