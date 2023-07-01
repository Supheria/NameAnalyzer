using System;
using Microsoft.UI.Xaml.Data;

namespace NameAnalyzer.Converters;

public class IntToStringForItemNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        (int)value <= 0 ? "" : (int)value is 1 ? $"{(int)value} item" : $"{(int)value} items";


    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
