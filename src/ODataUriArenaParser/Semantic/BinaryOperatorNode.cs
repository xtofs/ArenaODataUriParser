using ODataUriParser.Syntax;

namespace ODataUriParser.Semantic;


public sealed class BinaryOperatorNode(SemanticNode left, SemanticNode right, OperatorKind @operator) : SemanticNode
{
    public SemanticNode Left { get; } = left;

    public SemanticNode Right { get; } = right;

    public OperatorKind Operator { get; } = @operator;


}
