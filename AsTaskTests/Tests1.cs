using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTaskLib;
using HardDev.AsTaskLib.Awaiter;
using HardDev.AsTaskLib.Context;
using NUnit.Framework;

namespace AsTaskTests
{
    [TestFixture]
    public class Tests1
    {
        private readonly int _contextTests1Id;

        public Tests1()
        {
            AsTask.Initialize();
            _contextTests1Id = AsTask.CreateContext("Tests1");
        }

        [Test]
        public async Task TestSwitchingToContexts()
        {
            await AsTask.ToMainContext();
            Assert.True(AsTask.GetCurrentContextType() == AsContextType.AsyncContext);

            await AsTask.ToBackgroundContext();
            Assert.True(AsTask.GetCurrentContextType() == AsContextType.AsyncContext);

            await AsTask.ToContext(_contextTests1Id);
            Assert.True(AsTask.GetCurrentContextType() == AsContextType.AsyncContext);

            await AsTask.ToNormalThreadPool();
            Assert.True(AsTask.GetCurrentContextType() == AsContextType.NormalThreadPool);

            await AsTask.ToBlockingThreadPool();
            Assert.True(AsTask.GetCurrentContextType() == AsContextType.BlockingThreadPool);
        }

        [Test]
        public async Task TestToMainContext()
        {
            await AsTask.ToMainContext();
            Console.WriteLine(AsTask.WhereAmI());
            Assert.True(AsTask.IsMainContext(), "This is not the MainContext!");
        }

        [Test]
        public async Task TestToBackgroundContext()
        {
            await AsTask.ToBackgroundContext();
            Console.WriteLine(AsTask.WhereAmI());
            Assert.True(AsTask.IsBackgroundContext(), "This is not the BackgroundContext!");
        }

        [Test]
        public async Task TestToAsyncContext()
        {
            await AsTask.ToContext(_contextTests1Id);

            Console.WriteLine(AsTask.WhereAmI());
            Assert.True(AsTask.IsAsContext(_contextTests1Id), $"This is not the AsContext({_contextTests1Id})!");
        }

        [Test]
        public async Task TestToNormalThreadPool()
        {
            await AsTask.ToNormalThreadPool();

            Console.WriteLine(AsTask.WhereAmI());
            Assert.True(AsTask.IsNormalThreadPool(), "This is not the NormalThreadPool!");
        }

        [Test]
        public async Task TestToBlockingThreadPool1()
        {
            await AsTask.ToBlockingThreadPool();

            Console.WriteLine(AsTask.WhereAmI());
            Assert.True(AsTask.IsBlockingThreadPool(), "This is not the BlockingThreadPool!");
        }

        [Test]
        public async Task TestToBlockingThreadPool2()
        {
            await AsTask.ToMainContext();

            var stopwatch = Stopwatch.StartNew();
            var list = new List<Task>();
            for (var i = 0; i < 64; i++)
                list.Add(AsTask.ToBlockingThreadPool(() => Thread.Sleep(100)));
            await Task.WhenAll(list);
            stopwatch.Stop();

            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms <  150ms");
            Assert.True(stopwatch.ElapsedMilliseconds < 150);

            stopwatch = Stopwatch.StartNew();
            list = new List<Task>();
            for (var i = 0; i < 100; i++)
                list.Add(AsTask.ToBlockingThreadPool(() => Thread.Sleep(100)));
            await Task.WhenAll(list);
            stopwatch.Stop();

            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms > 150ms");
            Assert.True(stopwatch.ElapsedMilliseconds > 150);
        }

        [Test]
        public async Task TestBlockingThreadPoolCountTasksInQueue()
        {
            var taskScheduler = AsTask.GetBlockingTaskScheduler();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < taskScheduler.MaximumConcurrencyLevel * 2; i++)
            {
                _ = AsTask.ToBlockingThreadPool(() => { Thread.Sleep(100); });
            }

            Assert.True(taskScheduler.CountTasksInQueue == taskScheduler.MaximumConcurrencyLevel);

            await AsTask.ToBlockingThreadPool();
            stopwatch.Stop();

            var ms = stopwatch.ElapsedMilliseconds;
            Console.WriteLine(ms.ToString());
            Assert.True(ms > 200 && ms < 250);
        }

        [Test]
        public async Task TestNormalThreadPoolCountTasksInQueue()
        {
            var taskScheduler = AsTask.GetNormalTaskScheduler();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < taskScheduler.MaximumConcurrencyLevel * 2; i++)
            {
                _ = AsTask.ToNormalThreadPool(() => { Thread.Sleep(200); });
            }

            await 100;
            Assert.True(taskScheduler.CountTasksInQueue == taskScheduler.MaximumConcurrencyLevel);

            await AsTask.ToNormalThreadPool();
            stopwatch.Stop();

            var ms = stopwatch.ElapsedMilliseconds;
            Console.WriteLine(ms.ToString());
            Assert.True(ms > 400 && ms < 450);
        }

        [Test]
        public async Task TestNormalThreadPoolCountRunningTasks()
        {
            var taskScheduler = AsTask.GetNormalTaskScheduler();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < taskScheduler.MaximumConcurrencyLevel * 2; i++)
            {
                _ = AsTask.ToNormalThreadPool(() => { Thread.Sleep(100); });
            }

            await 100;
            
            Assert.True(taskScheduler.CountRunningTasks == taskScheduler.MaximumConcurrencyLevel);

            await AsTask.ToNormalThreadPool();

            Console.WriteLine(taskScheduler.CountRunningTasks);
            Assert.True(taskScheduler.CountRunningTasks == 1);

            stopwatch.Stop();

            var ms = stopwatch.ElapsedMilliseconds;
            Console.WriteLine(ms.ToString());
            Assert.True(ms > 200 && ms < 250);
        }

        [Test]
        public async Task TestDelayAwaiterMainContext()
        {
            await AsTask.ToMainContext();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(AsTask.IsMainContext(), $"This is not the MainContext: {AsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {AsTask.GetCurrentContextType()} awaiter 100ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 80 && stopwatch.ElapsedMilliseconds <= 150);
        }

        [Test]
        public async Task TestDelayAwaiterBackgroundContext()
        {
            await AsTask.ToBackgroundContext();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(AsTask.IsBackgroundContext(), $"This is not the BackgroundContext: {AsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {AsTask.GetCurrentContextType()} awaiter 100ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 80 && stopwatch.ElapsedMilliseconds <= 150);
        }

        [Test]
        public async Task TestDelayAwaiterNormalThreadPool()
        {
            await AsTask.ToNormalThreadPool();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(AsTask.IsNormalThreadPool(), $"This is not the NormalThreadPool: {AsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {AsTask.GetCurrentContextType()} awaiter 100ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 80 && stopwatch.ElapsedMilliseconds <= 150);
        }

        [Test]
        public async Task TestDelayAwaiterBlockingThreadPool()
        {
            await AsTask.ToBlockingThreadPool();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(AsTask.IsBlockingThreadPool(), $"This is not the BlockingThreadPool: {AsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {AsTask.GetCurrentContextType()} awaiter 100ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 80 && stopwatch.ElapsedMilliseconds <= 150);
        }

        [Test]
        public async Task TestMoreSwitching()
        {
            for (var i = 0; i < 1000; i++)
            {
                await TestSwitchingToContexts();
            }

            for (var i = 0; i < 1000; i++)
            {
                await AsTask.ToMainContext();
                Assert.True(AsTask.IsMainContext());
            }

            for (var i = 0; i < 1000; i++)
            {
                await AsTask.ToBackgroundContext();
                Assert.True(AsTask.IsBackgroundContext());
            }

            for (var i = 0; i < 1000; i++)
            {
                await AsTask.ToContext(_contextTests1Id);
                Assert.True(AsTask.IsAsContext(_contextTests1Id));
            }

            for (var i = 0; i < 1000; i++)
            {
                await AsTask.ToNormalThreadPool();
                Assert.True(AsTask.IsNormalThreadPool());
            }

            for (var i = 0; i < 1000; i++)
            {
                await AsTask.ToBlockingThreadPool();
                Assert.True(AsTask.IsBlockingThreadPool());
            }
        }

        [Test]
        public async Task TestAddAndRemoveCustomThread()
        {
            const string THREAD_NAME = "TestThread";
            var threadsCount = Process.GetCurrentProcess().Threads.Count;
            var id = AsTask.CreateContext(THREAD_NAME);

            await AsTask.ToContext(id);
            Assert.True(AsTask.GetCurrentContextName() == THREAD_NAME);
            Assert.True(threadsCount + 1 == Process.GetCurrentProcess().Threads.Count);

            await AsTask.ToMainContext();
            _ = AsTask.ToContext(id, () => Thread.Sleep(100));

            AsTask.RemoveContext(id);

            await 200;
            Assert.True(threadsCount == Process.GetCurrentProcess().Threads.Count);
        }

        [Test]
        public async Task Test100AddAndRemoveAsyncContext()
        {
            var threadsCount = Process.GetCurrentProcess().Threads.Count;

            for (var i = 0; i < 100; i++)
                _ = AsTask.CreateContext(i.ToString());

            await 100;
            Assert.True(threadsCount + 100 == Process.GetCurrentProcess().Threads.Count);

            for (var i = 0; i < 100; i++)
                AsTask.RemoveContext(i.ToString());

            await 100;
            Assert.True(threadsCount == Process.GetCurrentProcess().Threads.Count);
        }

        [Test]
        public async Task TestAwaitPost()
        {
            await AsTask.ToMainContext();

            var stopwatch = Stopwatch.StartNew();
            await AsTask.ToBackgroundContext(() => Thread.Sleep(1000));
            stopwatch.Stop();
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {AsTask.GetCurrentContextType()} awaiter 1000ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 1000);
        }
    }
}