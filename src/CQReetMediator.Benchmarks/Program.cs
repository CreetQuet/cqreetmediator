using BenchmarkDotNet.Running;
using CQReetMediator.Benchmarks;

BenchmarkRunner.Run<MediatorBenchmarks>();
//await ComparisonLoadTest.RunAsync();
//await MassiveLoadTest.RunAsync();