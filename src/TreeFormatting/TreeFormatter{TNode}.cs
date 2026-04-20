namespace TreeFormatting;


/// <summary>
/// Formats a tree structure into a text representation using a specified style.
/// </summary>
/// <typeparam name="TNode"></typeparam>
/// <param name="treeView"></param>
/// <param name="style"></param>
/// <remarks>based on Andrew Lock's implementation: https://andrewlock.net/creating-a-tree-view-in-csharp/</remarks>
public readonly record struct TreeFormatter<TNode>(
    Func<TNode, string> GetText,
    Func<TNode, IReadOnlyCollection<TNode>> GetChildren)
{

    public void Format(TNode node, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Format(node, TreeStyle.Default, writer);
    }

    public void Format(TNode node, TreeStyle style, TextWriter writer)
    {
        FormatNode(node, writer, style, "", true);
    }

    private void FormatNode(TNode node, TextWriter writer, TreeStyle style, string indent, bool isLast)
    {
        var text = this.GetText(node);
        var prefix = isLast ? style.LastBranch : style.Branch;
        writer.WriteLine($"{indent}{prefix}{text}");

        var children = this.GetChildren(node);
        var childIndent = indent + (isLast ? style.Space : style.Vertical);
        var lastIndex = children.Count - 1;
        foreach (var (child, i) in children.Select((child, i) => (child, i)))
        {
            FormatNode(child, writer, style, childIndent, i == lastIndex);
        }
    }

    // public void Format(IReadOnlyCollection<TNode> nodes, TreeStyle style, TextWriter writer)
    // {
    //     foreach (var node in nodes)
    //     {
    //         Format(node, style, writer);
    //     }
    // }
}
