﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace HardDev.AsTask.Context
{
    /// <inheritdoc />
    /// <summary>
    /// A task scheduler which schedules tasks to an async context.
    /// </summary>
    public sealed class QAsyncContextTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// The async context for this task scheduler.
        /// </summary>
        private readonly QAsyncContext _context;

        /// <inheritdoc />
        /// <summary>
        /// Indicates the maximum concurrency level this <see cref="T:System.Threading.Tasks.TaskScheduler" /> is able to support.
        /// </summary>
        public override int MaximumConcurrencyLevel => 1;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:HardDev.AsTask.Context.QAsyncContext.QAsyncContextTaskScheduler" /> class.
        /// </summary>
        /// <param name="context">The async context for this task scheduler. May not be <c>null</c>.</param>
        public QAsyncContextTaskScheduler(QAsyncContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        /// <summary>
        /// Generates an enumerable of <see cref="T:System.Threading.Tasks.Task" /> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        /// <returns>An enumerable that allows traversal of tasks currently queued to this scheduler.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _context.GetScheduledTasks();
        }

        /// <inheritdoc />
        /// <summary>
        /// Queues a <see cref="T:System.Threading.Tasks.Task" /> to the scheduler. If all tasks have been completed and the outstanding asynchronous operation count is zero, then this method has undefined behavior.
        /// </summary>
        /// <param name="task">The <see cref="T:System.Threading.Tasks.Task" /> to be queued.</param>
        protected override void QueueTask(Task task)
        {
            _context.Enqueue(task, false);
        }

        /// <inheritdoc />
        /// <summary>
        /// Determines whether the provided <see cref="T:System.Threading.Tasks.Task" /> can be executed synchronously in this call, and if it can, executes it.
        /// </summary>
        /// <param name="task">The <see cref="T:System.Threading.Tasks.Task" /> to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">A Boolean denoting whether or not task has previously been queued. If this parameter is True, then the task may have been previously queued (scheduled); if False, then the task is known not to have been queued, and this call is being made in order to execute the task inline without queuing it.</param>
        /// <returns>A Boolean value indicating whether the task was executed inline.</returns>
        /// <exception cref="T:System.InvalidOperationException">The <paramref name="task" /> was already executed.</exception>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return QAsyncContext.Current == _context && TryExecuteTask(task);
        }

        /// <summary>
        /// Exposes the base <see cref="TaskScheduler.TryExecuteTask"/> method.
        /// </summary>
        /// <param name="task">The task to attempt to execute.</param>
        public void DoTryExecuteTask(Task task)
        {
            TryExecuteTask(task);
        }
    }
}