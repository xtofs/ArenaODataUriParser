
namespace TreeFormatting;

public record struct TreeStyle(
    string Indent,      // "  "
    string Branch,      // "├─"
    string LastBranch,  // "└─"
    string Vertical,    // "│ "
    string Space)       // "  "
{

#if DEBUG
    readonly bool valid = Validate(Indent, Branch, LastBranch, Vertical, Space);

    private static bool Validate(params string[] strings)
    {
        return strings.Select(s => s.Length).AllEqual() ? true :
            throw new ArgumentException("All TreeStyle components must have the same length. " +
                $"Received lengths: {string.Join(", ", strings.Select(s => $"'{s}' {s.Length}"))}");
    }

#endif

    public static TreeStyle Default => Ascii;

    public static TreeStyle Ascii { get; } = new TreeStyle(
        Indent: "   ",
        Branch: "|--",
        LastBranch: "+--",
        Vertical: "|  ",
        Space: "   "
    );

    public static TreeStyle Unicode { get; } = new TreeStyle(
        Indent: "  ",
        Branch: "├─",
        LastBranch: "└─",
        Vertical: "│ ",
        Space: "  "
    );


    public static TreeStyle AsciiWide { get; } = new TreeStyle(
        Indent: "    ",
        Branch: "|-- ",
        LastBranch: "+-- ",
        Vertical: "|   ",
        Space: "    "
     );

}
