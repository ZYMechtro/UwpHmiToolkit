using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace UwpHmiToolkit.UiConverter
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool b
                ? b ? "ON" : "Off"
                : throw new ArgumentException("value is not a bool.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool b && b
                ? new SolidColorBrush() { Color = Colors.LightGreen }
                : new SolidColorBrush() { Color = Colors.LightGray };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is int i ? i.ToString() : throw new ArgumentException("Converter: value is not a int.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is string str && int.TryParse(str, out var number) ? number : 0;
        }
    }

    public class IntToHexStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is int i ? i.ToString("X") : throw new ArgumentException("Converter: value is not a int.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToDecimalStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int v)
            {
                int digit = parameter is string s ? int.Parse(s) : 0;
                float output = v * MathF.Pow(0.1f, digit);
                string format = "0.";
                for (int i = 0; i < digit; i++)
                {
                    format += "0";
                }
                return output.ToString(format);
            }
            else
            {
                throw new ArgumentException("Converter: value is not a int.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToSigned16StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int u )
            {
                var v = (short)u;
                int digit = parameter is string s ? int.Parse(s) : 0;
                float output = v * MathF.Pow(0.1f, digit);
                string format = "0.";
                for (int i = 0; i < digit; i++)
                {
                    format += "0";
                }
                return output.ToString(format);
            }
            else
            {
                throw new ArgumentException("Converter: value is not a int.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IntTypeFloatToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is int v
                ? BitConverter.ToSingle(BitConverter.GetBytes(v), 0).ToString()
                : throw new ArgumentException("Converter: value is not a int.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();

        }
    }

    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is int i && parameter is string p && int.TryParse(p, out int c) && i == c
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class UshortToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is ushort i ? i.ToString() : throw new ArgumentException("Converter: value is not a ushort.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is string str && ushort.TryParse(str, out var number) ? number : (ushort)0;
        }
    }

    public class IntToShortStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int v && v < 65536)
            {
                int digit = parameter is string s ? int.Parse(s) : 0;
                short su = (short)v;
                float output = su * MathF.Pow(0.1f, digit);
                string format = "0.";
                for (int i = 0; i < digit; i++)
                {
                    format += "0";
                }
                return output.ToString(format);
            }
            else
            {
                throw new ArgumentException("Converter: value is not a short.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is double i ? i.ToString() : throw new ArgumentException("Converter: value is not a double.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is string str && double.TryParse(str, out var number) ? number : 0;
        }
    }


    public class FloatToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is float i ? i.ToString() : throw new ArgumentException("Converter: value is not a double.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is string str && float.TryParse(str, out var number) ? number : 0;
        }
    }


}
