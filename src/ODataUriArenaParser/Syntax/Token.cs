namespace ODataUriArenaParser.Syntax;

public struct Token
{
    public TokenKind Kind;
    public int Offset;
    public int Length;

    override public string ToString()
    {
        return $"{Kind} @ {Offset} ({Length} bytes)";
    }
}
