using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Repro38612;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        CrashLogger.Init();   // čim prej, da ujamemo tudi napake ob zagonu

        return MauiApp.CreateBuilder().UseMauiApp<App>().UseSkiaSharp().Build();
    }
}
