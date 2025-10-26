using System;
using System.Globalization;
using System.Windows.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Converters
{
    public class OrderStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrderStatus status)
            {
                return status switch
                {
                    OrderStatus.Новый => "Новый",
                    OrderStatus.ВОбработке => "В обработке",
                    OrderStatus.Отправлен => "Отправлен",
                    OrderStatus.Доставлен => "Доставлен",
                    OrderStatus.Отменен => "Отменён",
                    _ => status.ToString()
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
