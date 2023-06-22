using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace NameAnalyzer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        CurrentContext.Window = this;
        InitializeComponent();
    }
}
