using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ODataUriArenaParser.Binding;
using ODataUriArenaParser.Semantic;
using ODataUriArenaParser.Syntax;

namespace ODataUriArenaParser.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BindingBenchmarks
{
    private byte[] input = [];
    private SyntaxSnapshot? snapshot;

    [Params(
        "name eq 'foo'",
        "(name eq 'foo' or name eq @const1) and x.active",
        "not (price add tax gt 10 and name eq 'A') or category eq 'tools'")]
    public string Expression { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        input = Encoding.UTF8.GetBytes(Expression);
        using var arena = ArenaParser.RentArena(input);
        var syntax = ArenaParser.Parse(arena, input);

        // snapshotting ArenaSyntax in one SyntaxSnapshot object 
        // to allow the parsing to happen in setup and store it in snapshot (because syntax can't be a field/property).   
        snapshot = SyntaxSnapshot.FromSyntax(syntax);
    }

    [Benchmark]
    public SemanticNode Bind()
    {
        var syntax = (snapshot ?? throw new InvalidOperationException("Benchmark snapshot has not been initialized.")).ToArenaSyntax();

        return syntax.Bind();
    }
}
