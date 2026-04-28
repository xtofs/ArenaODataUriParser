namespace ODataUriParser.Benchmarks;

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;


internal class MeanPerCharColumn : IColumn
{
    public string Id => nameof(MeanPerCharColumn);
    public string ColumnName => "Per Char (ns)";
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
}

