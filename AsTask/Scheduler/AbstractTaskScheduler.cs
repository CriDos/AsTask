using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTaskLib.Awaiter;

namespace HardDev.AsTaskLib.Scheduler
{
    /// <summary>
    /// Provides a TaskScheduler that provides control over the underlying threads utilized.
    /// </summary>
    public abstract class AbstractTaskScheduler : TaskScheduler, IDisposable
    {
        public readonly string Name;
        public int CountExecutableTasks => _countExecutableTasks;
        public int CountTasksInQueue => TasksToRun.Count;
        public readonly ThreadPriority Priority;
        public readonly TaskFactory TaskFactory;
        public readonly IAwaiter Awaiter;

        /// <summary>Gets the maximum concurrency level to use when processing tasks.</summary>
        public override int MaximumConcurrencyLevel { get; }

        /// <summary>Whether we're processing tasks on the current thread.</summary>
        [ThreadStatic]
        protected static bool TaskProcessingThread;

        /// <summary>The collection of tasks to be executed on our custom threads.</summary>
        protected readonly BlockingCollection<Task> TasksToRun = new BlockingCollection<Task>();

        /// <summary>Cancellation token used for disposal.</summary>
        protected readonly CancellationTokenSource DisposeCancellation = new CancellationTokenSource();

        protected int _countExecutableTasks;

        /// <summary>Initializes the scheduler.</summary>
        /// <param name="maxConcurrency">The number of threads to create and use for processing work items.</param>
        /// <param name="name">The name to use for each of the created threads.</param>
        /// <param name="priority">The priority to assign to each thread.</param>
        protected AbstractTaskScheduler(string name, int maxConcurrency, ThreadPriority priority = ThreadPriority.Normal)
        {
            Name = name;
            MaximumConcurrencyLevel = maxConcurrency <= 0 ? Environment.ProcessorCount : maxConcurrency;
            Priority = priority;
            TaskFactory = new TaskFactory(this);
            Awaiter = new TaskFactoryAwaiter(TaskFactory);
        }


        /// <summary>Queues a task to the scheduler.</summary>
        /// <param name="task">The task to be queued.</param>
        protected override void QueueTask(Task task)
        {
            // If we've been disposed, no one should be queueing
            if (DisposeCancellation.IsCancellationRequested)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // add the task to the blocking queue
            TasksToRun.Add(task);
        }

        /// <summary>Tries to execute a task synchronously on the current thread.</summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
        /// <returns>true if the task was executed; otherwise, false.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If we're already running tasks on this threads, enable inlining
            return TaskProcessingThread && TryExecuteTask(task);
        }

        /// <summary>Gets the tasks scheduled to this scheduler.</summary>
        /// <returns>An enumerable of all tasks queued to this scheduler.</returns>
        /// <remarks>This does not include the tasks on sub-schedulers.  Those will be retrieved by the debugger separately.</remarks>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Get the tasks from the blocking queue.
            return TasksToRun;
        }

        /// <summary>Initiates shutdown of the scheduler.</summary>
        public void Dispose()
        {
            DisposeCancellation.Cancel();
        }
    }
}