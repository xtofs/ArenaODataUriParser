using System.Text;
using ODataUriArenaParser.Binding;
using ODataUriArenaParser.Semantic;
using ODataUriArenaParser.Syntax;
using TreeFormatting;

namespace ODataUriArenaParser.Tests;

public class SemanticParserTests
{
    public static TheoryData<string, ExpectedNode> SemanticCases => new()
    {
        {
            "name eq 'foo'",
            Expected.Binary(
                OperatorKind.Equal,
                Expected.Property("name"),
                Expected.Constant("foo"))
        },
        {
            "not isDeleted and enabled",
            Expected.Binary(
                OperatorKind.And,
                Expected.Unary(OperatorKind.Not, Expected.Property("isDeleted")),
                Expected.Property("enabled"))
        },
        {
            "(name eq @p1) or (price gt 10)",
            Expected.Binary(
                OperatorKind.Or,
                Expected.Binary(
                    OperatorKind.Equal,
                    Expected.Property("name"),
                    Expected.Variable("@p1")),
                Expected.Binary(
                    OperatorKind.GreaterThan,
                    Expected.Property("price"),
                    Expected.Constant("10")))
        },
        {
            "a and b or c",
            Expected.Binary(
                OperatorKind.Or,
                Expected.Binary(
                    OperatorKind.And,
                    Expected.Property("a"),
                    Expected.Property("b")),
                Expected.Property("c"))
        },
        {
            "a and (b or x)",
            Expected.Binary(
                OperatorKind.And,
                Expected.Property("a"),
                Expected.Binary(
                    OperatorKind.Or,
                    Expected.Property("b"),
                    Expected.Property("c"))
                )
        }
    };

    [Theory]
    [MemberData(nameof(SemanticCases))]
    public void Bind_ProducesExpectedSemanticGraph(string expression, ExpectedNode expected)
    {
        var semanticRoot = BindExpression(expression);

        Assert.True(
            expected.Equals(semanticRoot),
            $"Semantic graph mismatch for expression '{expression}'.{Environment.NewLine}Actual tree:{Environment.NewLine}{semanticRoot.ToTree()} {Environment.NewLine}Expected tree:{Environment.NewLine}{expected.ToTree()}");
    }

    private static SemanticNode BindExpression(string expression)
    {
        var input = Encoding.UTF8.GetBytes(expression);
        using var arena = ArenaParser.RentArena(input);

        var syntax = ArenaParser.Parse(arena, input);
        return syntax.Bind();
    }

    #region Test expectation Nodes

    // tiny test-only DTO records that implement IEquatable<SemanticNode>
    // directly, so expected semantic shape is explicit in TheoryData
    // without a DTO transformation step.

    public abstract record ExpectedNode : IEquatable<SemanticNode>
    {
        public abstract bool Equals(SemanticNode? other);

        internal string ToTree()
        {

            TreeFormatter.Format(this, new StringWriter());
            return TreeFormatter.ToString();
        }

        private static readonly TreeFormatter<ExpectedNode> TreeFormatter = new TreeFormatter<ExpectedNode>(
            treeView: new TreeView<ExpectedNode>(
                GetText: static node => node switch
                {
                    ExpectedBinary binary => $"Binary {binary.Operator}",
                    ExpectedUnary unary => $"Unary {unary.Operator}",
                    ExpectedConstant constant => $"Constant {constant.Value}",
                    ExpectedProperty property => $"Property {property.Name}",
                    ExpectedVariable variable => $"Variable {variable.Name}",
                    _ => throw new InvalidOperationException($"Unknown expected node type: {node.GetType().Name}")
                },
                GetChildren: static node => node switch
                {
                    ExpectedBinary binary => [binary.Left, binary.Right],
                    ExpectedUnary unary => [unary.Operand],
                    ExpectedConstant _ => [],
                    ExpectedProperty _ => [],
                    ExpectedVariable _ => [],
                    _ => throw new InvalidOperationException($"Unknown expected node type: {node.GetType().Name}")
                }),
            style: TreeStyle.Unicode);
    }

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
    #endregion
}
