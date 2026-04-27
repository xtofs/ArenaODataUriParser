using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using ODataUriParser.Benchmarks;

// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);


Type[] benchmarks = [typeof(ParserBenchmarks), typeof(BindingBenchmarks)];

// var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
var config = DefaultConfig.Instance
    .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
    .AddLogger(NullLogger.Instance); // ConsoleLogger.Quiet)  // or NullLogger.Instance to silence completely


BenchmarkSwitcher
    // .FromTypes(benchmarks)
    .FromTypes([typeof(ParserBenchmarks)])
    .Run(args, config);