using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using ODataUriParser.Syntax;

namespace ODataUriParser.Benchmarks;

// Add a custom column to show mean time per input character
[MemoryDiagnoser]
[ShortRunJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(ConfigWithPerCharColumn))]
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
    private class ConfigWithPerCharColumn : ManualConfig
    {
        public ConfigWithPerCharColumn()
        {
            AddColumn(new MeanPerCharColumn());
        }
    }

    private class MeanPerCharColumn : IColumn
    {
        public string Id => nameof(MeanPerCharColumn);
        public string ColumnName => "Mean/Chars (ns)";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Time;
        public string Legend => "Mean time input character (ns)";
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var meanNs = summary[benchmarkCase]?.ResultStatistics?.Mean ?? 0.0;
            var expr = benchmarkCase.Parameters["Expression"] as string;
            if (string.IsNullOrEmpty(expr)) return "-";
            double len = expr.Length;
            if (len == 0) return "-";
            return (meanNs / len).ToString("N1");
        }
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public bool IsAvailable(Summary summary) => true;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsVisible(Summary summary, BenchmarkCase benchmarkCase) => true;
    }
}
