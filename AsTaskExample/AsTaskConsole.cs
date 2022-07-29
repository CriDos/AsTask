using System.Collections.Generic;
using System.Threading.Tasks;
using HardDev;
using HardDev.Context;
using static System.Console;

namespace AsTaskExample
{
    public static class AsTaskConsole
    {
        private static bool _isShutdown;

        private static async Task Main()
        {
            WriteLine("Assign an exception handler.");
            TaskExceptionHandler.SetExceptionHandler(task =>
            {
                Error.WriteLine(task.Exception != null
                    ? $"[ExceptionHandler] {task.Exception.GetBaseException().Message}"
                    : $"[ExceptionHandler] Unhandled exception in task {task}");
            });


            WriteLine("We get or create a synchronization context and switch to it.");
            await AsTask.Initialize();

            WriteLine($"Print to the console information about the current context: {AsTask.WhereAmI()}");

            WriteLine("Switch to the background context.");
            await AsTask.ToBackgroundContext();

            WriteLine($"Now we get information about the context of the background context: {AsTask.WhereAmI()}");

            WriteLine("Back switch to the main context.");
            await AsTask.ToMainContext();

            WriteLine("We call the asynchronous methods, which performs the heavy work...");
            var tasks = new List<Task<long>>();
            for (var i = 1; i <= 10; i++)
            {
                tasks.Add(FindPrimeNumberAsync(20000 * i));
            }

            WriteLine("Asynchronously waiting for the execution of tasks.");
            _ = Task.WhenAll(tasks).ContinueWith(async task =>
            {
                await AsTask.ToMainContext();

                WriteLine("This is the result of our calculations:");

                foreach (var val in task.Result)
                    WriteLine(val);

                Shutdown();
            }).ExceptionHandler();

            if (AsTask.IsMainContext())
                WriteLine("hmm, we're still in the main context!:)");

            WriteLine("The life cycle of a console application runs on the main context.");
            var scheduler = AsTask.GetStaticTaskScheduler();
            while (!_isShutdown)
            {
                WriteLine($"Count running tasks: {scheduler.CountExecutableTasks}; Count tasks in queue: {scheduler.CountTasksInQueue}");
                await Task.Delay(1000); // Each iteration waits asynchronously for 1000 ms
            }

            WriteLine("Shutdown...");
            for (var i = 5; i > 0; i--)
            {
                WriteLine($"Shutdown through {i}s");
                await Task.Delay(1000);
            }
        }

        public static void Shutdown() => _isShutdown = true;

        private static async Task<long> FindPrimeNumberAsync(int n)
        {
            // Here we switch to a normal thread pool to do the heavy work...
            await AsTask.ToStaticThreadPool();

            var count = 0;
            long a = 2;
            while (count < n)
            {
                long b = 2;
                var prime = 1;
                while (b * b <= a)
                {
                    if (a % b == 0)
                    {
                        prime = 0;
                        break;
                    }

                    b++;
                }

                if (prime > 0)
                {
                    count++;
                }

                a++;
            }

            return --a;
        }
    }
}