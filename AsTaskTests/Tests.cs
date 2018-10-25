using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HardDev.AsTask;
using HardDev.AsTask.Awaiters;
using HardDev.AsTask.Context;
using NUnit.Framework;

namespace AsTaskTests
{
    [TestFixture]
    public class Tests
    {
        private readonly int _newContextThreadId;

        public Tests()
        {
            QAsTask.Initialize();
            _newContextThreadId = QAsTask.CreateAsyncContextThread("NewContextThread");
        }

        [Test]
        public async Task TestSwitchingToContexts()
        {
            await QAsTask.ToMainThread();
            Assert.True(QAsTask.GetCurrentContextType() == QAsyncContextType.MainThread);

            await QAsTask.ToBackgroundThread();
            Assert.True(QAsTask.GetCurrentContextType() == QAsyncContextType.AsyncContextThread);

            await QAsTask.ToAsyncContextThread(_newContextThreadId);
            Assert.True(QAsTask.GetCurrentContextType() == QAsyncContextType.AsyncContextThread);

            await QAsTask.ToNormalThreadPool();
            Assert.True(QAsTask.GetCurrentContextType() == QAsyncContextType.NormalThreadPool);

            await QAsTask.ToBlockingThreadPool();
            Assert.True(QAsTask.GetCurrentContextType() == QAsyncContextType.BlockingThreadPool);
        }

        [Test]
        public async Task TestToMainThread()
        {
            await QAsTask.ToMainThread();
            Console.WriteLine(QAsTask.WhereAmI());
            Assert.True(QAsTask.IsMainThread(), "This is not the MainThread!");
        }

        [Test]
        public async Task TestToBackgroundThread()
        {
            await QAsTask.ToBackgroundThread();
            Console.WriteLine(QAsTask.WhereAmI());
            Assert.True(QAsTask.IsBackgroundThread(), "This is not the BackgroundThread!");
        }

        [Test]
        public async Task TestToAsyncContextThread()
        {
            await QAsTask.ToAsyncContextThread(_newContextThreadId);

            Console.WriteLine(QAsTask.WhereAmI());
            Assert.True(QAsTask.IsAsyncContextThread(_newContextThreadId), $"This is not the AsyncContextThread({_newContextThreadId})!");
        }

        [Test]
        public async Task TestToNormalThreadPool()
        {
            await QAsTask.ToNormalThreadPool();

            Console.WriteLine(QAsTask.WhereAmI());
            Assert.True(QAsTask.IsNormalThreadPool(), "This is not the NormalThreadPool!");
        }

        [Test]
        public async Task TestToBlockingThreadPool()
        {
            await QAsTask.ToBlockingThreadPool();

            Console.WriteLine(QAsTask.WhereAmI());
            Assert.True(QAsTask.IsBlockingThreadPool(), "This is not the BlockingThreadPool!");
        }

        [Test]
        public async Task TestDelayAwaiterMainThread()
        {
            await QAsTask.ToMainThread();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(QAsTask.IsMainThread(), $"This is not the MainThread: {QAsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {QAsTask.GetCurrentContextType()} awaiter 100ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 80 && stopwatch.ElapsedMilliseconds <= 150);
        }

        [Test]
        public async Task TestDelayAwaiterBackgroundThread()
        {
            await QAsTask.ToBackgroundThread();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(QAsTask.IsBackgroundThread(), $"This is not the BackgroundThread: {QAsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {QAsTask.GetCurrentContextType()} awaiter 100ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 80 && stopwatch.ElapsedMilliseconds <= 150);
        }

        [Test]
        public async Task TestDelayAwaiterNormalThreadPool()
        {
            await QAsTask.ToNormalThreadPool();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(QAsTask.IsNormalThreadPool(), $"This is not the NormalThreadPool: {QAsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {QAsTask.GetCurrentContextType()} awaiter 100ms");
            Assert.True(stopwatch.ElapsedMilliseconds >= 80 && stopwatch.ElapsedMilliseconds <= 150);
        }

        [Test]
        public async Task TestDelayAwaiterBlockingThreadPool()
        {
            await QAsTask.ToBlockingThreadPool();
            var stopwatch = Stopwatch.StartNew();
            await 100;
            stopwatch.Stop();
            Assert.True(QAsTask.IsBlockingThreadPool(), $"This is not the BlockingThreadPool: {QAsTask.WhereAmI()}");
            Console.WriteLine($"{nameof(stopwatch)} {stopwatch.ElapsedMilliseconds}ms == {QAsTask.GetCurrentContextType()} awaiter 100ms");
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
                await QAsTask.ToMainThread();
                Assert.True(QAsTask.IsMainThread());
            }

            for (var i = 0; i < 1000; i++)
            {
                await QAsTask.ToBackgroundThread();
                Assert.True(QAsTask.IsBackgroundThread());
            }

            for (var i = 0; i < 1000; i++)
            {
                await QAsTask.ToAsyncContextThread(_newContextThreadId);
                Assert.True(QAsTask.IsAsyncContextThread(_newContextThreadId));
            }

            for (var i = 0; i < 1000; i++)
            {
                await QAsTask.ToNormalThreadPool();
                Assert.True(QAsTask.IsNormalThreadPool());
            }

            for (var i = 0; i < 1000; i++)
            {
                await QAsTask.ToBlockingThreadPool();
                Assert.True(QAsTask.IsBlockingThreadPool());
            }
        }

        [Test]
        public async Task TestExceptions1()
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    await Exceptions(i);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Catch exception[{i}]");
                }
            }

            async Task Exceptions(int idx)
            {
                switch (idx)
                {
                    case 0:
                        await QAsTask.ToMainThread();
                        throw new ApplicationException("ToMainThread");
                    case 1:
                        await QAsTask.ToBackgroundThread();
                        throw new ApplicationException("ToBackgroundThread");
                    case 2:
                        await QAsTask.ToAsyncContextThread(_newContextThreadId);
                        throw new ApplicationException("ToAsyncContextThread");
                    case 3:
                        await QAsTask.ToNormalThreadPool();
                        throw new ApplicationException("ToNormalThreadPool");
                    case 4:
                        await QAsTask.ToBlockingThreadPool();
                        throw new ApplicationException("ToBlockingThreadPool");
                }
            }
        }

        [Test]
        public async Task TestExceptions2()
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    await Exceptions(i);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Catch exception[{i}]");
                }
            }

            async Task Exceptions(int idx)
            {
                switch (idx)
                {
                    case 0:
                        await QAsTask.ToMainThread(() => throw new ApplicationException("ToMainThread"));
                        break;
                    case 1:
                        await QAsTask.ToBackgroundThread(() => throw new ApplicationException("ToBackgroundThread"));
                        break;
                    case 2:
                        await QAsTask.ToAsyncContextThread(_newContextThreadId, () => throw new ApplicationException("ToAsyncContextThread"));
                        break;
                    case 3:
                        await QAsTask.ToNormalThreadPool(() => throw new ApplicationException("ToNormalThreadPool"));
                        break;
                    case 4:
                        await QAsTask.ToBlockingThreadPool(() => throw new ApplicationException("ToBlockingThreadPool"));
                        break;
                }
            }
        }

        [Test]
        public void TestExceptions3()
        {
            QAsTask.SetExceptionHandler(task => Console.WriteLine($"[UnhandledException] {task.Exception?.GetBaseException().TargetSite}"));

            for (var i = 0; i < 5; i++)
            {
                Exceptions(i);
            }

            void Exceptions(int idx)
            {
                switch (idx)
                {
                    case 0:
                        QAsTask.ToMainThread(() => throw new ApplicationException("ToMainThread"));
                        break;
                    case 1:
                        QAsTask.ToBackgroundThread(() => throw new ApplicationException("ToBackgroundThread"));
                        break;
                    case 2:
                        QAsTask.ToAsyncContextThread(_newContextThreadId, () => throw new ApplicationException("ToAsyncContextThread"));
                        break;
                    case 3:
                        QAsTask.ToNormalThreadPool(() => throw new ApplicationException("ToNormalThreadPool"));
                        break;
                    case 4:
                        QAsTask.ToBlockingThreadPool(() => throw new ApplicationException("ToBlockingThreadPool"));
                        break;
                }
            }
        }
    }
}