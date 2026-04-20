using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ODataUriArenaParser.Syntax;

namespace ODataUriArenaParser.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ParserBenchmarks
{
    private byte[] input = [];

    [Params(
        "name eq 'foo'",
        "(name eq 'foo' or name eq @const1) and x.active",
        "not (price add tax gt 10 and name eq 'A') or category eq 'tools'")]

    public string Expression { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        input = Encoding.UTF8.GetBytes(Expression);
    }

    [Benchmark]
    public int Parse()
    {

        using var arena = ArenaParser.RentArena(input);
        var syntax = ArenaParser.Parse(arena, input);
        return syntax.RootNodeIndex;
    }
}
