using System.Globalization;
using System.Windows.Data;
using OtimizaPC.Helpers;

namespace OtimizaPC.Converters;

public class BytesToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is long bytes ? FormatHelper.FormatarBytes(bytes) : "0 B";

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
