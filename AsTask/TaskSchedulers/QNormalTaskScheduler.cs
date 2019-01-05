using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HardDev.AsTask.Awaiters;

namespace HardDev.AsTask.TaskSchedulers
{
    /// <inheritdoc />
    /// <summary>
    /// Provides a task scheduler that ensures a maximum concurrency level while
    /// running on top of the thread pool.
    /// </summary>
    public sealed class QNormalTaskScheduler : TaskScheduler
    {
        public readonly TaskFactory TaskFactory;
        public readonly IAwaiter Awaiter;

        // The maximum concurrency level allowed by this scheduler. 
        public override int MaximumConcurrencyLevel { get; }

        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;

        // The list of tasks to be executed 
        private readonly LinkedList<Task> _tasksToRun = new LinkedList<Task>();

        // Indicates whether the scheduler is currently processing work items. 
        private int _delegatesQueuedOrRunning;


        public QNormalTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

            MaximumConcurrencyLevel = maxDegreeOfParallelism;
            TaskFactory = new TaskFactory(this);
            Awaiter = new QTaskFactoryAwaiter(TaskFactory);
        }

        // Queues a task to the scheduler. 
        protected override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough 
            // delegates currently queued or running to process tasks, schedule another. 
            lock (_tasksToRun)
            {
                _tasksToRun.AddLast(task);

                if (_delegatesQueuedOrRunning >= MaximumConcurrencyLevel)
                    return;

                _delegatesQueuedOrRunning++;
                Task.Run(NotifyThreadPoolOfPendingWork);
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler. 
        private void NotifyThreadPoolOfPendingWork()
        {
            // Note that the current thread is now processing work items.
            // This is necessary to enable inlining of tasks into this thread.
            _currentThreadIsProcessingItems = true;
            try
            {
                // Process all available items in the queue.
                while (true)
                {
                    Task item;
                    lock (_tasksToRun)
                    {
                        // When there are no more items to be processed,
                        // note that we're done processing, and get out.
                        if (_tasksToRun.Count == 0)
                        {
                            _delegatesQueuedOrRunning--;
                            break;
                        }

                        // Get the next item from the queue
                        item = _tasksToRun.First.Value;
                        _tasksToRun.RemoveFirst();
                    }

                    // Execute the task we pulled out of the queue
                    TryExecuteTask(item);
                }
            }
            // We're done processing items on the current thread
            finally
            {
                _currentThreadIsProcessingItems = false;
            }
        }

        // Attempts to execute the specified task on the current thread. 
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if (!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued)
                // Try to run the task. 
                return TryDequeue(task) && TryExecuteTask(task);

            return TryExecuteTask(task);
        }

        // Attempt to remove a previously scheduled task from the scheduler. 
        protected override bool TryDequeue(Task task)
        {
            lock (_tasksToRun)
                return _tasksToRun.Remove(task);
        }

        // Gets an enumerable of the tasks currently scheduled on this scheduler. 
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            lock (_tasksToRun)
                return _tasksToRun;
        }
    }
}