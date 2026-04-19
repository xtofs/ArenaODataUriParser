using System.IO;
using System.Text;
using ODataUriArenaParser.Syntax;

namespace ODataUriArenaParser.Binding;

public static class ArenaSerializer
{
    public static string Serialize(this ArenaSyntax syntax)
    {
        using var writer = new StringWriter();
        Write(syntax, writer);
        return writer.ToString();
    }

    public static void Write(this ArenaSyntax syntax, TextWriter writer)
    {

        WriteNode(syntax.RootNodeIndex, syntax, writer);
    }

    public static void WriteLine(this ArenaSyntax syntax, TextWriter writer)
    {
        WriteNode(syntax.RootNodeIndex, syntax, writer);
        writer.WriteLine();
    }

    // // Kept for compatibility while routing through TextWriter-based implementation.
    // public static void Serialize(this ArenaSyntax syntax, StringBuilder builder)
    // {
    //     using var writer = new StringWriter(builder);
    //     Serialize(syntax, writer);
    // }

    private static void WriteNode(int nodeIndex, ArenaSyntax syntax, TextWriter writer)
    {
        var (nodes, _, _, _, _) = syntax;

        if (nodeIndex < 0 || nodeIndex >= nodes.Length)
        {
            throw new InvalidOperationException($"Node index {nodeIndex} is out of range.");
        }

        SyntaxNode node = nodes[nodeIndex];
        switch (node.Kind)
        {
            case SyntaxKind.BinaryExpression:
                WriteBinary(node, syntax, writer);
                break;
            case SyntaxKind.PropertyAccess:
                WritePropertyAccess(syntax, node, writer);
                break;
            case SyntaxKind.Constant:
                WriteConstant(syntax, node, writer);
                break;
            default:
                throw new InvalidOperationException($"Unsupported syntax kind: {node.Kind}.");
        }
    }

    private static void WriteBinary(SyntaxNode node, ArenaSyntax syntax, TextWriter writer)
    {
        if (node.ChildCount != 2)
        {
            throw new InvalidOperationException("Binary expression must have exactly two children.");
        }

        var (_, _, children, _, _) = syntax;

        if (node.FirstChild < 0 || node.FirstChild + 1 >= children.Length)
        {
            throw new InvalidOperationException("Binary expression child range is invalid.");
        }

        int leftNodeIndex = children[node.FirstChild];
        int rightNodeIndex = children[node.FirstChild + 1];

        WriteNode(leftNodeIndex, syntax, writer);
        switch ((OperatorKind)node.Payload)
        {
            case OperatorKind.Equal:
                writer.Write(" eq ");
                break;
            default:
                throw new InvalidOperationException($"Unsupported operator kind: {(OperatorKind)node.Payload}.");
        }

        WriteNode(rightNodeIndex, syntax, writer);
    }

    private static void WriteConstant(ArenaSyntax syntax, SyntaxNode node, TextWriter writer)
    {
        WriteTokenText(node.Payload, TokenKind.StringLiteral, syntax, writer);
    }

    private static void WritePropertyAccess(ArenaSyntax syntax, SyntaxNode node, TextWriter writer)
    {
        writer.Write('"');
        WriteTokenText(node.Payload, TokenKind.Identifier, syntax, writer);
        writer.Write('"');
    }

    private static void WriteTokenText(int tokenIndex, TokenKind expectedKind, ArenaSyntax syntax, TextWriter writer)
    {
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

        writer.Write(buffer.Slice(token.Offset, token.Length), Encoding.UTF8);
    }
}
