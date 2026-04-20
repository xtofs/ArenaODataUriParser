using ODataUriArenaParser.Binding;
using ODataUriArenaParser.Semantic;
using ODataUriArenaParser.Syntax;


internal class Program
{
    private static void Main(string[] args)
    {
        var input = "(name eq 'foo' or name eq @const1) and x.active"u8;

        // warmup to ensure JIT and one-time runtime paths are not included in the measurements.
        _ = Run(input);

        // actually measure
        var stats = Run(input);


        var (parsingAllocatedBytes, writingAllocatedBytes, bindingAllocatedBytes, totalAllocatedBytes, semantic) = stats;
        Console.WriteLine($"parsing allocated bytes: {parsingAllocatedBytes}");
        Console.WriteLine($"writing allocated bytes: {writingAllocatedBytes}");
        Console.WriteLine($"binding allocated bytes: {bindingAllocatedBytes}");
        Console.WriteLine($"total allocated bytes: {totalAllocatedBytes}");
        Console.WriteLine($"resulting semantic graph:\n {semantic.ToTree()}");
        Console.WriteLine();

    }


    static MemoryStats Run(ReadOnlySpan<byte> input)
    {

        using var arena = ArenaParser.RentArena(input);

        _ = ArenaParser.Parse(arena, input);

        var m0 = AllocationMeasurement.Start();

        var m1 = AllocationMeasurement.Start();
        var syntax = ArenaParser.Parse(arena, input);
        var parsingAllocatedBytes = m1.Stop();

        var m2 = AllocationMeasurement.Start();
        // Measure serializer cost without Console.Out side effects.
        syntax.Write(TextWriter.Null);
        var writingAllocatedBytes = m2.Stop();

        // this messes up the allocation measurement for the binder, 
        // for real measurments please comment out the syntx tree writing .
        // Console.WriteLine("/////////////////////////////////////");
        // syntax.ToTree(Console.Out);
        // Console.WriteLine("/////////////////////////////////////");

        var m3 = AllocationMeasurement.Start();
        var semantic = syntax.Bind();
        var bindingAllocatedBytes = m3.Stop();

        // dispose the arena after all operations are done 
        arena.Dispose();

        var totalAllocatedBytes = m0.Stop();

        return new MemoryStats(parsingAllocatedBytes, writingAllocatedBytes, bindingAllocatedBytes, totalAllocatedBytes, semantic);
    }

    internal record MemoryStats(
        long ParsingAllocatedBytes,
        long WritingAllocatedBytes,
        long BindingAllocatedBytes,
        long TotalAllocatedBytes,
        SemanticNode Semantic
    );
}

