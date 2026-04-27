using ODataUriParser.Semantic;
using ODataUriParser.Syntax;
using TreeFormatting;

namespace ODataUriParser.Tests;

public static class Expected
{
    public static ExpectedNode Binary(OperatorKind @operator, ExpectedNode left, ExpectedNode right)
    {
        return new ExpectedBinary(@operator, left, right);
    }

    public static ExpectedNode Unary(OperatorKind @operator, ExpectedNode operand) => new ExpectedUnary(@operator, operand);

    public static ExpectedNode Constant(string value) => new ExpectedConstant(value);

    public static ExpectedNode Property(string name) => new ExpectedProperty(name);

    public static ExpectedNode Variable(string name) => new ExpectedVariable(name);
}


/// <summary>
/// Represents an expected semantic node in the tests. This is used to compare against the actual semantic nodes produced by the parser.
/// The ExpectedNode record and its derived records (ExpectedBinary, ExpectedUnary, ExpectedConstant, ExpectedProperty, ExpectedVariable)
/// provide a way to construct expected semantic trees in a readable and structured manner. 
/// Each derived record implements the Equals method to compare against the corresponding actual semantic node type. 
/// The ToTree method can be used to visualize the expected semantic tree structure for debugging purposes.
/// </summary>
public abstract record ExpectedNode : IEquatable<SemanticNode>
{
    public abstract bool Equals(SemanticNode? other);

    internal string ToTree()
    {

        TreeFormatter.Format(this, new StringWriter());
        return TreeFormatter.ToString();
    }

    private static readonly TreeFormatter<ExpectedNode> TreeFormatter =
        new TreeFormatter<ExpectedNode>(GetLabel, GetChildren);

    static string GetLabel(ExpectedNode node) => node switch
    {
        ExpectedBinary binary => $"Binary {binary.Operator}",
        ExpectedUnary unary => $"Unary {unary.Operator}",
        ExpectedConstant constant => $"Constant {constant.Value}",
        ExpectedProperty property => $"Property {property.Name}",
        ExpectedVariable variable => $"Variable {variable.Name}",
        _ => throw new InvalidOperationException($"Unknown expected node type: {node.GetType().Name}")
    };

    private static IReadOnlyCollection<ExpectedNode> GetChildren(ExpectedNode node) => node switch
    {
        ExpectedBinary binary => [binary.Left, binary.Right],
        ExpectedUnary unary => [unary.Operand],
        ExpectedConstant _ => [],
        ExpectedProperty _ => [],
        ExpectedVariable _ => [],
        _ => throw new InvalidOperationException($"Unknown expected node type: {node.GetType().Name}")
    };
}

public sealed record ExpectedBinary(
    OperatorKind Operator,
    ExpectedNode Left,
    ExpectedNode Right) : ExpectedNode
{
    public override bool Equals(SemanticNode? other)
    {
        return other is BinaryOperatorNode binary
            && binary.Operator == Operator
            && Left.Equals(binary.Left)
            && Right.Equals(binary.Right);
    }
}

public sealed record ExpectedUnary(
    OperatorKind Operator,
    ExpectedNode Operand) : ExpectedNode
{
    public override bool Equals(SemanticNode? other)
    {
        return other is UnaryOperatorNode unary
            && unary.Operator == Operator
            && Operand.Equals(unary.Operand);
    }
}

public sealed record ExpectedConstant(string Value) : ExpectedNode
{
    public override bool Equals(SemanticNode? other)
    {
        return other is ConstantNode constant
            && constant.Value == Value;
    }
}

public sealed record ExpectedProperty(string Name) : ExpectedNode
{
    public override bool Equals(SemanticNode? other)
    {
        return other is PropertyAccessNode property
            && property.Name == Name;
    }
}

public sealed record ExpectedVariable(string Name) : ExpectedNode
{
    public override bool Equals(SemanticNode? other)
    {
        return other is VariableAccessNode variable
            && variable.Name == Name;
    }
}
