using System.Text;
using ODataUriArenaParser.Binding;
using ODataUriArenaParser.Semantic;
using ODataUriArenaParser.Syntax;


internal class Program
{
    private static void Main(string[] args)
    {
        var input = "(name eq 'foo' or name eq @const1) and x.active"u8;

        // warmup to ensure JIT and one-time runtime paths are not included in the measurements.
        Run(input, out var _, out var _);

        // actually measure
        var stats = Run(input, out var semantic, out var syntax);
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


    static MemoryStats Run(ReadOnlySpan<byte> input, out SemanticNode semantic, out ArenaSyntax syntax)
    {

        using var arena = ArenaParser.RentArena(input);

        _ = ArenaParser.Parse(arena, input);

        var m0 = AllocationMeasurement.Start();

        var m1 = AllocationMeasurement.Start();
        syntax = ArenaParser.Parse(arena, input);
        var parsingAllocatedBytes = m1.Stop();

        var m2 = AllocationMeasurement.Start();
        // Measure serializer cost without Console.Out side effects.
        syntax.Write(TextWriter.Null);
        var writingAllocatedBytes = m2.Stop();

        var m3 = AllocationMeasurement.Start();
        semantic = syntax.Bind();
        var bindingAllocatedBytes = m3.Stop();

        // dispose the arena after all operations are done 
        arena.Dispose();

        var totalAllocatedBytes = m0.Stop();

        return new MemoryStats(parsingAllocatedBytes, writingAllocatedBytes, bindingAllocatedBytes, totalAllocatedBytes);
    }

    internal record MemoryStats(
        long ParsingAllocatedBytes,
        long WritingAllocatedBytes,
        long BindingAllocatedBytes,
        long TotalAllocatedBytes
    );
}

