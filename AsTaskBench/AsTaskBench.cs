using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;

namespace HardDev;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, targetCount: 10)]
[RPlotExporter]
public class AsTaskBench
{
    [GlobalSetup]
    public void Setup()
    {
        AsTask.Initialize();
    }

    [Benchmark(Description = "ToBackgroundContext1")]
    public async ValueTask TestBc1()
    {
        await AsTask.ToBackgroundContext(() => Thread.Sleep(10));
        await Task.Yield();
    }

    [Benchmark(Description = "ToBackgroundContext2")]
    public async ValueTask TestBc2()
    {
        await AsTask.ToBackgroundContext();
        await Task.Delay(10);
        await Task.Yield();
    }

    [Benchmark(Description = "ToBackgroundContext3")]
    public async ValueTask TestBc3()
    {
        await AsTask.ToBackgroundContext();
        await Task.Yield();
    }

    [Benchmark(Description = "ToStaticThreadPool1")]
    public async ValueTask TestStp1()
    {
        await AsTask.ToStaticThreadPool(() => Thread.Sleep(10));
        await Task.Yield();
    }

    [Benchmark(Description = "ToStaticThreadPool2")]
    public async ValueTask TestStp2()
    {
        await AsTask.ToStaticThreadPool();
        await Task.Delay(10);
        await Task.Yield();
    }

    [Benchmark(Description = "ToStaticThreadPool3")]
    public async ValueTask TestStp3()
    {
        await AsTask.ToStaticThreadPool();
        await Task.Yield();
    }

    [Benchmark(Description = "ToDynamicThreadPool1")]
    public async ValueTask TestDtp1()
    {
        await AsTask.ToDynamicThreadPool(() => Thread.Sleep(10));
        await Task.Yield();
    }

    [Benchmark(Description = "ToDynamicThreadPool2")]
    public async ValueTask TestDtp2()
    {
        await AsTask.ToDynamicThreadPool();
        await Task.Delay(10);
        await Task.Yield();
    }

    [Benchmark(Description = "ToDynamicThreadPool3")]
    public async ValueTask TestDtp3()
    {
        await AsTask.ToDynamicThreadPool();
        await Task.Yield();
    }

    [Benchmark(Description = "TaskSelfSwitchBackgroundContext")]
    public async ValueTask TestSwitch1()
    {
        for (var i = 0; i < 100; i++)
        {
            await AsTask.ToBackgroundContext();
        }
    }

    [Benchmark(Description = "TaskSelfSwitchStaticThreadPool")]
    public async ValueTask TestSwitch2()
    {
        for (var i = 0; i < 100; i++)
        {
            await AsTask.ToStaticThreadPool();
        }
    }

    [Benchmark(Description = "TaskSelfSwitchDynamicThreadPool")]
    public async ValueTask TestSwitch3()
    {
        for (var i = 0; i < 100; i++)
        {
            await AsTask.ToDynamicThreadPool();
        }
    }

    [Benchmark(Description = "TaskMultiSwitch")]
    public async ValueTask TestSwitch4()
    {
        for (var i = 0; i < 100; i++)
        {
            await AsTask.ToBackgroundContext();
            await AsTask.ToStaticThreadPool();
            await AsTask.ToDynamicThreadPool();
        }
    }

    public static void Main()
    {
        foreach (var summary in BenchmarkSwitcher.FromAssembly(typeof(AsTaskBench).Assembly).Run())
        {
            MarkdownExporter.Console.ExportToLog(summary, ConsoleLogger.Default);
        }
    }
}