using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTaskLib.Awaiter;

namespace HardDev.AsTaskLib.Scheduler
{
    /// <summary>Provides a scheduler that uses threads.</summary>
    public sealed class ThreadPoolScheduler : TaskScheduler
    {
        public readonly TaskFactory TaskFactory;
        public readonly IAwaiter Awaiter;

        public int CountRunningTasks => Interlocked.CompareExchange(ref _countRunningTasks, 0, 0);
        public int CountTasksInQueue => _tasksToRun.Count;
        public override int MaximumConcurrencyLevel => _runningTasks.Length;

        private int _countRunningTasks;
        private readonly BlockingCollection<Task> _tasksToRun = new BlockingCollection<Task>();
        private readonly Task[] _runningTasks;

        /// <inheritdoc />
        /// <summary>Initializes a new instance of the TaskScheduler class with the specified concurrency level.</summary>
        /// <param name="numberOfThreads">The number of threads that should be created and used by this scheduler.</param>
        public ThreadPoolScheduler(int numberOfThreads)
        {
            // Validate arguments
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfThreads));

            TaskFactory = new TaskFactory(this);
            Awaiter = new TaskFactoryAwaiter(TaskFactory);

            // Create the task threads to be used by this scheduler
            _runningTasks = Enumerable.Range(1, numberOfThreads).Select(i =>
            {
                return Task.Factory.StartNew(() =>
                    {
                        // Continually get the next task and try to execute it.
                        // This will continue until the scheduler is disposed and no more tasks remain.
                        foreach (var t in _tasksToRun.GetConsumingEnumerable())
                        {
                            try
                            {
                                Interlocked.Increment(ref _countRunningTasks);
                                TryExecuteTask(t);
                            }
                            finally
                            {
                                Interlocked.Decrement(ref _countRunningTasks);
                            }
                        }
                    }, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
            }).ToArray();
        }

        /// <inheritdoc />
        /// <summary>Queues a Task to be executed by this scheduler.</summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task)
        {
            // Push it into the blocking collection of tasks
            _tasksToRun.Add(task);
        }

        /// <inheritdoc />
        /// <summary>Provides a list of the scheduled tasks for the debugger to consume.</summary>
        /// <returns>An enumerable of all tasks currently scheduled.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Serialize the contents of the blocking collection of tasks for the debugger
            return _tasksToRun;
        }

        /// <inheritdoc />
        /// <summary>Determines whether a Task may be inlined.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
        /// <returns>true if the task was successfully inlined; otherwise, false.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // Try to inline
            return TryExecuteTask(task);
        }
    }
}