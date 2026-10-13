using System.Globalization;
using Repro38612.Models;

namespace Repro38612;

public class RowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeaderTemplate { get; set; }
    public DataTemplate? PairTemplate { get; set; }
    public DataTemplate? FullTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        DeviceRow row = (DeviceRow)item;
        if (row.IsHeader) return HeaderTemplate!;
        return row.IsFullWidth ? FullTemplate! : PairTemplate!;
    }
}

public class ControlTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CardTemplate { get; set; }
    public DataTemplate? CameraTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => ((DemoControl)item).IsCamera ? CameraTemplate! : CardTemplate!;
}

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
