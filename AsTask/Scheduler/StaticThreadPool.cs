using System;
using System.Threading;
using System.Threading.Tasks;

namespace HardDev.Scheduler;

public sealed class StaticThreadPool : AbstractThreadPool
{
    public StaticThreadPool(string name, int maxConcurrency) :
        base(name, maxConcurrency)
    {
        // Create all of the threads
        var threads = new Task[maxConcurrency];
        for (var i = 0; i < maxConcurrency; i++)
        {
            threads[i] = Task.Factory.StartNew(ThreadBasedDispatchLoop, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
        }
    }

    /// <summary>The dispatch loop run by all threads in this scheduler.</summary>
    private void ThreadBasedDispatchLoop()
    {
        TaskProcessingThread = true;
        try
        {
            // If a thread abort occurs, we'll try to reset it and continue running.
            while (true)
            {
                // For each task queued to the scheduler, try to execute it.
                foreach (var task in Queue.GetConsumingEnumerable(DisposeCancellation.Token))
                {
                    Interlocked.Increment(ref CountExecutableTasks);
                    TryExecuteTask(task);
                    Interlocked.Decrement(ref CountExecutableTasks);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // If the scheduler is disposed, the cancellation token will be set and
            // we'll receive an OperationCanceledException.  That OCE should not crash the process.
        }
        finally
        {
            TaskProcessingThread = false;
        }
    }
}