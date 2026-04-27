static class Colors
{
    static Colors()
    {
        // if the output is redirected, i.e. not a TTY, 
        // we disable colors to avoid polluting the output with ANSI escape codes.
        if (Console.IsOutputRedirected)
        {
            Red = "";
            Green = "";
            Yellow = "";
            Blue = "";
            Magenta = "";
            Cyan = "";
            Reset = "";
        }
        else
        {
            Red = "\u001b[31m";
            Green = "\u001b[32m";
            Yellow = "\u001b[33m";
            Blue = "\u001b[34m";
            Magenta = "\u001b[35m";
            Cyan = "\u001b[36m";
            Reset = "\u001b[0m";
        }
    }
    public static string Red { get; }
    public static string Green { get; }
    public static string Yellow { get; }
    public static string Blue { get; }
    public static string Magenta { get; }
    public static string Cyan { get; }
    public static string Reset { get; }
}