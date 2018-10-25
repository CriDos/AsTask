using System;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTask.Disposables;
using HardDev.AsTask.TaskHelpers;

// Original idea from Stephen Toub: http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266983.aspx

namespace HardDev.AsTask.Coordination
{
    /// <summary>
    /// An async-compatible semaphore. Alternatively, you could use <c>SemaphoreSlim</c>.
    /// </summary>
    public sealed class QAsyncSemaphore
    {
        /// <summary>
        /// The queue of TCSs that other tasks are awaiting to acquire the semaphore.
        /// </summary>
        private readonly IAsyncWaitQueue<object> _queue;

        /// <summary>
        /// The number of waits that will be immediately granted.
        /// </summary>
        private long _count;

        /// <summary>
        /// The semi-unique identifier for this instance. This is 0 if the id has not yet been created.
        /// </summary>
        private int _id;

        /// <summary>
        /// The object used for mutual exclusion.
        /// </summary>
        private readonly object _mutex;

        /// <summary>
        /// Creates a new async-compatible semaphore with the specified initial count.
        /// </summary>
        /// <param name="initialCount">The initial count for this semaphore. This must be greater than or equal to zero.</param>
        /// <param name="queue">The wait queue used to manage waiters. This may be <c>null</c> to use a default (FIFO) queue.</param>
        public QAsyncSemaphore(long initialCount, IAsyncWaitQueue<object> queue)
        {
            _queue = queue ?? new QDefaultAsyncWaitQueue<object>();
            _count = initialCount;
            _mutex = new object();
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a new async-compatible semaphore with the specified initial count.
        /// </summary>
        /// <param name="initialCount">The initial count for this semaphore. This must be greater than or equal to zero.</param>
        public QAsyncSemaphore(long initialCount)
            : this(initialCount, null)
        {
        }

        /// <summary>
        /// Gets a semi-unique identifier for this asynchronous semaphore.
        /// </summary>
        public int Id => QIdManager<QAsyncSemaphore>.GetId(ref _id);

        /// <summary>
        /// Gets the number of slots currently available on this semaphore. This member is seldom used; code using this member has a high possibility of race conditions.
        /// </summary>
        public long CurrentCount
        {
            get
            {
                lock (_mutex)
                {
                    return _count;
                }
            }
        }

        /// <summary>
        /// Asynchronously waits for a slot in the semaphore to be available.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this is already set, then this method will attempt to take the slot immediately (succeeding if a slot is currently available).</param>
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            Task ret;
            lock (_mutex)
            {
                // If the semaphore is available, take it immediately and return.
                if (_count != 0)
                {
                    --_count;
                    ret = QTaskConstants.Completed;
                }
                else
                {
                    // Wait for the semaphore to become available or cancellation.
                    ret = _queue.Enqueue(_mutex, cancellationToken);
                }
            }

            return ret;
        }

        /// <summary>
        /// Asynchronously waits for a slot in the semaphore to be available.
        /// </summary>
        public Task WaitAsync()
        {
            return WaitAsync(CancellationToken.None);
        }

        /// <summary>
        /// Synchronously waits for a slot in the semaphore to be available. This method may block the calling thread.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this is already set, then this method will attempt to take the slot immediately (succeeding if a slot is currently available).</param>
        public void Wait(CancellationToken cancellationToken)
        {
            WaitAsync(cancellationToken).WaitAndUnwrapException(cancellationToken);
        }

        /// <summary>
        /// Synchronously waits for a slot in the semaphore to be available. This method may block the calling thread.
        /// </summary>
        public void Wait()
        {
            Wait(CancellationToken.None);
        }

        /// <summary>
        /// Releases the semaphore.
        /// </summary>
        public void Release(long releaseCount)
        {
            if (releaseCount == 0)
                return;

            lock (_mutex)
            {
                checked
                {
                    // ReSharper disable UnusedVariable
                    var test = _count + releaseCount;
                    // ReSharper restore UnusedVariable
                }

                while (releaseCount != 0 && !_queue.IsEmpty)
                {
                    _queue.Dequeue();
                    --releaseCount;
                }

                _count += releaseCount;
            }
        }

        /// <summary>
        /// Releases the semaphore.
        /// </summary>
        public void Release()
        {
            Release(1);
        }

        private async Task<IDisposable> DoLockAsync(CancellationToken cancellationToken)
        {
            await WaitAsync(cancellationToken).ConfigureAwait(false);
            return QAnonymousDisposable.Create(Release);
        }

        /// <summary>
        /// Asynchronously waits on the semaphore, and returns a disposable that releases the semaphore when disposed, thus treating this semaphore as a "multi-lock".
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this is already set, then this method will attempt to take the slot immediately (succeeding if a slot is currently available).</param>
        public QAwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            return new QAwaitableDisposable<IDisposable>(DoLockAsync(cancellationToken));
        }

        /// <summary>
        /// Asynchronously waits on the semaphore, and returns a disposable that releases the semaphore when disposed, thus treating this semaphore as a "multi-lock".
        /// </summary>
        public QAwaitableDisposable<IDisposable> LockAsync() => LockAsync(CancellationToken.None);

        /// <summary>
        /// Synchronously waits on the semaphore, and returns a disposable that releases the semaphore when disposed, thus treating this semaphore as a "multi-lock".
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this is already set, then this method will attempt to take the slot immediately (succeeding if a slot is currently available).</param>
        public IDisposable Lock(CancellationToken cancellationToken)
        {
            Wait(cancellationToken);
            return QAnonymousDisposable.Create(Release);
        }

        /// <summary>
        /// Synchronously waits on the semaphore, and returns a disposable that releases the semaphore when disposed, thus treating this semaphore as a "multi-lock".
        /// </summary>
        public IDisposable Lock() => Lock(CancellationToken.None);
    }
}