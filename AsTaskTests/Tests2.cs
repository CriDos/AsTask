using System;
using System.Threading;
using System.Threading.Tasks;
using HardDev.Context;
using HardDev.Scheduler;
using NUnit.Framework;

namespace HardDev;

[TestFixture]
public class Tests2
{
    private readonly int _contextTests2Id;

    public Tests2()
    {
        AsTask.Initialize();

        _contextTests2Id = AsTask.CreateContext("Tests2");
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
            catch (Exception e)
            {
                Console.WriteLine($"Catch exception[{e.Message}]");
            }
        }

        async Task Exceptions(int idx)
        {
            switch (idx)
            {
                case 0:
                    await AsTask.ToMainContext();
                    throw new Exception("ToMainContext");
                case 1:
                    await AsTask.ToBackgroundContext();
                    throw new Exception("ToBackgroundContext");
                case 2:
                    await AsTask.ToContext(_contextTests2Id);
                    throw new Exception("ToContext");
                case 3:
                    await AsTask.ToStaticThreadPool();
                    throw new Exception("ToStaticThreadPool");
                case 4:
                    await AsTask.ToDynamicThreadPool();
                    throw new Exception("ToDynamicThreadPool");
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
                    await AsTask.ToMainContext(() => throw new Exception("ToMainContext"));
                    break;
                case 1:
                    await AsTask.ToBackgroundContext(() => throw new Exception("ToBackgroundContext"));
                    break;
                case 2:
                    await AsTask.ToContext(_contextTests2Id, () => throw new Exception("ToContext"));
                    break;
                case 3:
                    await AsTask.ToStaticThreadPool(() => throw new Exception("ToStaticThreadPool"));
                    break;
                case 4:
                    await AsTask.ToDynamicThreadPool(() => throw new Exception("ToDynamicThreadPool"));
                    break;
            }
        }
    }

    [Test]
    public void TestExceptions3()
    {
        TaskExceptionHandler.SetExceptionHandler(task => Console.WriteLine($"[UnhandledException] {task.Exception?.GetBaseException().Message}"));

        for (var i = 0; i < 5; i++)
        {
            Exceptions(i);
        }

        void Exceptions(int idx)
        {
            switch (idx)
            {
                case 0:
                    AsTask.ToMainContext(() => throw new Exception("ToMainContext"));
                    break;
                case 1:
                    AsTask.ToBackgroundContext(() => throw new Exception("ToBackgroundContext"));
                    break;
                case 2:
                    AsTask.ToContext(_contextTests2Id, () => throw new Exception("ToContext"));
                    break;
                case 3:
                    AsTask.ToStaticThreadPool(() => throw new Exception("ToStaticThreadPool"));
                    break;
                case 4:
                    AsTask.ToDynamicThreadPool(() => throw new Exception("ToDynamicThreadPool"));
                    break;
            }
        }
    }
}