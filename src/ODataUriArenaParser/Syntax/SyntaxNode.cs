using System.Text;

namespace ODataUriArenaParser.Syntax;

public struct SyntaxNode
{
    public SyntaxKind Kind;
    public int FirstChild;
    public ushort ChildCount;
    public ushort Payload;

    public override readonly string ToString()
    {
        return $"{Kind} (Payload: {Payload}, Children: {ChildCount})";
    }

    internal readonly ReadOnlySpan<byte> GetTokenSpan(TokenKind expectedKind, ArenaSyntax syntax)
    {
        var tokenIndex = this.Payload;
        var (_, tokens, _, buffer, _) = syntax;

        if (tokenIndex < 0 || tokenIndex >= tokens.Length)
        {
            throw new InvalidOperationException($"Token index {tokenIndex} is out of range.");
        }

        Token token = tokens[tokenIndex];
        if (token.Kind != expectedKind)
        {
            throw new InvalidOperationException($"Expected token kind {expectedKind} but found {token.Kind}.");
        }

        return buffer.Slice(token.Offset, token.Length);
    }

    internal readonly string GetTokenText(TokenKind expectedKind, ArenaSyntax syntax)
    {
        return Encoding.UTF8.GetString(GetTokenSpan(expectedKind, syntax));
    }
}
