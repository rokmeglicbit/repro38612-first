using System.Diagnostics;
using Microsoft.Maui.Dispatching;
using Repro38612.Models;
using Repro38612.ViewModels;

namespace Repro38612;

// Namenoma najslabši možen scenarij:
//  - GC se neprestano prisili (managed peer-ji nativnih viewov se pobirajo)
//  - vsa jedra so zasedena (CPU busy)
//  - "MQTT" poplava posodobitev na UI niti
//  - Right se neprestano preklaplja null <-> vrednost (TemplateHost gradi poddrevo sinhrono)
//  - Rows se nadomešča, seznam se skriva/kaže, nenehno se scrolla (reciklaža celic, kamere)
public sealed class ChaosEngine
{
    private readonly MainViewModel vm;
    private readonly CollectionView list;
    private readonly IDispatcher dispatcher;

    private CancellationTokenSource? cts;
    private IDispatcherTimer? timer;
    private int tick;

    public bool IsRunning => cts is not null;

    public ChaosEngine(MainViewModel vm, CollectionView list, IDispatcher dispatcher)
    {
        this.vm = vm;
        this.list = list;
        this.dispatcher = dispatcher;
    }

    public void Start()
    {
        if (cts is not null) return;

        cts = new CancellationTokenSource();
        CancellationToken ct = cts.Token;

        // 1) GC kladivo
        Task.Factory.StartNew(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                List<byte[]> junk = new();
                for (int i = 0; i < 2000; i++) junk.Add(new byte[512]);
                junk.Clear();

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
                Thread.Sleep(2);
            }
        }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        // 2) "MQTT" poplava na UI niti
        Task.Factory.StartNew(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i < 30; i++)
                {
                    MainThread.BeginInvokeOnMainThread(vm.TouchRandomControls);
                }
                Thread.Sleep(1);
            }
        }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        // 3) CPU burn na vseh razen enem jedru
        int burners = Math.Max(1, Environment.ProcessorCount - 1);
        for (int b = 0; b < burners; b++)
        {
            Task.Factory.StartNew(() =>
            {
                double x = 1;
                while (!ct.IsCancellationRequested)
                {
                    x = Math.Sqrt(x + 1) * Math.Sin(x);
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        // 4) UI kaos na vsakih 10 ms
        timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(10);
        timer.Tick += OnTick;
        timer.Start();
    }

    public void Stop()
    {
        timer?.Stop();
        if (timer is not null) timer.Tick -= OnTick;
        timer = null;

        cts?.Cancel();
        cts = null;

        vm.IsListVisible = true;
    }

    // Posnema while(true) reconnect zanko za MQTT, ki teče NA GLAVNI NITI in nikoli ne vrne nadzora run loopu.
    // Aplikacija "živi" naprej samo zato, ker se Connected (onConnected) kliče sinhrono znotraj zanke.
    // Kličite na UI niti. Po 'duration' zanka vrne nadzor (da se aplikacija ne zaklene za vedno).
    public void HogMainThread(TimeSpan duration)
    {
        if (!IsRunning) Start();   // GC kladivo, CPU burn in poplava na ozadnjih nitih

        CancellationToken ct = cts?.Token ?? CancellationToken.None;
        Stopwatch total = Stopwatch.StartNew();
        Stopwatch sinceConnected = Stopwatch.StartNew();

        while (total.Elapsed < duration)
        {
            // Kot v pravi kodi: samo stalno preverjamo token, nikoli ne gremo iz niti
            if (ct.IsCancellationRequested)
            {
                continue;
            }

            // Vsakih ~5 ms "pride" onConnected in nadaljevanje (MainViewModel...) teče kar tukaj
            if (sinceConnected.ElapsedMilliseconds >= 5)
            {
                sinceConnected.Restart();
                OnConnected();
            }
        }
    }

    private void OnConnected()
    {
        DoTick();   // posodobitve ViewModela, nadomestitev Rows, preklop Right, IsVisible, ScrollTo

#if IOS
        // Run loop ne teče, zato layout prisilimo sami (podobno kot UpdateVisibility -> LayoutIfNeeded)
        if (list.Handler?.PlatformView is UIKit.UIView view)
        {
            view.SetNeedsLayout();
            view.LayoutIfNeeded();
            view.Window?.RootViewController?.View?.LayoutIfNeeded();
        }
#endif
    }

    private void OnTick(object? sender, EventArgs e) => DoTick();

    private void DoTick()
    {
        tick++;

        // Right: null <-> vrednost
        foreach (DeviceRow row in vm.Devices.Rows)
        {
            if (Random.Shared.Next(3) == 0)
            {
                row.FlipRight();
            }
        }

        // Rows nadomestimo z novo kolekcijo
        if (tick % 50 == 0)
        {
            vm.Devices.BuildRows(DemoData.Create());
        }

        // Seznam skrijemo in kmalu spet pokažemo
        if (tick % 70 == 0) vm.IsListVisible = false;
        if (tick % 70 == 3) vm.IsListVisible = true;

        // Nenehen scroll (reciklaža celic; sproži tudi Scrolled in klice kamer)
        if (tick % 2 == 0 && vm.Devices.Rows.Count > 0 && vm.IsListVisible)
        {
            int index = Random.Shared.Next(vm.Devices.Rows.Count);
            list.ScrollTo(index, position: ScrollToPosition.Start, animate: false);
        }
    }
}
