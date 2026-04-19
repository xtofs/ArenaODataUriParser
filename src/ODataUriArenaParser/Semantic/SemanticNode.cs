namespace ODataUriArenaParser.Semantic;

// [Closed]
// [DerivedTypes(typeof(BinaryOperatorNode), typeof(ConstantNode), typeof(PropertyAccessNode))]
public abstract class SemanticNode
{

    public override string ToString()
    {
        return this switch
        {
            BinaryOperatorNode binaryNode => $"{{BinaryNode {binaryNode.Left} {binaryNode.Operator} {binaryNode.Right}}}",
            ConstantNode constantNode => $"{{ConstantNode {constantNode.Value})}}",
            PropertyAccessNode propertyAccessNode => $"{{PropertyAccessNode {propertyAccessNode.Name}}}",
            VariableAccessNode variableAccessNode => $"{{VariableAccessNode {variableAccessNode.Name}}}",
            UnaryOperatorNode unaryOperatorNode => $"{{UnaryNode {unaryOperatorNode.Operator} {unaryOperatorNode.Operand}}}",
            _ => throw new InvalidOperationException($"Unknown semantic node type: {GetType().Name}")
        };
    }
}