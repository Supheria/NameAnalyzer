using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace NameAnalyzer.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value.To<bool>() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
