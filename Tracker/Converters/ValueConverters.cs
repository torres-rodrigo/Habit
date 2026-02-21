using System;
using System.Globalization;

namespace Tracker.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorArray = colors.Split('|');
                if (colorArray.Length == 2)
                {
                    var trueColor = colorArray[0];
                    var falseColor = colorArray[1];

                    // Return color based on bool value
                    var colorName = boolValue ? trueColor : falseColor;

                    // Use Application.Current.Resources to get the color
                    if (Application.Current?.Resources.TryGetValue(colorName, out var color) == true)
                    {
                        return color;
                    }
                }
            }
            return Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringNullOrEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTextDecorationConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return TextDecorations.Strikethrough;
            }
            return TextDecorations.None;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertedBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class BoolToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1.0 : 0.3;
            }
            return 0.3;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PriorityToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var priority = value as string;
            return !string.IsNullOrEmpty(priority) && priority != "None";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PriorityToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var priority = value as string;
            return priority switch
            {
                "Low" => Color.FromArgb("#39FF14"),      // Fluorescent green
                "Medium" => Color.FromArgb("#FF8C00"),   // Bright orange
                "High" => Color.FromArgb("#FF0000"),     // Bright red
                _ => Colors.Transparent
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PriorityToShapeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var priority = value as string;
            var targetShape = parameter as string;

            return priority switch
            {
                "Low" => targetShape == "Circle",
                "Medium" => targetShape == "Hexagon",
                "High" => targetShape == "Triangle",
                _ => false
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
