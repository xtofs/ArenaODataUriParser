using System.Text;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using ODataUriParser.Syntax;

namespace ODataUriParser.Benchmarks;

internal class ArenaSizeColumn : IColumn
{
    public string Id => nameof(ArenaSizeColumn);
    public string ColumnName => "Arena Size";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 0;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Size;
    public string Legend => "Size of the allocated Arena (bytes)";
    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        var expr = benchmarkCase.Parameters["Expression"] as string;
        var input = Encoding.UTF8.GetBytes(expr ?? string.Empty);
        var size = Arena.GetArenaSize(input);
        return size.ToString();
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}

