using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SOLARY.ViewModels
{
    public class BoolToTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string options)
            {
                string[] texts = options.Split('|');
                if (texts.Length == 2)
                {
                    return boolValue ? texts[1] : texts[0];
                }
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
