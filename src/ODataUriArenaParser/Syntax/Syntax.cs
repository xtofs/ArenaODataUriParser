using TreeFormatting;

namespace ODataUriParser.Syntax;


/// <summary>
/// Syntax is a ref struct that represents the syntax tree of an OData URI. 
/// It contains the original request buffer, the tokens, the syntax nodes, 
/// the child indices, and the index of the root node. 
/// 
/// The Syntax is designed to be used in a single pass parser that constructs 
/// the syntax tree in place without allocating additional memory for intermediate data structures.
/// 
/// Another way to look at it is that Syntax is a smart view over the arena which is a contiguous 
/// block of memory that contains all the tokens and syntax nodes and projects it as tables of 
/// tokens and syntax nodes with indices to represent the parent-child relationships between the nodes.
/// </summary>
/// <param name="requestBuffer"></param>
/// <param name="tokens"></param>
/// <param name="nodes"></param>
/// <param name="childIndices"></param>
/// <param name="rootNodeIndex"></param>
public readonly ref struct Syntax(
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

    static readonly TreeFormatter<SyntaxNode, Syntax> formatter = new TreeFormatter<SyntaxNode, Syntax>(
        GetLabel, GetChildren);

    private static string GetLabel(SyntaxNode node, Syntax syntax)
    {
        return node.Kind switch
        {
            SyntaxKind.Constant or SyntaxKind.VariableAccess or SyntaxKind.PropertyAccess
                => $"{node.Kind} '{node.GetTokenText(syntax)}'",
            SyntaxKind.BinaryExpression or SyntaxKind.UnaryExpression =>
                $"{node.Kind} ({node.GetOperatorKind()})",
            _ => throw new NotImplementedException(),
        };
    }

    private static SyntaxNode[] GetChildren(SyntaxNode node, Syntax syntax)
    {
        return node.GetChildren(syntax).ToArray();
    }
}

