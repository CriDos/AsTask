using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HardDev.AsTask.Awaiters;

namespace HardDev.AsTask.TaskSchedulers
{
    /// <summary>Provides a scheduler that uses threads.</summary>
    public sealed class QBlockingTaskScheduler : TaskScheduler, IDisposable
    {
        public readonly TaskFactory TaskFactory;
        public readonly IAwaiter Awaiter;

        /// <summary>Stores the queued tasks to be executed by our pool of threads.</summary>
        private BlockingCollection<Task> _tasks;
        /// <summary>The task threads used by the scheduler.</summary>
        private readonly List<Task> _threads;

        /// <inheritdoc />
        /// <summary>Initializes a new instance of the TaskScheduler class with the specified concurrency level.</summary>
        /// <param name="numberOfThreads">The number of threads that should be created and used by this scheduler.</param>
        public QBlockingTaskScheduler(int numberOfThreads)
        {
            // Validate arguments
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfThreads));

            TaskFactory = new TaskFactory(this);
            Awaiter = new QTaskFactoryAwaiter(TaskFactory);

            // Initialize the tasks collection
            _tasks = new BlockingCollection<Task>();

            // Create the task threads to be used by this scheduler
            _threads = Enumerable.Range(0, numberOfThreads).Select(i =>
            {
                return Task.Factory.StartNew(() =>
                    {
                        // Continually get the next task and try to execute it.
                        // This will continue until the scheduler is disposed and no more tasks remain.
                        foreach (var t in _tasks.GetConsumingEnumerable())
                        {
                            TryExecuteTask(t);
                        }
                    }, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
            }).ToList();
        }

        /// <inheritdoc />
        /// <summary>Queues a Task to be executed by this scheduler.</summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task)
        {
            // Push it into the blocking collection of tasks
            _tasks.Add(task);
        }

        /// <inheritdoc />
        /// <summary>Provides a list of the scheduled tasks for the debugger to consume.</summary>
        /// <returns>An enumerable of all tasks currently scheduled.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Serialize the contents of the blocking collection of tasks for the debugger
            return _tasks.ToArray();
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

        /// <inheritdoc />
        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel => _threads.Count;

        /// <inheritdoc />
        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            if (_tasks == null)
                return;

            // Indicate that no new tasks will be coming in
            _tasks.CompleteAdding();

            // Cleanup
            _tasks.Dispose();
            _tasks = null;
        }
    }
}