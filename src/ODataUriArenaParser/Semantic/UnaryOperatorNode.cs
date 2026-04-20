using ODataUriParser.Syntax;

namespace ODataUriParser.Semantic;

public sealed class UnaryOperatorNode(OperatorKind @operator, SemanticNode operand) : SemanticNode
{
    public OperatorKind Operator { get; } = @operator;

    public SemanticNode Operand { get; } = operand;
}
