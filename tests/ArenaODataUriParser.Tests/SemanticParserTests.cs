using System.Text;
using ODataUriParser.Binding;
using ODataUriParser.Semantic;
using ODataUriParser.Syntax;
using TreeFormatting;

namespace ODataUriParser.Tests;


public partial class SemanticParserTests
{
    // public static TheoryData<string, ExpectedNode> SemanticCases => new()
    // {
    //     {
    //         "name eq 'foo'",
    //         Expected.Binary(
    //             OperatorKind.Equal,
    //             Expected.Property("name"),
    //             Expected.Constant("foo"))
    //     },
    //     {
    //         "not isDeleted and enabled",
    //         Expected.Binary(
    //             OperatorKind.And,
    //             Expected.Unary(OperatorKind.Not, Expected.Property("isDeleted")),
    //             Expected.Property("enabled"))
    //     },
    //     {
    //         "(name eq @p1) or (price gt 10)",
    //         Expected.Binary(
    //             OperatorKind.Or,
    //             Expected.Binary(
    //                 OperatorKind.Equal,
    //                 Expected.Property("name"),
    //                 Expected.Variable("@p1")),
    //             Expected.Binary(
    //                 OperatorKind.GreaterThan,
    //                 Expected.Property("price"),
    //                 Expected.Constant("10")))
    //     },
    //     {
    //         "a and b or c",
    //         Expected.Binary(
    //             OperatorKind.Or,
    //             Expected.Binary(
    //                 OperatorKind.And,
    //                 Expected.Property("a"),
    //                 Expected.Property("b")),
    //             Expected.Property("c"))
    //     },
    //     {
    //         "a and (b or c)",
    //         Expected.Binary(
    //             OperatorKind.And,
    //             Expected.Property("a"),
    //             Expected.Binary(
    //                 OperatorKind.Or,
    //                 Expected.Property("b"),
    //                 Expected.Property("c"))
    //             )
    //     }
    // };

    [Theory]
    [ClassData(typeof(SemanticParserTestCases))]
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
        using var arena = Arena.RentArena(input);

        var syntax = Parser.Parse(arena, input);
        return syntax.Bind();
    }
}

class SemanticParserTestCases : TheoryData<string, ExpectedNode>
{
    public SemanticParserTestCases()
    {
        Add(
            "name eq 'foo'",
            Expected.Binary(
                OperatorKind.Equal,
                Expected.Property("name"),
                Expected.Constant("foo"))
        );
        Add(
            "not isDeleted and enabled",
            Expected.Binary(
                OperatorKind.And,
                Expected.Unary(OperatorKind.Not, Expected.Property("isDeleted")),
                Expected.Property("enabled"))
        );
        Add(
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
        );
        Add(
            "a and b or c",
            Expected.Binary(
                OperatorKind.Or,
                Expected.Binary(
                    OperatorKind.And,
                    Expected.Property("a"),
                    Expected.Property("b")),
                Expected.Property("c"))
        );
        Add(
            "a and (b or c)",
            Expected.Binary(
                OperatorKind.And,
                Expected.Property("a"),
                Expected.Binary(
                    OperatorKind.Or,
                    Expected.Property("b"),
                    Expected.Property("c")
                )
            )
        );
    }
}
