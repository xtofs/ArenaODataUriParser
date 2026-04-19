using System.Buffers;
using System.Text;
using ODataUriArenaParser.Binding;
using ODataUriArenaParser.Semantic;
using ODataUriArenaParser.Syntax;

namespace ODataUriArenaParser.Tests;

public class ArenaSyntaxLifetimeTests
{
    [Fact]
    public void Parse_DoesNotAllocate()
    {

        using IMemoryOwner<byte> arena = MemoryPool<byte>.Shared.Rent(65536);
        ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes("name eq 'foo'");

        // Warm-up parse avoids counting one-time framework/JIT work in the allocation measurement.
        _ = ArenaParser.Parse(arena, input);

        long before = GC.GetAllocatedBytesForCurrentThread();
        var syntax = ArenaParser.Parse(arena, input);
        long after = GC.GetAllocatedBytesForCurrentThread();

        // The prompt shape "name eq 'foo'" should become 3 tokens, 3 syntax nodes, and 2 child links.
        Assert.Equal(0, after - before);
        Assert.Equal(3, syntax.Tokens.Length);
        Assert.Equal(3, syntax.Nodes.Length);
        Assert.Equal(2, syntax.ChildIndices.Length);
    }

    [Fact]
    public void Bind_AllocatesAndMaterializesExpectedValues()
    {

        using IMemoryOwner<byte> arena = MemoryPool<byte>.Shared.Rent(65536);
        ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes("name eq 'foo'");
        var syntax = ArenaParser.Parse(arena, input);

        long before = GC.GetAllocatedBytesForCurrentThread();
        SemanticNode semanticRoot = syntax.Bind(); ;
        long after = GC.GetAllocatedBytesForCurrentThread();

        // Binding must materialize heap objects/strings, so allocation should be positive.
        Assert.True(after - before > 0);

        Assert.True(semanticRoot is BinaryOperatorNode);
        var semantic = (BinaryOperatorNode)semanticRoot;
        Assert.True(semantic.Left is PropertyAccessNode);
        Assert.True(semantic.Right is ConstantNode);
        Assert.Equal("name", ((PropertyAccessNode)semantic.Left).Name);
        Assert.Equal("foo", ((ConstantNode)semantic.Right).Value);
        Assert.Equal(OperatorKind.Equal, semantic.Operator);
    }

    [Fact]
    public void ArenaDisposal_DoesNotBreakSemanticGraph()
    {
        SemanticNode semanticRoot;


        IMemoryOwner<byte> arena = MemoryPool<byte>.Shared.Rent(65536);
        ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes("name eq 'foo'");
        var syntax = ArenaParser.Parse(arena, input);
        semanticRoot = syntax.Bind(); ;

        arena.Dispose();


        Assert.True(semanticRoot is BinaryOperatorNode);
        var semantic = (BinaryOperatorNode)semanticRoot;
        Assert.True(semantic.Left is PropertyAccessNode);
        Assert.True(semantic.Right is ConstantNode);
        Assert.Equal("name", ((PropertyAccessNode)semantic.Left).Name);
        Assert.Equal("foo", ((ConstantNode)semantic.Right).Value);
        Assert.Equal(OperatorKind.Equal, semantic.Operator);
    }

    [Fact]
    public void Bind_ConstantExpression_ReturnsConstantNode()
    {

        using IMemoryOwner<byte> arena = MemoryPool<byte>.Shared.Rent(65536);
        ReadOnlySpan<byte> input = "'bar'"u8;
        var syntax = ArenaParser.ParseConstant(arena, input);

        SemanticNode semantic = syntax.Bind(); ;

        Assert.True(semantic is ConstantNode);
        Assert.Equal("bar", ((ConstantNode)semantic).Value);
    }

    [Fact]
    public void Bind_PropertyExpression_ReturnsPropertyAccessNode()
    {

        using IMemoryOwner<byte> arena = MemoryPool<byte>.Shared.Rent(65536);
        ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes("age");
        var syntax = ArenaParser.ParseProperty(arena, input);

        SemanticNode semantic = syntax.Bind(); ;

        // Assert.True(semantic is PropertyAccessNode);
        // Assert.Equal("age", ((PropertyAccessNode)semantic).Name);

        switch (semantic)
        {
            case PropertyAccessNode { Name: var name }:
                Assert.Equal("age", name);
                break;
            default:
                Assert.Fail($"Unexpected semantic node of type {semantic.GetType().Name}");
                break;
        }
    }

    [Fact]
    public void Parse_ArithmeticAndLogicalOperators_BindsWithCorrectPrecedence()
    {
        using IMemoryOwner<byte> arena = MemoryPool<byte>.Shared.Rent(65536);
        ReadOnlySpan<byte> input = "price add tax mul 2 gt 100 and not isDeleted"u8;

        var syntax = ArenaParser.Parse(arena, input);
        SemanticNode root = syntax.Bind();

        Assert.True(root is BinaryOperatorNode { Operator: OperatorKind.And });
        var andNode = (BinaryOperatorNode)root;

        Assert.True(andNode.Left is BinaryOperatorNode { Operator: OperatorKind.GreaterThan });
        var gtNode = (BinaryOperatorNode)andNode.Left;

        Assert.True(gtNode.Left is BinaryOperatorNode { Operator: OperatorKind.Add });
        var addNode = (BinaryOperatorNode)gtNode.Left;
        Assert.True(addNode.Left is PropertyAccessNode { Name: "price" });
        Assert.True(addNode.Right is BinaryOperatorNode { Operator: OperatorKind.Multiply });

        var mulNode = (BinaryOperatorNode)addNode.Right;
        Assert.True(mulNode.Left is PropertyAccessNode { Name: "tax" });
        Assert.True(mulNode.Right is ConstantNode { Value: "2" });
        Assert.True(gtNode.Right is ConstantNode { Value: "100" });

        Assert.True(andNode.Right is UnaryOperatorNode { Operator: OperatorKind.Not });
        var notNode = (UnaryOperatorNode)andNode.Right;
        Assert.True(notNode.Operand is PropertyAccessNode { Name: "isDeleted" });
    }

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("42")]
    [InlineData("-42")]
    [InlineData("3.14")]
    [InlineData("NaN")]
    [InlineData("INF")]
    [InlineData("-INF")]
    [InlineData("2024-03-19")]
    [InlineData("12:13:14")]
    [InlineData("2024-03-19T12:13:14Z")]
    [InlineData("duration'P1DT2H3M4S'")]
    [InlineData("guid'01234567-89ab-cdef-0123-456789abcdef'")]
    [InlineData("binary'QUJD'")]
    public void Parse_Literals_AreBoundAsConstants(string literal)
    {
        using IMemoryOwner<byte> arena = MemoryPool<byte>.Shared.Rent(65536);
        ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes(literal);

        var syntax = ArenaParser.Parse(arena, input);
        SemanticNode semantic = syntax.Bind();

        Assert.True(semantic is ConstantNode);
        string expected = literal.Length >= 2 && literal[0] == '\'' && literal[^1] == '\''
            ? literal[1..^1].Replace("''", "'")
            : literal;
        Assert.Equal(expected, ((ConstantNode)semantic).Value);
    }

    [Fact]
    public void Parse_MemberAndVariableAccess_BindsCorrectly()
    {
        using IMemoryOwner<byte> arena = MemoryPool<byte>.Shared.Rent(65536);
        ReadOnlySpan<byte> input = "Order/Customer/Name eq @p1"u8;

        var syntax = ArenaParser.Parse(arena, input);
        SemanticNode root = syntax.Bind();

        Assert.True(root is BinaryOperatorNode { Operator: OperatorKind.Equal });
        var eqNode = (BinaryOperatorNode)root;
        Assert.True(eqNode.Left is PropertyAccessNode { Name: "Order/Customer/Name" });
        Assert.True(eqNode.Right is VariableAccessNode { Name: "@p1" });
    }
}
