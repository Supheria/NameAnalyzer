using System;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace NameAnalyzer.Converters;

public class StringIsNotEmptyToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        !string.IsNullOrEmpty(value.To<string>());

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
