using System;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace NameAnalyzer.Converters;

public class NameInfoLabelTypeToStringConverter : IValueConverter
{
    public static object Convert(object value) => value.To<NameInfoLabelType>() switch
    {
        NameInfoLabelType.Type => "Type",
        NameInfoLabelType.PropertyName => "Property",
        NameInfoLabelType.Value => "Value",
        NameInfoLabelType.SourceFile => "Source File",
        NameInfoLabelType.Warning => "!",
        NameInfoLabelType.From => "From",
        _ => ""
    };

    public object Convert(object value, Type targetType, object parameter, string language) => Convert(value);

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
