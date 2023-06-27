using System;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace NameAnalyzer.Converters;

public class NameInfoLabelTypeToString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        switch (value.To<NameInfoLabelType>())
        {
            case NameInfoLabelType.Type:
                return "Type";
            case NameInfoLabelType.PropertyName:
                return "Property Name";
            case NameInfoLabelType.Value:
                return "Value";
            case NameInfoLabelType.SourceFile:
                return "Source File";
            default:
                return "Null";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

