using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HardDev;
using HardDev.Awaiter;
using HardDev.Context;
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
            await AsTask.ToBackgroundContext();
            Assert.True(AsTask.GetCurrentContextType() == ThreadContextType.ThreadContext);

            await AsTask.ToContext(_contextTests1Id);
            Assert.True(AsTask.GetCurrentContextType() == ThreadContextType.ThreadContext);

            await AsTask.ToStaticThreadPool();
            Assert.True(AsTask.GetCurrentContextType() == ThreadContextType.StaticThreadPool);

            await AsTask.ToDynamicThreadPool();
            Assert.True(AsTask.GetCurrentContextType() == ThreadContextType.DynamicThreadPool);
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
            Assert.True(AsTask.IsThreadContext(_contextTests1Id), $"This is not the AsContext({_contextTests1Id})!");
        }

        [Test]
        public async Task TestToStaticThreadPool()
        {
            await AsTask.ToStaticThreadPool();

            Console.WriteLine(AsTask.WhereAmI());
            Assert.True(AsTask.IsStaticThreadPool(), "This is not the StaticThreadPool!");
        }

        [Test]
        public async Task TestToDynamicThreadPool1()
        {
            await AsTask.ToDynamicThreadPool();

            Console.WriteLine(AsTask.WhereAmI());
            Assert.True(AsTask.IsDynamicThreadPool(), "This is not the DynamicThreadPool!");
        }

        [Test]
        public async Task TestToDynamicThreadPool2()
        {
            await AsTask.ToMainContext();

            var stopwatch = Stopwatch.StartNew();
            var list = new List<Task>();
            for (var i = 0; i < 64; i++)
                list.Add(AsTask.ToDynamicThreadPool(() => Thread.Sleep(100)));
            await Task.WhenAll(list);
            stopwatch.Stop();

            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms <  150ms");
            Assert.True(stopwatch.ElapsedMilliseconds < 150);

            stopwatch = Stopwatch.StartNew();
            list = new List<Task>();
            for (var i = 0; i < 100; i++)
                list.Add(AsTask.ToDynamicThreadPool(() => Thread.Sleep(100)));
            await Task.WhenAll(list);
            stopwatch.Stop();

            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms > 150ms");
            Assert.True(stopwatch.ElapsedMilliseconds > 150);
        }

        [Test]
        public async Task TestDynamicThreadPoolCountTasksInQueue()
        {
            var taskScheduler = AsTask.GetDynamicTaskScheduler();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < taskScheduler.MaximumConcurrencyLevel * 2; i++)
            {
                _ = AsTask.ToDynamicThreadPool(() => { Thread.Sleep(100); });
            }

            Assert.True(taskScheduler.CountTasksInQueue == taskScheduler.MaximumConcurrencyLevel);

            await AsTask.ToDynamicThreadPool();
            stopwatch.Stop();

            var ms = stopwatch.ElapsedMilliseconds;
            Console.WriteLine(ms.ToString());
            Assert.True(ms > 200 && ms < 300);
        }

        [Test]
        public async Task TestStaticThreadPoolCountTasksInQueue()
        {
            var taskScheduler = AsTask.GetStaticTaskScheduler();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < taskScheduler.MaximumConcurrencyLevel * 2; i++)
            {
                _ = AsTask.ToStaticThreadPool(() => { Thread.Sleep(200); });
            }

            await 100;
            Assert.True(taskScheduler.CountTasksInQueue == taskScheduler.MaximumConcurrencyLevel);

            await AsTask.ToStaticThreadPool();
            stopwatch.Stop();

            var ms = stopwatch.ElapsedMilliseconds;
            Console.WriteLine(ms.ToString());
            Assert.True(ms >= 400 && ms <= 450);
        }

        [Test]
        public async Task TestStaticThreadPoolCountRunningTasks()
        {
            var taskScheduler = AsTask.GetStaticTaskScheduler();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < taskScheduler.MaximumConcurrencyLevel * 2; i++)
            {
                _ = AsTask.ToStaticThreadPool(() => { Thread.Sleep(100); });
            }

            await 100;

            Assert.True(taskScheduler.CountExecutableTasks == taskScheduler.MaximumConcurrencyLevel);

            await AsTask.ToStaticThreadPool();

            Console.WriteLine(taskScheduler.CountExecutableTasks);
            Assert.True(taskScheduler.CountExecutableTasks == 1);

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
        public async Task TestDelayAwaiterStaticThreadPool()
        {
            await AsTask.ToStaticThreadPool();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(AsTask.IsStaticThreadPool(), $"This is not the StaticThreadPool: {AsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {AsTask.GetCurrentContextType()} awaiter 100ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 80 && stopwatch.ElapsedMilliseconds <= 150);
        }

        [Test]
        public async Task TestDelayAwaiterDynamicThreadPool()
        {
            await AsTask.ToDynamicThreadPool();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(AsTask.IsDynamicThreadPool(), $"This is not the DynamicThreadPool: {AsTask.WhereAmI()}");
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
                Assert.True(AsTask.IsThreadContext(_contextTests1Id));
            }

            for (var i = 0; i < 1000; i++)
            {
                await AsTask.ToStaticThreadPool();
                Assert.True(AsTask.IsStaticThreadPool());
            }

            for (var i = 0; i < 1000; i++)
            {
                await AsTask.ToDynamicThreadPool();
                Assert.True(AsTask.IsDynamicThreadPool());
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