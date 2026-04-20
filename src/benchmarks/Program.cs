using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using ODataUriArenaParser.Benchmarks;

// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);


Type[] benchmarks = [typeof(ParserBenchmarks), typeof(BindingBenchmarks)];

var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkSwitcher
    .FromTypes(benchmarks)
    .Run(args, config);