using System.Threading;

namespace HardDev.AsTaskLib.Context
{
    /// <inheritdoc />
    /// <summary>
    /// The <see cref="T:System.Threading.SynchronizationContext" /> implementation used by <see cref="T:HardDev.AsTaskLib.Context.AsContext" />.
    /// </summary>
    public sealed class AsSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// The async context thread.
        /// </summary>
        public AsContext Context { get; }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:HardDev.AsTask.Context.AsContext.QAsyncContextSynchronizationContext" /> class.
        /// </summary>
        /// <param name="context">The async context.</param>
        public AsSynchronizationContext(AsContext context)
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
            Context.Enqueue(Context.Post(() => d(state)), true);
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispatches an asynchronous message to the async context, and waits for it to complete.
        /// </summary>
        /// <param name="d">The <see cref="T:System.Threading.SendOrPostCallback" /> delegate to call. May not be <c>null</c>.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Send(SendOrPostCallback d, object state)
        {
            if (AsContext.Current == Context)
            {
                d(state);
            }
            else
            {
                var task = Context.Post(() => d(state));
                task.GetAwaiter().GetResult();
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
            return new AsSynchronizationContext(Context);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return Context != null ? Context.GetHashCode() : 0;
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
            return obj is AsSynchronizationContext other && Equals(other);
        }

        public static bool operator ==(AsSynchronizationContext left, AsSynchronizationContext right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AsSynchronizationContext left, AsSynchronizationContext right)
        {
            return !Equals(left, right);
        }

        private bool Equals(AsSynchronizationContext other)
        {
            return Equals(Context, other.Context);
        }
    }
}