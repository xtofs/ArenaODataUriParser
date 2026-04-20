namespace TreeFormatting;

public class TreeFormatter<TNode, TContext>(Func<TNode, TContext, string> getLabel, Func<TNode, TContext, IReadOnlyList<TNode>> getChildren) where TContext : allows ref struct
{
    private readonly Func<TNode, TContext, string> getLabel = getLabel;
    private readonly Func<TNode, TContext, IReadOnlyList<TNode>> getChildren = getChildren;

    public void Format(TNode node, TContext context, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        FormatInternal(node, context, writer, "", true);
    }

    private void FormatInternal(TNode node, TContext context, TextWriter writer, string indent, bool isLast)
    {
        var label = getLabel(node, context);
        var prefix = isLast ? TreeStyles.Unicode.LastBranch : TreeStyles.Unicode.Branch;
        writer.WriteLine($"{indent}{prefix}{label}");

        var children = getChildren(node, context);
        var childIndent = indent + (isLast ? TreeStyles.Unicode.Space : TreeStyles.Unicode.Vertical);
        int lastIndex = children.Count - 1;
        for (int i = 0; i < children.Count; i++)
        {

            FormatInternal(children[i], context, writer, childIndent, i == lastIndex);
        }
    }
}