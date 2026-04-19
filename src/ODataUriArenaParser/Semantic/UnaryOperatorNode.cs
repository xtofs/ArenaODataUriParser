using ODataUriArenaParser.Syntax;

namespace ODataUriArenaParser.Semantic;

public sealed class UnaryOperatorNode(OperatorKind @operator, SemanticNode operand) : SemanticNode
{
    public OperatorKind Operator { get; } = @operator;

    public SemanticNode Operand { get; } = operand;
}
