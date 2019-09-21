using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using HardDev;
using HardDev.Awaiter;

namespace AsTaskBench
{
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
            await AsTask.ToBackgroundContext( () => Thread.Sleep(100));
            await Task.Yield();
        }

        [Benchmark(Description = "ToBackgroundContext2")]
        public async Task TestBc2()
        {
            await AsTask.ToBackgroundContext();
            await 100;
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
            await AsTask.ToStaticThreadPool(() => Thread.Sleep(100));
            await Task.Yield();
        }

        [Benchmark(Description = "ToStaticThreadPool2")]
        public async Task TestStp2()
        {
            await AsTask.ToStaticThreadPool();
            await 100;
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
            await AsTask.ToDynamicThreadPool(() => Thread.Sleep(100));
            await Task.Yield();
        }
        
        [Benchmark(Description = "ToDynamicThreadPool2")]
        public async Task TestDtp2()
        {
            await AsTask.ToDynamicThreadPool();
            await 100;
            await Task.Yield();
        }
        
        [Benchmark(Description = "ToDynamicThreadPool3")]
        public async Task TestDtp3()
        {
            await AsTask.ToDynamicThreadPool();
            await Task.Yield();
        }
    }
}