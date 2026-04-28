using ODataUriParser.Syntax;

namespace ODataUriParser.Benchmarks;

internal sealed class SyntaxSnapshot(
    byte[] requestBuffer,
    Token[] tokens,
    SyntaxNode[] nodes,
    int[] children,
    int rootNodeIndex)
{
    public byte[] RequestBuffer { get; } = requestBuffer;
    public Token[] Tokens { get; } = tokens;
    public SyntaxNode[] Nodes { get; } = nodes;
    public int[] Children { get; } = children;
    public int RootNodeIndex { get; } = rootNodeIndex;

    public static SyntaxSnapshot FromSyntax(Syntax.Syntax syntax)
    {

        return new SyntaxSnapshot(
            syntax.RequestBuffer.ToArray(),
            syntax.Tokens.ToArray(),
            syntax.Nodes.ToArray(),
            syntax.ChildIndices.ToArray(),
            syntax.RootNodeIndex);
    }

    public Syntax.Syntax ToSyntax()
    {

        return new Syntax.Syntax(RequestBuffer, Tokens, Nodes, Children, RootNodeIndex);
    }
}
