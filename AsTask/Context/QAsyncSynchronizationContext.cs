using System.Threading;
using HardDev.AsTask.TaskHelpers;

namespace HardDev.AsTask.Context
{
    /// <inheritdoc />
    /// <summary>
    /// The <see cref="T:System.Threading.SynchronizationContext" /> implementation used by <see cref="T:HardDev.AsTask.Context.QAsyncContext" />.
    /// </summary>
    public sealed class QAsyncSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// The async context.
        /// </summary>
        public QAsyncContext Context { get; }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:HardDev.AsTask.Context.QAsyncContext.QAsyncContextSynchronizationContext" /> class.
        /// </summary>
        /// <param name="context">The async context.</param>
        public QAsyncSynchronizationContext(QAsyncContext context)
        {
            Context = context;
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispatches an asynchronous message to the async context. If all tasks have been completed and the outstanding asynchronous operation count is zero, then this method has undefined behavior.
        /// </summary>
        /// <param name="d">The <see cref="T:System.Threading.SendOrPostCallback" /> delegate to call. May not be <c>null</c>.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            Context.Enqueue(Context.Factory.Run(() => d(state)), true);
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispatches an asynchronous message to the async context, and waits for it to complete.
        /// </summary>
        /// <param name="d">The <see cref="T:System.Threading.SendOrPostCallback" /> delegate to call. May not be <c>null</c>.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Send(SendOrPostCallback d, object state)
        {
            if (QAsyncContext.Current == Context)
            {
                d(state);
            }
            else
            {
                var task = Context.Factory.Run(() => d(state));
                task.WaitAndUnwrapException();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Responds to the notification that an operation has started by incrementing the outstanding asynchronous operation count.
        /// </summary>
        public override void OperationStarted()
        {
            Context.OperationStarted();
        }

        /// <inheritdoc />
        /// <summary>
        /// Responds to the notification that an operation has completed by decrementing the outstanding asynchronous operation count.
        /// </summary>
        public override void OperationCompleted()
        {
            Context.OperationCompleted();
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a copy of the synchronization context.
        /// </summary>
        /// <returns>A new <see cref="T:System.Threading.SynchronizationContext" /> object.</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new QAsyncSynchronizationContext(Context);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return (Context != null ? Context.GetHashCode() : 0);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance. It is considered equal if it refers to the same underlying async context as this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is QAsyncSynchronizationContext other && Equals(other);
        }

        public static bool operator ==(QAsyncSynchronizationContext left, QAsyncSynchronizationContext right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(QAsyncSynchronizationContext left, QAsyncSynchronizationContext right)
        {
            return !Equals(left, right);
        }

        private bool Equals(QAsyncSynchronizationContext other)
        {
            return Equals(Context, other.Context);
        }
    }
}