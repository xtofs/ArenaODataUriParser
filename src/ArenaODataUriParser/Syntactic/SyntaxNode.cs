using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ODataUriParser.Syntax;

public struct SyntaxNode
{
    // Kind is the type of the syntax node, e.g. BinaryOperation, Literal, Identifier, etc.
    public SyntaxKind Kind;

    // ChildCount is the number of children of this node. 
    // The actual child nodes can be found in the Syntax.Children array, starting at index FirstChild.
    public ushort ChildCount;

    // FirstChild is the index in the Syntax.Children array where the children of this node start.
    public int FirstChild;

    // Payload is a flexible field that can be used to store additional information about the node.
    // For example, for literal nodes, it can store the index of the token in the token array.
    public ushort Payload;

    public override readonly string ToString()
    {
        return $"{Kind} (Payload: {Payload}, Children: {ChildCount})";
    }

    internal readonly ReadOnlySpan<byte> GetTokenSpan(Syntax syntax)
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

    internal readonly string GetTokenText(Syntax syntax)
    {
        return Encoding.UTF8.GetString(GetTokenSpan(syntax));
    }

    internal readonly OperatorKind GetOperatorKind()
    {
        if (Kind is not SyntaxKind.BinaryOperation and not SyntaxKind.UnaryExpression)
        {
            throw new InvalidOperationException($"Cannot get operator kind from syntax node of kind {Kind}.");
        }
        return (OperatorKind)Payload;
    }

    internal readonly IReadOnlyList<SyntaxNode> GetChildren(Syntax syntax)
    {
        if (ChildCount == 0)
        {
            return [];
        }

        var (nodes, _, children, _, _) = syntax;

        var result = new SyntaxNode[ChildCount];
        for (int i = 0; i < ChildCount; i++)
        {
            int childIndex = children[FirstChild + i];
            if (childIndex < 0 || childIndex >= nodes.Length)
            {
                throw new InvalidOperationException($"Child index {childIndex} is out of range.");
            }
            result[i] = nodes[childIndex];
        }
        return result;
    }
}
