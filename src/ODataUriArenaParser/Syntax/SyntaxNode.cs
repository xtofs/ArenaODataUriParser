namespace ODataUriArenaParser.Syntax;

public struct SyntaxNode
{
    public SyntaxKind Kind;
    public int FirstChild;
    public ushort ChildCount;
    public ushort Payload;

    public override string ToString()
    {
        return $"{Kind} (Payload: {Payload}, Children: {ChildCount})";
    }
}
