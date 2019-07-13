using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTaskLib.Awaiter;
using HardDev.AsTaskLib.Scheduler;

namespace HardDev.AsTaskLib.Context
{
    /// <summary>
    /// A context thread that executes actions within an <see cref="T:HardDev.AsTaskLib.Context.AsContext" />.
    /// </summary>
    public sealed class AsContext : IDisposable
    {
        public static AsContext Current => (SynchronizationContext.Current as AsSynchronizationContext)?.Context;

        /// <summary>
        /// Name context
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Id context
        /// </summary>
        public int Id => _scheduler.Id;

        /// <summary>
        /// Awaiter current context
        /// </summary>
        public readonly IAwaiter Awaiter;

        /// <summary>
        /// Gets the <see cref="SynContext"/> for this <see cref="AsContext"/>. From inside <see cref="Execute"/>, this value is always equal to <see cref="System.Threading.SynchronizationContext.Current"/>.
        /// </summary>
        public SynchronizationContext SynContext { get; }

        /// <summary>
        /// The thread loop.
        /// </summary>
        private readonly Thread _thread;

        /// <summary>
        /// Gets the <see cref="System.Threading.Tasks.TaskScheduler"/> for this <see cref="AsContext"/>. From inside <see cref="Execute"/>, this value is always equal to <see cref="System.Threading.Tasks.TaskScheduler.Current"/>.
        /// </summary>
        private readonly AsContextScheduler _scheduler;

        /// <summary>
        /// The <see cref="TaskFactory"/> for this <see cref="AsContext"/>.
        /// </summary>
        private readonly TaskFactory _factory;

        /// <summary>
        /// The queue holding the actions to run.
        /// </summary>
        private readonly BlockingCollection<Tuple<Task, bool>> _taskQueue;

        /// <summary>
        /// The number of outstanding operations, including actions in the queue.
        /// </summary>
        private int _outstandingOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsContext"/> class, creating a child thread waiting for commands.
        /// </summary>
        public AsContext(string name, SynchronizationContext context = null)
        {
            Name = name;
            SynContext = context ?? new AsSynchronizationContext(this);
            Awaiter = new AsContextAwaiter(this);

            _scheduler = new AsContextScheduler(this);
            _factory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler, TaskContinuationOptions.HideScheduler,
                _scheduler);
            _taskQueue = new BlockingCollection<Tuple<Task, bool>>();
            
            SynContext.OperationStarted();
            _thread = new Thread(Execute) {Name = name, IsBackground = true};
            _thread.Start();
        }

        /// <summary>
        /// Asynchronously executes a delegate on this synchronization context.
        /// </summary>
        /// <param name="action">The delegate to execute.</param>
        public Task PostAsync(Action action)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            SynContext.Post(state =>
            {
                try
                {
                    ((Action) state)();
                    tcs.TrySetResult(null);
                }
                catch (OperationCanceledException ex)
                {
                    tcs.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, action);

            return tcs.Task.ExceptionHandler();
        }

        /// <summary>
        /// Queues a task for execution by <see cref="Execute"/>. If all tasks have been completed and the outstanding asynchronous operation count is zero, then this method has undefined behavior.
        /// </summary>
        /// <param name="task">The task to queue. May not be <c>null</c>.</param>
        /// <param name="propagateExceptions">A value indicating whether exceptions on this task should be propagated out of the main loop.</param>
        public void Enqueue(Task task, bool propagateExceptions)
        {
            OperationStarted();
            task.ContinueWith(_ => OperationCompleted(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, _scheduler);
            _taskQueue.TryAdd(Tuple.Create(task, propagateExceptions));
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
            if (Interlocked.Decrement(ref _outstandingOperations) == 0)
                _taskQueue.CompleteAdding();
        }

        /// <summary>
        /// Executes all queued actions. This method returns when all tasks have been completed and the outstanding asynchronous operation count is zero.
        /// This method will unwrap and propagate errors from tasks that are supposed to propagate errors.
        /// </summary>
        public void Execute()
        {
            SynchronizationContext.SetSynchronizationContext(SynContext);
            foreach (var e in _taskQueue.GetConsumingEnumerable())
            {
                var task = e.Item1;
                var propagateExceptions = e.Item2;

                _scheduler.RunTask(task);

                // Propagate exception if necessary.
                if (propagateExceptions)
                    task.GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Queues a task for execution, and begins executing all tasks in the queue. This method returns when all tasks have been completed and the outstanding asynchronous operation count is zero. This method will unwrap and propagate errors from the task.
        /// </summary>
        /// <param name="action">The action to execute. May not be <c>null</c>.</param>
        public Task Post(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return _factory.StartNew(action, _factory.CancellationToken,
                _factory.CreationOptions | TaskCreationOptions.DenyChildAttach,
                _factory.Scheduler ?? TaskScheduler.Default);
        }

        /// <summary>
        /// Queues a task for execution, and begins executing all tasks in the queue. This method returns when all tasks have been completed and the outstanding asynchronous operation count is zero. This method will unwrap and propagate errors from the task.
        /// </summary>
        /// <param name="action">The action to execute. May not be <c>null</c>.</param>
        public Task Post<T>(Func<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return _factory.StartNew(action, _factory.CancellationToken,
                _factory.CreationOptions | TaskCreationOptions.DenyChildAttach,
                _factory.Scheduler ?? TaskScheduler.Default);
        }

        public IEnumerable<Task> GetScheduledTasks()
        {
            return _taskQueue.Select(item => item.Item1);
        }

        /// <summary>
        /// Requests the thread to exit and blocks until the thread exits. The thread will exit when all outstanding asynchronous operations complete.
        /// </summary>
        public void Join()
        {
            _thread.Join();
        }

        /// <summary>
        /// Permits the thread to exit, if we have not already done so.
        /// </summary>
        public void AllowToExit()
        {
            SynContext.OperationCompleted();
        }

        /// <inheritdoc />
        /// <summary>
        /// Disposes all resources used by this class. This method should NOT be called while <see cref="M:HardDev.AsTaskLib.Context.AsContext.Execute" /> is executing.
        /// </summary>
        public void Dispose()
        {
            _taskQueue.Dispose();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is AsContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        public static bool operator ==(AsContext left, AsContext right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AsContext left, AsContext right)
        {
            return !Equals(left, right);
        }

        private bool Equals(AsContext other)
        {
            return string.Equals(Name, other.Name);
        }

        public override string ToString()
        {
            return $"{nameof(AsContext)}[{nameof(Name)}: {Name}, {nameof(Id)}: {Id}, {nameof(_outstandingOperations)}: {_outstandingOperations}]";
        }
    }
}