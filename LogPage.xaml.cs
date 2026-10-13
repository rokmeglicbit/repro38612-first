namespace Repro38612;

public partial class LogPage : ContentPage
{
    public LogPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Load();
    }

    private void Load() => logLabel.Text = CrashLogger.ReadAll();

    private void OnRefresh(object? sender, EventArgs e) => Load();

    private void OnClear(object? sender, EventArgs e)
    {
        CrashLogger.Clear();
        Load();
    }

    private async void OnShare(object? sender, EventArgs e)
    {
        if (!File.Exists(CrashLogger.LogPath)) return;

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "crash.log",
            File = new ShareFile(CrashLogger.LogPath)
        });
    }
}
