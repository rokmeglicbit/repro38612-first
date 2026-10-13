using System.Text;

namespace Repro38612;

// Zapisuje neujete napake v datoteko (sinhrono, da se zapis konča, preden iOS aplikacijo ubije)
public static class CrashLogger
{
    private static readonly object gate = new();

    public static string LogPath => Path.Combine(FileSystem.AppDataDirectory, "crash.log");

    public static void Init()
    {
        Write("=== App start ===");

        // Neujete izjeme na katerikoli niti
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Write($"AppDomain.UnhandledException (IsTerminating={e.IsTerminating})", e.ExceptionObject?.ToString());

        // Neopazovani Taski (async void / fire-and-forget)
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Write("TaskScheduler.UnobservedTaskException", e.Exception?.ToString());
            e.SetObserved();
        };

#if IOS
        // Managed izjema, ki gre skozi native iOS kodo (npr. LayoutSubviews). Sproži se tik pred abort().
        ObjCRuntime.Runtime.MarshalManagedException += (_, e) =>
            Write($"Runtime.MarshalManagedException (mode={e.ExceptionMode})", e.Exception?.ToString());

        // Native Objective-C izjema, ki gre v managed kodo
        ObjCRuntime.Runtime.MarshalObjectiveCException += (_, e) =>
            Write($"Runtime.MarshalObjectiveCException (mode={e.ExceptionMode})",
                  $"{e.Exception?.Name}: {e.Exception?.Reason}");
#endif
    }

    public static void Write(string title, string? details = null)
    {
        try
        {
            StringBuilder sb = new();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [thread {Environment.CurrentManagedThreadId}] {title}");
            if (!string.IsNullOrEmpty(details))
            {
                sb.AppendLine(details);
            }
            sb.AppendLine();

            lock (gate)
            {
                File.AppendAllText(LogPath, sb.ToString());
            }
        }
        catch
        {
            // Logiranje nikoli ne sme podreti aplikacije
        }
    }

    public static string ReadAll(int maxChars = 30000)
    {
        try
        {
            if (!File.Exists(LogPath)) return "(log je prazen)";
            string text = File.ReadAllText(LogPath);
            return text.Length > maxChars ? "..." + text[^maxChars..] : text;
        }
        catch (Exception ex)
        {
            return "Napaka pri branju loga: " + ex.Message;
        }
    }

    public static void Clear()
    {
        try { lock (gate) { File.Delete(LogPath); } } catch { }
    }
}
