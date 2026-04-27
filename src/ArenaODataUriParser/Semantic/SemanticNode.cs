namespace ODataUriParser.Semantic;

using System.IO;
using System.Text;
using TreeFormatting;

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
    #region Ascii tree – uses TreeFormatter

    // Changed: replaced hand-rolled ASCII-tree logic with TreeFormatter<SemanticNode>
    // so that tree rendering is delegated to the shared TreeFormatter library.
    private static readonly TreeFormatter<SemanticNode> DefaultFormatter = new TreeFormatter<SemanticNode>(
            GetLabel,
            GetChildren);

    public void ToTree(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        formatter.Format(this, writer);
    }

    public string ToTree()
    {
        var writer = new StringWriter();
        ToTree(writer);
        writer.Flush();
        return writer.ToString();
    }

    private static readonly TreeFormatter<SemanticNode> formatter =
        new TreeFormatter<SemanticNode>(GetLabel, GetChildren);

    private static string GetLabel(SemanticNode node)
    {
        return node switch
        {
            BinaryOperatorNode binary => $"Binary {binary.Operator}",
            UnaryOperatorNode unary => $"Unary {unary.Operator}",
            ConstantNode constant => $"Const {constant.Value}",
            PropertyAccessNode property => $"Prop {property.Name}",
            VariableAccessNode variable => $"Var {variable.Name}",
            _ => throw new InvalidOperationException($"Unknown semantic node type: {node.GetType().Name}")
        };
    }

    private static IReadOnlyCollection<SemanticNode> GetChildren(SemanticNode node)
    {
        return node switch
        {
            BinaryOperatorNode binary => [binary.Left, binary.Right],
            UnaryOperatorNode unary => [unary.Operand],
            ConstantNode => [],
            PropertyAccessNode => [],
            VariableAccessNode => [],
            _ => throw new InvalidOperationException($"Unknown semantic node type: {node.GetType().Name}")
        };
    }

    #endregion
}