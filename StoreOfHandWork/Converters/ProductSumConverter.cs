using System;
using System.Globalization;
using System.Windows.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Converters
{
    public class ProductSumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CartItem cartItem)
            {
                return $"{cartItem.Product.Price * cartItem.Quantity:C}";
            }
            return "0.00 ₽";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
