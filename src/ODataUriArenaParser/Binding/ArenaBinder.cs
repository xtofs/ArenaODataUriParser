using System.Text;
using ODataUriArenaParser.Semantic;
using ODataUriArenaParser.Syntax;

namespace ODataUriArenaParser.Binding;

public static class ArenaBinder
{
    public static SemanticNode Bind(this ArenaSyntax syntax)
    {
        return BindNode(syntax.RootNodeIndex, syntax);
    }

    private static SemanticNode BindNode(int nodeIndex, ArenaSyntax syntax)
    {
        var (nodes, _, _, _, _) = syntax;

        if (nodeIndex < 0 || nodeIndex >= nodes.Length)
        {
            throw new InvalidOperationException($"Node index {nodeIndex} is out of range.");
        }

        SyntaxNode node = nodes[nodeIndex];
        return node.Kind switch
        {
            SyntaxKind.BinaryExpression => BindBinary(node, syntax),
            SyntaxKind.PropertyAccess => BindPropertyAccess(syntax, node),
            SyntaxKind.Constant => BindConstant(syntax, node),
            SyntaxKind.VariableAccess => BindVariableAccess(syntax, node),
            SyntaxKind.UnaryExpression => BindUnary(node, syntax),
            _ => throw new InvalidOperationException($"Unsupported syntax kind: {node.Kind}.")
        };
    }

    private static ConstantNode BindConstant(ArenaSyntax syntax, SyntaxNode node)
    {
        return new ConstantNode(node.GetTokenText(syntax));
    }

    private static PropertyAccessNode BindPropertyAccess(ArenaSyntax syntax, SyntaxNode node)
    {
        return new PropertyAccessNode(node.GetTokenText(syntax));
    }

    private static VariableAccessNode BindVariableAccess(ArenaSyntax syntax, SyntaxNode node)
    {
        return new VariableAccessNode(node.GetTokenText(syntax));
    }

    private static UnaryOperatorNode BindUnary(SyntaxNode node, ArenaSyntax syntax)
    {
        if (node.ChildCount != 1)
        {
            throw new InvalidOperationException("Unary expression must have exactly one child.");
        }

        var (_, _, children, _, _) = syntax;
        if (node.FirstChild < 0 || node.FirstChild >= children.Length)
        {
            throw new InvalidOperationException("Unary expression child range is invalid.");
        }

        int operandNodeIndex = children[node.FirstChild];
        SemanticNode operand = BindNode(operandNodeIndex, syntax);
        return new UnaryOperatorNode((OperatorKind)node.Payload, operand);
    }

    private static BinaryOperatorNode BindBinary(SyntaxNode node, ArenaSyntax syntax)
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

        SemanticNode left = BindNode(leftNodeIndex, syntax);
        SemanticNode right = BindNode(rightNodeIndex, syntax);

        return new BinaryOperatorNode(left, right, (OperatorKind)node.Payload);
    }

    [Obsolete("use node.GetTokenText extension method instead")]
    private static string ReadTokenText(int tokenIndex, TokenKind expectedKind, ArenaSyntax syntax)
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

        return Encoding.UTF8.GetString(buffer.Slice(token.Offset, token.Length));
    }
}

