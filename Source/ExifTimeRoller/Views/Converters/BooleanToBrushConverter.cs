using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ExifTimeRoller.Views.Converters
{
	[ValueConversion(typeof(bool?), typeof(Brush))]
	public class BooleanToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value switch
			{
				true => Brushes.PowderBlue,
				false => Brushes.Pink,
				_ => Brushes.Transparent
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}