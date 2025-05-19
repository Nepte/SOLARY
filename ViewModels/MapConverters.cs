using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Globalization;

namespace SOLARY.ViewModels
{
    public class BoolToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isAvailable)
            {
                return isAvailable ? "icon_available.png" : "icon_occupied.png";
            }
            return "icon_map.png";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToStyleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isAvailable && parameter is Style availableStyle)
            {
                return isAvailable ? availableStyle : null;
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentToProgressConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int percentage)
            {
                return percentage / 100.0;
            }
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
