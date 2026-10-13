using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Repro38612.Models;

// Podatkovni model (ni View), podoben SmartHouseUIControl
public partial class DemoControl : ObservableObject
{
    public string Name { get; set; } = "";
    public bool IsFullWidth { get; set; }
    public bool IsCamera { get; set; }

    [ObservableProperty]
    private string reading = "";
}

public class DemoGroup : List<DemoControl>
{
    public string Name { get; set; } = "";
    public bool IsGroupHeaderVisible { get; set; } = true;
}

public partial class DeviceRow : ObservableObject
{
    public bool IsHeader { get; set; }
    public string HeaderText { get; set; } = "";

    [ObservableProperty]
    private ObservableCollection<DemoControl>? left;

    [ObservableProperty]
    private ObservableCollection<DemoControl>? right;

    private ObservableCollection<DemoControl>? rightBackup;

    public bool IsFullWidth => Left is { Count: 1 } && Left[0].IsFullWidth;

    // Right preklaplja med null in vrednostjo (Right == null pomeni prazno vsebino)
    public void FlipRight()
    {
        if (Right is not null)
        {
            rightBackup = Right;
            Right = null;
        }
        else if (rightBackup is not null)
        {
            Right = rightBackup;
        }
    }
}

public static class DemoData
{
    // Vsak klic ustvari NOVE objekte, kot pri pravi aplikaciji
    public static List<DemoGroup> Create()
    {
        List<DemoGroup> groups = [];
        for (int g = 0; g < 6; g++)
        {
            DemoGroup group = new() { Name = $"Skupina {g + 1}" };
            for (int i = 0; i < 7; i++)
            {
                group.Add(new DemoControl
                {
                    Name = $"Naprava {g + 1}.{i + 1}",
                    Reading = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsFullWidth = i % 5 == 4,
                    IsCamera = i % 5 == 4
                });
            }
            groups.Add(group);
        }
        return groups;
    }
}
