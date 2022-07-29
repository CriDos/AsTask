using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HardDev.Context;
using NUnit.Framework;

namespace HardDev;

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
        Assert.True(AsTask.ContainsContext(_contextTests1Id), $"This is not the Context({_contextTests1Id})!");
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
    public async Task TestDelayAwaiterMainContext()
    {
        await AsTask.ToMainContext();
        var stopwatch = Stopwatch.StartNew();
        await Task.Delay(100);
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
        await Task.Delay(100);
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
        await Task.Delay(100);
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
        await Task.Delay(100);
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
            Assert.True(AsTask.ContainsContext(_contextTests1Id));
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