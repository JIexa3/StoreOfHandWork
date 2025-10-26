using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace StoreOfHandWork.Converters
{
    public class MultiplyConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return 0;

            if (decimal.TryParse(values[0].ToString(), out decimal price) &&
                int.TryParse(values[1].ToString(), out int quantity))
            {
                return price * quantity;
            }

            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
