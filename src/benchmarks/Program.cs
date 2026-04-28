using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using ODataUriParser.Benchmarks;

// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);



// var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);


var config = new ManualConfig()
    .AddColumnProvider(DefaultColumnProviders.Instance)
    .AddColumn(new MeanPerCharColumn())
    .AddColumn(new ArenaSizeColumn())
    .AddLogger(QuietLogger.Default)
    .AddJob(BenchmarkDotNet.Jobs.Job.ShortRun)
    .AddExporter(DefaultExporters.AsciiDoc)
    .AddDiagnoser(MemoryDiagnoser.Default);

// .WithOrderer(new BenchmarkDotNet.Order.SummaryOrderPolicy(BenchmarkDotNet.Order.MethodOrderPolicy.FastestToSlowest));


BenchmarkRunner.Run<ParserBenchmarks>(config);
