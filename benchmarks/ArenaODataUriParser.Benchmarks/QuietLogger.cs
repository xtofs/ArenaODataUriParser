using BenchmarkDotNet.Loggers;

public class QuietLogger : ILogger
{
    public static readonly QuietLogger Default = new();
    public string Id => "QuietLogger";
    public int Priority => 0;

    private bool _inTableLine;

    public void Write(LogKind logKind, string text)
    {
        if (logKind is not LogKind.Statistic) return;
        if (IsTableLine(text)) { _inTableLine = true; }
        if (!_inTableLine) return;
        Console.Write(text);
    }

    public void WriteLine()
    {
        if (!_inTableLine) return;
        _inTableLine = false;
        Console.WriteLine();
    }

    public void WriteLine(LogKind logKind, string text)
    {
        if (logKind is not LogKind.Statistic) return;
        if (IsTableLine(text)) { _inTableLine = true; }
        if (!_inTableLine) return;
        _inTableLine = false;
        Console.WriteLine(text);
    }

    private static bool IsTableLine(string text) => text.TrimStart().StartsWith('|');

    public void Flush() { }
}

// public class SpinnerLogger : ILogger
// {
//     public static readonly SpinnerLogger Default = new();
//     public string Id => nameof(SpinnerLogger);
//     public int Priority => 0;

//     private static readonly char[] Frames = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];
//     private readonly bool _isTty = !Console.IsOutputRedirected;
//     private Thread? _spinnerThread;
//     private volatile bool _spinning;
//     private string _currentLabel = "";

//     public void Write(LogKind logKind, string text)
//     {
//         if (logKind == LogKind.Statistic) return;
//         if (!_isTty) { ConsoleLogger.Default.Write(logKind, text); return; }

//         if (logKind == LogKind.Header || logKind == LogKind.Help)
//         {
//             StopSpinner();
//             ConsoleLogger.Default.Write(logKind, text);
//         }
//         else if (logKind == LogKind.Default || logKind == LogKind.Info)
//         {
//             _currentLabel = text.Trim();
//             StartSpinner();
//         }
//     }

//     public void WriteLine(LogKind logKind, string text)
//     {
//         if (logKind == LogKind.Statistic) return;
//         if (!_isTty) { ConsoleLogger.Default.WriteLine(logKind, text); return; }

//         StopSpinner();
//         ConsoleLogger.Default.WriteLine(logKind, text);
//     }

//     public void WriteLine()
//     {
//         if (_isTty) StopSpinner();
//         ConsoleLogger.Default.WriteLine();
//     }

//     public void Flush() => ConsoleLogger.Default.Flush();

//     private void StartSpinner()
//     {
//         if (_spinning) return;
//         _spinning = true;
//         _spinnerThread = new Thread(() =>
//         {
//             int i = 0;
//             while (_spinning)
//             {
//                 Console.Write($"\r{Frames[i % Frames.Length]} {_currentLabel}  ");
//                 i++;
//                 Thread.Sleep(80);
//             }
//             // Clear spinner line
//             Console.Write($"\r{new string(' ', Console.WindowWidth - 1)}\r");
//         })
//         { IsBackground = true };
//         _spinnerThread.Start();
//     }

//     private void StopSpinner()
//     {
//         _spinning = false;
//         _spinnerThread?.Join();
//         _spinnerThread = null;
//     }
// }