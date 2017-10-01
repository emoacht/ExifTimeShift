using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExifDateEditor.Views.Converters
{
	[ValueConversion(typeof(bool?), typeof(Brush))]
	public class BooleanToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool?))
				return DependencyProperty.UnsetValue;

			switch ((bool?)value)
			{
				default: return Brushes.Transparent;
				case true: return Brushes.PowderBlue;
				case false: return Brushes.Pink;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}