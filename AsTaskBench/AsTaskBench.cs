using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;
using HardDev;
using HardDev.Awaiter;

namespace AsTaskBench
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, targetCount: 10)]
    public class AsTaskBench
    {
        public AsTaskBench()
        {
            AsTask.Initialize();
        }

        [Benchmark(Description = "ToBackgroundContext1")]
        public async Task TestBc1()
        {
            await AsTask.ToBackgroundContext(() => Thread.Sleep(10));
            await Task.Yield();
        }

        [Benchmark(Description = "ToBackgroundContext2")]
        public async Task TestBc2()
        {
            await AsTask.ToBackgroundContext();
            await 10;
            await Task.Yield();
        }

        [Benchmark(Description = "ToBackgroundContext3")]
        public async Task TestBc3()
        {
            await AsTask.ToBackgroundContext();
            await Task.Yield();
        }

        [Benchmark(Description = "ToStaticThreadPool1")]
        public async Task TestStp1()
        {
            await AsTask.ToStaticThreadPool(() => Thread.Sleep(10));
            await Task.Yield();
        }

        [Benchmark(Description = "ToStaticThreadPool2")]
        public async Task TestStp2()
        {
            await AsTask.ToStaticThreadPool();
            await 10;
            await Task.Yield();
        }

        [Benchmark(Description = "ToStaticThreadPool3")]
        public async Task TestStp3()
        {
            await AsTask.ToStaticThreadPool();
            await Task.Yield();
        }

        [Benchmark(Description = "ToDynamicThreadPool1")]
        public async Task TestDtp1()
        {
            await AsTask.ToDynamicThreadPool(() => Thread.Sleep(10));
            await Task.Yield();
        }

        [Benchmark(Description = "ToDynamicThreadPool2")]
        public async Task TestDtp2()
        {
            await AsTask.ToDynamicThreadPool();
            await 10;
            await Task.Yield();
        }

        [Benchmark(Description = "ToDynamicThreadPool3")]
        public async Task TestDtp3()
        {
            await AsTask.ToDynamicThreadPool();
            await Task.Yield();
        }

        [Benchmark(Description = "TaskSelfSwitchBackgroundContext")]
        public async Task TestSwitch1()
        {
            for (var i = 0; i < 100; i++)
            {
                await AsTask.ToBackgroundContext();
            }
        }

        [Benchmark(Description = "TaskSelfSwitchStaticThreadPool")]
        public async Task TestSwitch2()
        {
            for (var i = 0; i < 100; i++)
            {
                await AsTask.ToStaticThreadPool();
            }
        }

        [Benchmark(Description = "TaskSelfSwitchDynamicThreadPool")]
        public async Task TestSwitch3()
        {
            for (var i = 0; i < 100; i++)
            {
                await AsTask.ToDynamicThreadPool();
            }
        }

        [Benchmark(Description = "TaskMultiSwitch")]
        public async Task TestSwitch4()
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
            var bench = BenchmarkConverter.TypeToBenchmarks(typeof(AsTaskBench));
            var summary = BenchmarkRunnerCore.Run(bench, _ => InProcessToolchain.Instance);
            MarkdownExporter.Console.ExportToLog(summary, ConsoleLogger.Default);
        }
    }
}