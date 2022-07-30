using System;
using System.Threading;
using System.Threading.Tasks;

namespace HardDev.Scheduler;

public sealed class DynamicThreadPool : AbstractThreadPool
{
    private int _countThreads;

    public DynamicThreadPool(string name, int maxConcurrency) :
        base(name, maxConcurrency)
    {
    }

    protected override void QueueTask(Task task)
    {
        // add the task to the blocking queue
        Queue.Add(task);

        if (Interlocked.CompareExchange(ref _countThreads, 0, 0) >= MaximumConcurrencyLevel)
            return;

        Interlocked.Increment(ref _countThreads);
        Task.Factory.StartNew(ThreadBasedDispatchLoop, TaskCreationOptions.DenyChildAttach);
    }

    private void ThreadBasedDispatchLoop()
    {
        TaskProcessingThread = true;
        try
        {
            // For each task queued to the scheduler, try to execute it.
            while (Queue.TryTake(out var task))
            {
                if (DisposeCancellation.IsCancellationRequested)
                {
                    throw new OperationCanceledException(GetType().Name);
                }

                Interlocked.Increment(ref CountExecutableTasks);
                TryExecuteTask(task);
                Interlocked.Decrement(ref CountExecutableTasks);
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
            Interlocked.Decrement(ref _countThreads);
        }
    }
}