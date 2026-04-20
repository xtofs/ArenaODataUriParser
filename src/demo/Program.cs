using System.Buffers;
using System.Text;
using ODataUriParser.Binding;
using ODataUriParser.Semantic;
using ODataUriParser.Syntax;


internal class Program
{
    private static void Main(string[] args)
    {
        var input = "(name eq 'foo' or name eq @const1) and x.active"u8;

        // warmup to ensure JIT and one-time runtime paths are not included in the measurements.
        RunAndMeasure(input, out var _, out var _);

        // actually measure
        var stats = RunAndMeasure(input, out var semantic, out var syntax);
        var (parsingAllocatedBytes, writingAllocatedBytes, bindingAllocatedBytes, totalAllocatedBytes) = stats;

        Console.WriteLine($"input  `{Encoding.UTF8.GetString(input)}`");

        Console.WriteLine();
        Console.WriteLine($"resulting semantic tree:\n {semantic.ToTree()}");
        Console.WriteLine();

        Console.WriteLine($"resulting syntactic tree:\n {syntax.ToTree()}");

        Console.WriteLine("/////////////////////////////////////");

        Console.WriteLine($"parsing allocated bytes: {parsingAllocatedBytes}");
        Console.WriteLine($"writing allocated bytes: {writingAllocatedBytes}");
        Console.WriteLine($"binding allocated bytes: {bindingAllocatedBytes}");
        Console.WriteLine($"total allocated bytes: {totalAllocatedBytes}");

    }


    static MemoryStats RunAndMeasure(ReadOnlySpan<byte> input, out SemanticNode semantic, out Syntax syntax)
    {

        using var arena = Arena.RentArena(input);

        // warmup parse to ensure JIT and one-time runtime paths are not included in the measurements.
        _ = Parser.Parse(arena, input);

        var m0 = AllocationMeasurement.Start();

        var m1 = AllocationMeasurement.Start();
        syntax = Parser.Parse(arena, input);
        var parsingAllocatedBytes = m1.Stop();

        var m2 = AllocationMeasurement.Start();
        // Measure serializer cost without Console.Out side effects.
        syntax.ToTree(TextWriter.Null);
        var syntaxWritingAllocatedBytes = m2.Stop();

        var m3 = AllocationMeasurement.Start();
        semantic = syntax.Bind();
        var bindingAllocatedBytes = m3.Stop();

        // dispose the arena after all operations are done 
        arena.Dispose();

        var totalAllocatedBytes = m0.Stop();

        return new MemoryStats(parsingAllocatedBytes, syntaxWritingAllocatedBytes, bindingAllocatedBytes, totalAllocatedBytes);
    }

    internal record MemoryStats(
        long ParsingAllocatedBytes,
        long WritingAllocatedBytes,
        long BindingAllocatedBytes,
        long TotalAllocatedBytes
    );
}

