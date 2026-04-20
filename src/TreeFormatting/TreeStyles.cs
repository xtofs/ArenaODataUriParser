
namespace TreeFormatting;

public static class TreeStyles
{
    public static TreeStyle Unicode { get; } = new TreeStyle(
        Indent: "  ",
        Branch: "├─",
        LastBranch: "└─",
        Vertical: "│ ",
        Space: "  "
    );
}


//     public static TreeStyle Default => Ascii;

//     public static TreeStyle Unicode { get; } = new StreeStyle(
//         Indent: "  ",
//         Branch: "├─",
//         LastBranch: "└─",
//         Vertical: "│ ",
//         Space: "  "
//     );

//     public static StreeStyle Ascii { get; } = new StreeStyle(
//         Indent: "  ",
//         Branch: "|--",
//         LastBranch: "+--",
//         Vertical: "| ",
//         Space: "  "
//      );

//     public static StreeStyle Ascii { get; } = new StreeStyle(
//         Indent: "  ",
//         Branch: "|--",
//         LastBranch: "+--",
//         Vertical: "| ",
//         Space: "  "
//      );

//     public static StreeStyle Unicode { get; } = new StreeStyle(
//        Indent: "   ",
//        Branch: "├─",
//        LastBranch: "└─",
//        Vertical: "│ ",
//        Space: "   "
//     );
// }

//     // private const string LastChildPrefix = "+-- ";
//     // private const string NonLastChildPrefix = "|-- ";
//     // private const string LastChildIndent = "    ";
//     // private const string NonLastChildIndent = "|   ";


//     private const string LastChildPrefix = " └─";
//     private const string NonLastChildPrefix = " ├─";
//     private const string LastChildIndent = "   ";
//     private const string NonLastChildIndent = " │ ";

