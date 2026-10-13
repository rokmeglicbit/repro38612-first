using Repro38612.Models;

namespace Repro38612.Controls;

public class TemplateHost : ContentView
{
    public static readonly BindableProperty HostContentProperty = BindableProperty.Create(
        nameof(HostContent), typeof(object), typeof(TemplateHost), null,
        propertyChanged: (b, _, _) => ((TemplateHost)b).Rebuild());

    public static readonly BindableProperty TemplateSelectorProperty = BindableProperty.Create(
        nameof(TemplateSelector), typeof(DataTemplateSelector), typeof(TemplateHost), null,
        propertyChanged: (b, _, _) => ((TemplateHost)b).Rebuild());

    public object? HostContent
    {
        get => GetValue(HostContentProperty);
        set => SetValue(HostContentProperty, value);
    }

    public DataTemplateSelector? TemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(TemplateSelectorProperty);
        set => SetValue(TemplateSelectorProperty, value);
    }

    // NAMERNO sinhrono, brez await Task.Delay(1): staro poddrevo se podre sredi layout passa
    private void Rebuild()
    {
        Content = null;

        object? item = HostContent is IEnumerable<DemoControl> list ? list.FirstOrDefault() : HostContent;
        if (item is null || TemplateSelector is null)
        {
            return;
        }

        DataTemplate template = TemplateSelector.SelectTemplate(item, this);
        if (template.CreateContent() is View view)
        {
            view.BindingContext = item;
            Content = view;
        }
    }
}
