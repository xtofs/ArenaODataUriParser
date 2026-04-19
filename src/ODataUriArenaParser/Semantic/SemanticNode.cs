namespace ODataUriArenaParser.Semantic;

using System.IO;
using System.Text;

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
    #region Ascii tree https://andrewlock.net/creating-an-ascii-art-tree-in-csharp/

    // private const string LastChildPrefix = "+-- ";
    // private const string NonLastChildPrefix = "|-- ";
    // private const string LastChildIndent = "    ";
    // private const string NonLastChildIndent = "|   ";


    private const string LastChildPrefix = " └─";
    private const string NonLastChildPrefix = " ├─";
    private const string LastChildIndent = "   ";
    private const string NonLastChildIndent = " │ ";

    public void ToTree(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        WriteNode(this, writer, string.Empty, isLast: true, isRoot: true);

        static void WriteNode(SemanticNode node, TextWriter writer, string indent, bool isLast, bool isRoot)
        {
            if (!isRoot)
            {
                writer.Write(indent);
                writer.Write(isLast ? LastChildPrefix : NonLastChildPrefix);
            }

            writer.WriteLine(GetLabel(node));

            var childCount = GetChildCount(node);
            if (childCount == 0)
            {
                return;
            }

            var childIndent = isRoot ? string.Empty : indent + (isLast ? LastChildIndent : NonLastChildIndent);
            for (var i = 0; i < childCount; i++)
            {
                var child = GetChild(node, i);
                WriteNode(child, writer, childIndent, i == childCount - 1, isRoot: false);
            }
        }

        static string GetLabel(SemanticNode node)
        {
            return node switch
            {
                // BinaryOperatorNode binary => "Binary (" + binary.Operator + ')',
                // UnaryOperatorNode unary => "Unary (" + unary.Operator + ')',
                // ConstantNode constant => "Constant: " + constant.Value,
                // PropertyAccessNode property => "Property: " + property.Name,
                // VariableAccessNode variable => "Variable: " + variable.Name,
                BinaryOperatorNode binary => $"{binary.Operator}",
                UnaryOperatorNode unary => $"{unary.Operator}",
                ConstantNode constant => $"{constant.Value}",
                PropertyAccessNode property => $"property {property.Name}",
                VariableAccessNode variable => $"variable {variable.Name}",
                _ => throw new InvalidOperationException($"Unknown semantic node type: {node.GetType().Name}")
            };
        }

        static int GetChildCount(SemanticNode node)
        {
            return node switch
            {
                BinaryOperatorNode => 2,
                UnaryOperatorNode => 1,
                ConstantNode => 0,
                PropertyAccessNode => 0,
                VariableAccessNode => 0,
                _ => throw new InvalidOperationException($"Unknown semantic node type: {node.GetType().Name}")
            };
        }

        static SemanticNode GetChild(SemanticNode node, int index)
        {
            return node switch
            {
                BinaryOperatorNode binary when index == 0 => binary.Left,
                BinaryOperatorNode binary when index == 1 => binary.Right,
                UnaryOperatorNode unary when index == 0 => unary.Operand,
                _ => throw new InvalidOperationException($"Node {node.GetType().Name} has no child at index {index}.")
            };
        }
    }

    public string ToTree()
    {
        var builder = new StringBuilder();
        using var writer = new StringWriter(builder);
        ToTree(writer);
        return builder.ToString();
    }
    #endregion
}