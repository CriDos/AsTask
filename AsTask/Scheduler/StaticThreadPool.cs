using System;
using System.Threading;
using System.Threading.Tasks;

namespace HardDev.Scheduler
{
    internal sealed class StaticThreadPool : AbstractTaskScheduler
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
                    try
                    {
                        // For each task queued to the scheduler, try to execute it.
                        foreach (var task in TasksToRun.GetConsumingEnumerable(DisposeCancellation.Token))
                        {
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
}