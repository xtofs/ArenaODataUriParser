using System.Drawing;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using ODataUriParser.Syntax;

namespace ODataUriParser.Benchmarks;

// Add a custom column to show mean time per input character
[MemoryDiagnoser]
[ShortRunJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ParserBenchmarks
{
    private byte[] input = [];

    [Params(
        "name eq 'foo'",
        "(name eq 'foo' or name eq @const1) and x.active",
        "not (price add tax gt 10 and name eq 'A') or category eq 'tools'",
        "(name eq 'foo' or (name eq @const1 and (age gt 30 or (city eq 'Berlin' and (score add 10) lt 100)))) and (x.active or (y.status eq 'pending' and z.count ge 5))"
    )]

    public string Expression { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        input = Encoding.UTF8.GetBytes(Expression);
    }

    [Benchmark]
    public int Parse()
    {

        using var arena = Arena.RentArena(input);
        var syntax = Parser.Parse(arena, input);
        return syntax.RootNodeIndex;
    }
}

