using System;
using System.Globalization;
using System.Windows.Data;

namespace StoreOfHandWork.Converters
{
    public class ProductStockToButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stockQuantity)
            {
                return stockQuantity > 0 ? "В корзину" : "Нет в наличии";
            }
            return "Нет в наличии";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
