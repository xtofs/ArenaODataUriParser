using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
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

    internal readonly ReadOnlySpan<byte> GetTokenSpan(ArenaSyntax syntax)
    {
        var tokenIndex = this.Payload;
        var (_, tokens, _, buffer, _) = syntax;

        if (tokenIndex < 0 || tokenIndex >= tokens.Length)
        {
            throw new InvalidOperationException($"Token index {tokenIndex} is out of range.");
        }

        Token token = tokens[tokenIndex];

        if (token.Kind is not TokenKind.Literal and not TokenKind.Variable and not TokenKind.Identifier)
        {
            throw new InvalidOperationException($"Cannot get text from syntax node of kind {Kind}.");
        }

        return buffer.Slice(token.Offset, token.Length);
    }

    internal readonly string GetTokenText(ArenaSyntax syntax)
    {
        return Encoding.UTF8.GetString(GetTokenSpan(syntax));
    }

    internal readonly OperatorKind GetOperatorKind()
    {
        if (Kind is not SyntaxKind.BinaryExpression and not SyntaxKind.UnaryExpression)
        {
            throw new InvalidOperationException($"Cannot get operator kind from syntax node of kind {Kind}.");
        }
        return (OperatorKind)Payload;
    }
}
