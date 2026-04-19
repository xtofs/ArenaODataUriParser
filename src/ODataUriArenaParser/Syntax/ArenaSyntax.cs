namespace ODataUriArenaParser.Syntax;

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
}
