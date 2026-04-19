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
        ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes("'bar'");
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
}
