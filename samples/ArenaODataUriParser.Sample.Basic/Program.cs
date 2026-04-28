using System.Text;
using ODataUriParser.Binding;
using ODataUriParser.Semantic;
using ODataUriParser.Syntax;

using static Colors;

internal class Program
{
    private static void Main(string[] args)
    {

        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }

        // Simple input example
        RunDemo("'foo'"u8);

        // Medium complexity input example
        RunDemo("(name eq 'foo' or name eq @const1) and x.active"u8);

        // More complicated input example        
        RunDemo("(name eq 'foo' or (name eq @const1 and (age gt 30 or (city eq 'Berlin' and (score add 10) lt 100)))) and (x.active or (y.status eq 'pending' and z.count ge 5))"u8);
    }

    public static void RunDemo(ReadOnlySpan<byte> input)
    {
        Console.WriteLine("/////////////////////////////////////");

        // warmup to ensure JIT and one-time runtime paths are not included in the measurements.
        RunAndMeasure(input, out var _, out var _);

        // actually measure
        var stats = RunAndMeasure(input, out var semantic, out var syntax);
        var (parsingAllocatedBytes, bindingAllocatedBytes, writingAllocatedBytes) = stats;

        Console.WriteLine($"{Red}input:{Reset}\n`{Encoding.UTF8.GetString(input)}`");

        Console.WriteLine();
        Console.WriteLine($"{Yellow}resulting syntactic tree:{Reset}\n{syntax.ToTree()}");
        Console.WriteLine();

        Console.WriteLine($"{Yellow}resulting semantic tree:{Reset}\n{semantic.ToTree()}");


        Console.WriteLine($"input:   {input.Length} bytes");
        Console.WriteLine($"parsing: {parsingAllocatedBytes} bytes");
        Console.WriteLine($"binding: {bindingAllocatedBytes} bytes");
        Console.WriteLine($"writing: {writingAllocatedBytes} bytes");

        Console.WriteLine("");
    }


    static MemoryStats RunAndMeasure(ReadOnlySpan<byte> input, out Syntax syntax, out SemanticNode semantic)
    {

        using var arena = Arena.RentArena(input);

        // warmup parse to ensure JIT and one-time runtime paths are not included in the measurements.
        _ = Parser.Parse(arena, input);

        var m0 = AllocationMeasurement.Start();

        var m1 = AllocationMeasurement.Start();
        syntax = Parser.Parse(arena, input);
        var parsingAllocatedBytes = m1.Stop();


        var m2 = AllocationMeasurement.Start();
        semantic = syntax.Bind();
        var bindingAllocatedBytes = m2.Stop();

        var m3 = AllocationMeasurement.Start();
        // Measure serializer cost of syntax without Console.Out side effects.
        syntax.ToTree(TextWriter.Null);
        var syntaxWritingAllocatedBytes = m3.Stop();

        // dispose the arena after all operations are done 
        arena.Dispose();

        var totalAllocatedBytes = m0.Stop();

        return new MemoryStats(parsingAllocatedBytes, bindingAllocatedBytes, syntaxWritingAllocatedBytes);
    }

    internal record MemoryStats(
        long ParsingAllocatedBytes,
        long BindingAllocatedBytes,
        long WritingAllocatedBytes
    );
}
