using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTask.Awaiters;
using HardDev.AsTask.TaskHelpers;

namespace HardDev.AsTask.Context
{
    /// <inheritdoc />
    /// <summary>
    /// Provides a context for asynchronous operations. This class is thread safe.
    /// </summary>
    /// <remarks>
    /// <para><see cref="M:HardDev.AsTask.Context.QAsyncContext.Execute" /> may only be called once. After <see cref="M:HardDev.AsTask.Context.QAsyncContext.Execute" /> returns, the async context should be disposed.</para>
    /// </remarks>
    public sealed class QAsyncContext : IDisposable
    {
        /// <summary>
        /// Name context
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Id context
        /// </summary>
        public int Id => TaskScheduler.Id;

        /// <summary>
        /// Awaiter current context
        /// </summary>
        public readonly IAwaiter Awaiter;

        /// <summary>
        /// Gets the <see cref="SynchronizationContext"/> for this <see cref="QAsyncContext"/>. From inside <see cref="Execute"/>, this value is always equal to <see cref="System.Threading.SynchronizationContext.Current"/>.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; }

        /// <summary>
        /// Gets the <see cref="System.Threading.Tasks.TaskScheduler"/> for this <see cref="QAsyncContext"/>. From inside <see cref="Execute"/>, this value is always equal to <see cref="System.Threading.Tasks.TaskScheduler.Current"/>.
        /// </summary>
        public QAsyncContextTaskScheduler TaskScheduler { get; }

        /// <summary>
        /// The <see cref="TaskFactory"/> for this <see cref="QAsyncContext"/>.
        /// </summary>
        public TaskFactory Factory { get; }

        /// <summary>
        /// The queue holding the actions to run.
        /// </summary>
        private readonly QTaskQueue _taskQueue;

        /// <summary>
        /// The number of outstanding operations, including actions in the queue.
        /// </summary>
        private int _outstandingOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="QAsyncContext"/> class. This is an advanced operation; most people should use one of the static <c>Run</c> methods instead.
        /// </summary>
        public QAsyncContext(string name = "")
        {
            Name = name;

            _taskQueue = new QTaskQueue();
            SynchronizationContext = new QAsyncSynchronizationContext(this);
            TaskScheduler = new QAsyncContextTaskScheduler(this);
            Factory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler, TaskContinuationOptions.HideScheduler,
                TaskScheduler);

            Awaiter = new QSynchronizationContextAwaiter(SynchronizationContext);
        }

        /// <summary>
        /// Queues a task for execution by <see cref="Execute"/>. If all tasks have been completed and the outstanding asynchronous operation count is zero, then this method has undefined behavior.
        /// </summary>
        /// <param name="task">The task to queue. May not be <c>null</c>.</param>
        /// <param name="propagateExceptions">A value indicating whether exceptions on this task should be propagated out of the main loop.</param>
        public void Enqueue(Task task, bool propagateExceptions)
        {
            OperationStarted();
            task.ContinueWith(_ => OperationCompleted(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler);
            _taskQueue.TryAdd(task, propagateExceptions);

            // If we fail to add to the queue, just drop the Task. This is the same behavior as the TaskScheduler.FromCurrentSynchronizationContext(WinFormsSynchronizationContext).
        }

        /// <summary>
        /// Increments the outstanding asynchronous operation count.
        /// </summary>
        public void OperationStarted()
        {
            Interlocked.Increment(ref _outstandingOperations);
        }

        /// <summary>
        /// Decrements the outstanding asynchronous operation count.
        /// </summary>
        public void OperationCompleted()
        {
            var newCount = Interlocked.Decrement(ref _outstandingOperations);
            if (newCount == 0)
                _taskQueue.CompleteAdding();
        }

        /// <inheritdoc />
        /// <summary>
        /// Disposes all resources used by this class. This method should NOT be called while <see cref="M:HardDev.AsTask.Context.QAsyncContext.Execute" /> is executing.
        /// </summary>
        public void Dispose()
        {
            _taskQueue.Dispose();
        }

        /// <summary>
        /// Executes all queued actions. This method returns when all tasks have been completed and the outstanding asynchronous operation count is zero. This method will unwrap and propagate errors from tasks that are supposed to propagate errors.
        /// </summary>
        public void Execute()
        {
            using (new QSynchronizationContextSwitcher(SynchronizationContext))
            {
                var tasks = _taskQueue.GetConsumingEnumerable();
                foreach (var task in tasks)
                {
                    TaskScheduler.DoTryExecuteTask(task.Item1);

                    // Propagate exception if necessary.
                    if (task.Item2)
                        task.Item1.WaitAndUnwrapException();
                }
            }
        }

        /// <summary>
        /// Queues a task for execution, and begins executing all tasks in the queue. This method returns when all tasks have been completed and the outstanding asynchronous operation count is zero. This method will unwrap and propagate errors from the task.
        /// </summary>
        /// <param name="action">The action to execute. May not be <c>null</c>.</param>
        /// <param name="contextName">Context name.</param>
        public static void Run(Action action, string contextName = "")
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var context = new QAsyncContext(contextName))
            {
                var task = context.Factory.Run(action);
                context.Execute();
                task.WaitAndUnwrapException();
            }
        }

        /// <summary>
        /// Queues a task for execution, and begins executing all tasks in the queue. This method returns when all tasks have been completed and the outstanding asynchronous operation count is zero. Returns the result of the task. This method will unwrap and propagate errors from the task.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="action">The action to execute. May not be <c>null</c>.</param>
        /// <param name="contextName">Context name.</param>
        public static TResult Run<TResult>(Func<TResult> action, string contextName = "")
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var context = new QAsyncContext(contextName))
            {
                var task = context.Factory.Run(action);
                context.Execute();
                return task.WaitAndUnwrapException();
            }
        }

        /// <summary>
        /// Queues a task for execution, and begins executing all tasks in the queue. This method returns when all tasks have been completed and the outstanding asynchronous operation count is zero. This method will unwrap and propagate errors from the task proxy.
        /// </summary>
        /// <param name="action">The action to execute. May not be <c>null</c>.</param>
        /// <param name="contextName">Context name.</param>
        public static void Run(Func<Task> action, string contextName = "")
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // ReSharper disable AccessToDisposedClosure
            using (var context = new QAsyncContext(contextName))
            {
                context.OperationStarted();
                var task = context.Factory.Run(action).ContinueWith(t =>
                {
                    context.OperationCompleted();
                    t.WaitAndUnwrapException();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, context.TaskScheduler);
                context.Execute();
                task.WaitAndUnwrapException();
            }
            // ReSharper restore AccessToDisposedClosure
        }

        /// <summary>
        /// Queues a task for execution, and begins executing all tasks in the queue. This method returns when all tasks have been completed and the outstanding asynchronous operation count is zero. Returns the result of the task proxy. This method will unwrap and propagate errors from the task proxy.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="action">The action to execute. May not be <c>null</c>.</param>
        /// <param name="contextName">Context name.</param>
        public static TResult Run<TResult>(Func<Task<TResult>> action, string contextName = "")
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // ReSharper disable AccessToDisposedClosure
            using (var context = new QAsyncContext(contextName))
            {
                context.OperationStarted();
                var task = context.Factory.Run(action).ContinueWith(t =>
                {
                    context.OperationCompleted();
                    return t.WaitAndUnwrapException();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, context.TaskScheduler);
                context.Execute();
                return task.WaitAndUnwrapException();
            }
            // ReSharper restore AccessToDisposedClosure
        }

        /// <summary>
        /// Generates an enumerable of <see cref="T:System.Threading.Tasks.Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        /// <returns>An enumerable that allows traversal of tasks currently queued to this scheduler.</returns>
        public IEnumerable<Task> GetScheduledTasks()
        {
            return _taskQueue.GetScheduledTasks();
        }

        /// <summary>
        /// Gets the current <see cref="QAsyncContext"/> for this thread, or <c>null</c> if this thread is not currently running in an <see cref="QAsyncContext"/>.
        /// </summary>
        public static QAsyncContext Current
        {
            get
            {
                var syncContext = SynchronizationContext.Current as QAsyncSynchronizationContext;

                return syncContext?.Context;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(TaskScheduler.Id)}: {TaskScheduler.Id}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is QAsyncContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        public static bool operator ==(QAsyncContext left, QAsyncContext right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(QAsyncContext left, QAsyncContext right)
        {
            return !Equals(left, right);
        }

        private bool Equals(QAsyncContext other)
        {
            return string.Equals(Name, other.Name);
        }
    }
}