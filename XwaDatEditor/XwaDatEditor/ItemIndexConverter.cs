using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace XwaDatEditor
{
    class ItemIndexConverter : FrameworkContentElement, IValueConverter
    {
        public ItemIndexConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = DataContext as IList;

            if (list == null)
            {
                return string.Empty;
            }

            int index = list.IndexOf(value) + 1;

            return index.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
