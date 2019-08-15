using System;
using System.Threading;
using System.Threading.Tasks;

namespace HardDev.Scheduler
{
    public sealed class DynamicThreadPool : AbstractTaskScheduler
    {
        private int _countThreads;

        public DynamicThreadPool(string name, int maxConcurrency, ThreadPriority priority = ThreadPriority.Normal) :
            base(name, maxConcurrency, priority)
        {
        }

        protected override void QueueTask(Task task)
        {
            // If we've been disposed, no one should be queueing
            if (DisposeCancellation.IsCancellationRequested)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // add the task to the blocking queue
            TasksToRun.Add(task);

            if (Interlocked.CompareExchange(ref _countThreads, 0, 0) >= MaximumConcurrencyLevel)
                return;

            Interlocked.Increment(ref _countThreads);
            new Thread(ThreadBasedDispatchLoop)
            {
                Name = Name,
                Priority = Priority,
                IsBackground = true
            }.Start();
        }

        private void ThreadBasedDispatchLoop()
        {
            TaskProcessingThread = true;
            try
            {
                // If a thread abort occurs, we'll try to reset it and continue running.
                try
                {
                    // For each task queued to the scheduler, try to execute it.
                    while (TasksToRun.TryTake(out var task))
                    {
                        if (DisposeCancellation.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(GetType().Name);
                        }

                        Interlocked.Increment(ref _countExecutableTasks);
                        TryExecuteTask(task);
                        Interlocked.Decrement(ref _countExecutableTasks);
                    }
                }
                catch (ThreadAbortException)
                {
                    // If we received a thread abort, and that thread abort was due to shutting down
                    // or unloading, let it pass through.  Otherwise, reset the abort so we can
                    // continue processing work items.
                    if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
                    {
                        Thread.ResetAbort();
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
                Interlocked.Decrement(ref _countThreads);
            }
        }
    }
}