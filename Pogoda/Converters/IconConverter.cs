using System;
using System.Globalization;
using System.Windows.Data;

namespace Pogoda.Converters  // Важно! Пространство имен должно совпадать
{
    public class IconConverter : IValueConverter  // Важно! Имя класса
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string iconCode && !string.IsNullOrEmpty(iconCode))
            {
                return $"https://openweathermap.org/img/w/{iconCode}.png";
            }
            return "https://openweathermap.org/img/w/01d.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}