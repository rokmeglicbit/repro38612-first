using System.Net.Sockets;
using Repro38612.Models;
using Repro38612.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Repro38612;

public partial class MainPage : ContentPage
{
    // true = chaos se sam zažene 3 s po zagonu (po "kill + ponovni zagon" naj bi se napaka pojavila)
    private const bool AutoStartChaos = true;

    // true = 5 s po zagonu se glavna nit zasede z zanko (kot while(true) reconnect za MQTT) za 20 s
    private const bool AutoStartHog = true;

    // Nedosegljiv naslov kamere (http kamera ni na voljo, kot v pravi aplikaciji)
    private const string CameraHost = "192.168.1.1";

    private readonly Dictionary<DemoControl, WeakReference<SKCanvasView>> canvases = [];
    private readonly MainViewModel viewModel = new();
    private ChaosEngine? chaos;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        chaos = new ChaosEngine(viewModel, listControlsItems, Dispatcher);

        if (AutoStartChaos)
        {
            await Task.Delay(3000);
            StartChaos();
        }

        if (AutoStartHog)
        {
            await Task.Delay(2000);
            chaos.HogMainThread(TimeSpan.FromSeconds(20));
        }
    }

    // Glavna nit bo 20 s zasedena; UI se v tem času ne bo odzival, razen prek sinhronih klicev v zanki
    private void OnHogClicked(object? sender, EventArgs e)
    {
        chaos?.HogMainThread(TimeSpan.FromSeconds(20));
    }

    private void OnChaosClicked(object? sender, EventArgs e)
    {
        if (chaos is null) return;

        if (chaos.IsRunning)
        {
            chaos.Stop();
            viewModel.ChaosText = "Chaos START";
        }
        else
        {
            StartChaos();
        }
    }

    private void StartChaos()
    {
        chaos?.Start();
        viewModel.ChaosText = "Chaos STOP";
    }

    private void OnCameraPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        e.Surface.Canvas.Clear(SKColors.LightSteelBlue);

        if (sender is SKCanvasView canvas && canvas.BindingContext is DemoControl control)
        {
            canvases[control] = new WeakReference<SKCanvasView>(canvas);
        }
    }

    // Vsak scroll event sproži poizvedbe za vse vidne kamere, brez omejitve in brez preklica
    private void ListControlsItems_Scrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        int first = Math.Max(e.FirstVisibleItemIndex, 0);
        int last = Math.Min(e.LastVisibleItemIndex, viewModel.Devices.Rows.Count - 1);

        for (int i = first; i <= last; i++)
        {
            DeviceRow row = viewModel.Devices.Rows[i];
            if (row.Left is { Count: 1 } && row.Left[0] is { IsCamera: true } control
                && canvases.TryGetValue(control, out WeakReference<SKCanvasView>? weak)
                && weak.TryGetTarget(out SKCanvasView? canvas))
            {
                _ = PollCameraAsync(canvas);
            }
        }
    }

    private static async Task PollCameraAsync(SKCanvasView canvas)
    {
        try
        {
            // Kot v pravi kodi: TCP preverjanje brez timeouta, nato HTTP (nikoli se ne dispose-a)
            TcpClient tcpClient = new();
            await tcpClient.ConnectAsync(CameraHost, 80);

            HttpClient httpClient = new() { BaseAddress = new Uri($"http://{CameraHost}/") };
            HttpResponseMessage response = await httpClient.GetAsync("");
            await response.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            // Kamera ni na voljo
        }
        finally
        {
            // Simulacija uspešne poti iz prave kode; celica je lahko med čakanjem že reciklirana
            MainThread.BeginInvokeOnMainThread(() =>
            {
                canvas.InvalidateSurface();
                canvas.InvalidateMeasure();
            });
        }
    }
}
