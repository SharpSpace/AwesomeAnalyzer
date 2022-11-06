// See https://aka.ms/new-console-template for more information

using AwesomeAnalyzer.Test;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<Benchmarks>();

[MemoryDiagnoser()]
public sealed class Benchmarks
{
    [Benchmark]
    public async Task Test1() => await new SortTest().TestSort1_Diagnostic();
    
    [Benchmark]
    public async Task Test2() => await new SortTest().TestSort2_Diagnostic();

    [Benchmark]
    public async Task TestSortMember2() => await new SortTest().TestSortMember2_Diagnostic();
}