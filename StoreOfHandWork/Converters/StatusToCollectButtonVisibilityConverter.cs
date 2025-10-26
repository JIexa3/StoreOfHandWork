using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Converters
{
    public class StatusToCollectButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrderStatus status)
            {
                return status == OrderStatus.Новый ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 