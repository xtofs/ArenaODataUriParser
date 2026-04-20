using TreeFormatting;

namespace ODataUriArenaParser.Syntax;


/// <summary>
/// ArenaSyntax is a ref struct that represents the syntax tree of an OData URI. 
/// It contains the original request buffer, the tokens, the syntax nodes, 
/// the child indices, and the index of the root node. 
/// 
/// The ArenaSyntax is designed to be used in a single pass parser that constructs 
/// the syntax tree in place without allocating additional memory for intermediate data structures.
/// 
/// Another way to look at it is that ArenaSyntax is a smart view over the arena which is a contiguous 
/// block of memory that contains all the tokens and syntax nodes and projects it as tables of 
/// tokens and syntax nodes with indices to represent the parent-child relationships between the nodes.
/// </summary>
/// <param name="requestBuffer"></param>
/// <param name="tokens"></param>
/// <param name="nodes"></param>
/// <param name="childIndices"></param>
/// <param name="rootNodeIndex"></param>
public readonly ref struct ArenaSyntax(
        ReadOnlySpan<byte> requestBuffer,
        ReadOnlySpan<Token> tokens,
        ReadOnlySpan<SyntaxNode> nodes,
        ReadOnlySpan<int> childIndices,
        int rootNodeIndex)
{
    public ReadOnlySpan<byte> RequestBuffer { get; } = requestBuffer;
    public ReadOnlySpan<Token> Tokens { get; } = tokens;
    public ReadOnlySpan<SyntaxNode> Nodes { get; } = nodes;
    public ReadOnlySpan<int> ChildIndices { get; } = childIndices;
    public int RootNodeIndex { get; } = rootNodeIndex;

    public void Deconstruct(
       out ReadOnlySpan<SyntaxNode> nodes,
       out ReadOnlySpan<Token> tokens,
       out ReadOnlySpan<int> children,
       out ReadOnlySpan<byte> buffer,
        out int rootNodeIndex)
    {
        nodes = Nodes;
        tokens = Tokens;
        children = ChildIndices;
        buffer = RequestBuffer;
        rootNodeIndex = RootNodeIndex;
    }

    public void ToTree(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var rootNode = Nodes[RootNodeIndex];
        formatter.Format(rootNode, this, writer);
        writer.Flush();
    }

    public string ToTree()
    {
        var writer = new StringWriter();

        var rootNode = Nodes[RootNodeIndex];
        formatter.Format(rootNode, this, writer);
        writer.Flush();
        return writer.ToString();
    }

    static readonly TreeFormatter<SyntaxNode, ArenaSyntax> formatter = new TreeFormatter<SyntaxNode, ArenaSyntax>(
        GetLabel, GetChildren);

    private static string GetLabel(SyntaxNode node, ArenaSyntax syntax)
    {
        var label = $"{node.Kind}";
        switch (node.Kind)
        {
            case SyntaxKind.Constant or SyntaxKind.VariableAccess or SyntaxKind.PropertyAccess:
                label += $" '{node.GetTokenText(syntax)}'";
                break;

            case SyntaxKind.BinaryExpression or SyntaxKind.UnaryExpression:
                label += $" ({node.GetOperatorKind()})";
                break;
        }
        return label;
    }

    private static SyntaxNode[] GetChildren(SyntaxNode node, ArenaSyntax syntax)
    {
        var (nodes, _, children, _, _) = syntax;
        var result = new SyntaxNode[node.ChildCount];

        for (int i = 0; i < node.ChildCount; i++)
        {
            int childIndex = children[node.FirstChild + i];
            if (childIndex < 0 || childIndex >= syntax.Nodes.Length)
            {
                throw new InvalidOperationException($"Child index {childIndex} is out of range.");
            }
            result[i] = syntax.Nodes[childIndex];
        }
        return result;
    }
}

