using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTask.Disposables;
using HardDev.AsTask.TaskHelpers;

namespace HardDev.AsTask.Context
{
    /// <inheritdoc />
    /// <summary>
    /// A thread that executes actions within an <see cref="T:HardDev.AsTask.Context.QAsyncContext" />.
    /// </summary>
    public sealed class QAsyncContextThread : QSingleDisposable<QAsyncContext>
    {
        /// <summary>
        /// Gets the <see cref="QAsyncContext"/>.
        /// </summary>
        public readonly QAsyncContext Context;

        /// <summary>
        /// The Id.
        /// </summary>
        public int Id => Context.Id;

        /// <summary>
        /// Gets the <see cref="TaskFactory"/> for this thread, which can be used to schedule work to this thread.
        /// </summary>
        public TaskFactory Factory => Context.Factory;

        /// <summary>
        /// The child thread.
        /// </summary>
        private readonly Task _thread;


        /// <summary>
        /// Initializes a new instance of the <see cref="QAsyncContextThread"/> class, creating a child thread waiting for commands.
        /// </summary>
        public QAsyncContextThread(string name)
        {
            Context = new QAsyncContext(name);
            Context.SynchronizationContext.OperationStarted();

            CreatesDisposableContext(Context);

            _thread = Task.Factory.StartNew(Execute, CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Requests the thread to exit and returns a task representing the exit of the thread. The thread will exit when all outstanding asynchronous operations complete.
        /// </summary>
        public Task JoinAsync()
        {
            Dispose();
            return _thread;
        }

        /// <summary>
        /// Requests the thread to exit and blocks until the thread exits. The thread will exit when all outstanding asynchronous operations complete.
        /// </summary>
        public void Join()
        {
            JoinAsync().WaitAndUnwrapException();
        }

        private void Execute()
        {
            using (Context)
            {
                Context.Execute();
            }
        }

        /// <summary>
        /// Permits the thread to exit, if we have not already done so.
        /// </summary>
        private void AllowThreadToExit()
        {
            Context.SynchronizationContext.OperationCompleted();
        }

        /// <inheritdoc />
        /// <summary>
        /// Requests the thread to exit.
        /// </summary>
        protected override void Dispose(QAsyncContext context)
        {
            AllowThreadToExit();
        }
    }
}