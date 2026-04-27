using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ODataUriParser.Binding;
using ODataUriParser.Semantic;
using ODataUriParser.Syntax;

namespace ODataUriParser.Benchmarks;

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
        "not (price add tax gt 10 and name eq 'A') or category eq 'tools'",
        "(name eq 'foo' or (name eq @const1 and (age gt 30 or (city eq 'Berlin' and (score add 10) lt 100)))) and (x.active or (y.status eq 'pending' and z.count ge 5))"
    )]
    public string Expression { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        input = Encoding.UTF8.GetBytes(Expression);
        using var arena = Arena.RentArena(input);
        var syntax = Parser.Parse(arena, input);

        // snapshotting Syntax in one SyntaxSnapshot object to allow 
        // the parsing to happen in setup and store it in snapshot (because syntax is a ref struct.   
        snapshot = SyntaxSnapshot.FromSyntax(syntax);
    }

    [Benchmark]
    public SemanticNode Bind()
    {
        var syntax = (snapshot ?? throw new InvalidOperationException("Benchmark snapshot has not been initialized.")).ToSyntax();

        return syntax.Bind();
    }
}
