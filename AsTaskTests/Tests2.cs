using System;
using System.Threading.Tasks;
using HardDev.Context;
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
                    await AsTask.ToMainContext();
                    throw new ApplicationException("ToMainContext");
                case 1:
                    await AsTask.ToBackgroundContext();
                    throw new ApplicationException("ToBackgroundContext");
                case 2:
                    await AsTask.ToContext(_contextTests2Id);
                    throw new ApplicationException("ToContext");
                case 3:
                    await AsTask.ToStaticThreadPool();
                    throw new ApplicationException("ToStaticThreadPool");
                case 4:
                    await AsTask.ToDynamicThreadPool();
                    throw new ApplicationException("ToDynamicThreadPool");
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
                    await AsTask.ToMainContext(() => throw new ApplicationException("ToMainContext"));
                    break;
                case 1:
                    await AsTask.ToBackgroundContext(() => throw new ApplicationException("ToBackgroundContext"));
                    break;
                case 2:
                    await AsTask.ToContext(_contextTests2Id, () => throw new ApplicationException("ToContext"));
                    break;
                case 3:
                    await AsTask.ToStaticThreadPool(() => throw new ApplicationException("ToStaticThreadPool"));
                    break;
                case 4:
                    await AsTask.ToDynamicThreadPool(() => throw new ApplicationException("ToDynamicThreadPool"));
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
                    AsTask.ToMainContext(() => throw new ApplicationException("ToMainContext"));
                    break;
                case 1:
                    AsTask.ToBackgroundContext(() => throw new ApplicationException("ToBackgroundContext"));
                    break;
                case 2:
                    AsTask.ToContext(_contextTests2Id, () => throw new ApplicationException("ToContext"));
                    break;
                case 3:
                    AsTask.ToStaticThreadPool(() => throw new ApplicationException("ToStaticThreadPool"));
                    break;
                case 4:
                    AsTask.ToDynamicThreadPool(() => throw new ApplicationException("ToDynamicThreadPool"));
                    break;
            }
        }
    }
}