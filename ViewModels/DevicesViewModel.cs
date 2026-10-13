using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Repro38612.Models;

namespace Repro38612.ViewModels;

public partial class DevicesViewModel : ObservableObject
{
    private ObservableCollection<DeviceRow> rows = [];
    public ObservableCollection<DeviceRow> Rows
    {
        get => rows;
        set => SetProperty(ref rows, value);
    }

    // Kot v pravi aplikaciji: Rows se vsakič NADOMESTI z novo kolekcijo
    public void BuildRows(IEnumerable<DemoGroup> groups)
    {
        ObservableCollection<DeviceRow> result = [];

        foreach (DemoGroup group in groups)
        {
            if (group.IsGroupHeaderVisible)
            {
                result.Add(new DeviceRow { IsHeader = true, HeaderText = group.Name });
            }

            DemoControl? waiting = null;

            foreach (DemoControl item in group)
            {
                if (item.IsFullWidth)
                {
                    if (waiting != null)
                    {
                        result.Add(new DeviceRow { Left = [waiting] });
                        waiting = null;
                    }
                    result.Add(new DeviceRow { Left = [item] });
                }
                else if (waiting == null)
                {
                    waiting = item;
                }
                else
                {
                    result.Add(new DeviceRow { Left = [waiting], Right = [item] });
                    waiting = null;
                }
            }

            if (waiting != null)
            {
                result.Add(new DeviceRow { Left = [waiting] });
            }
        }

        Rows = result;
    }
}
