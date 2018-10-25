using System;
using System.Collections.Generic;

namespace HardDev.AsTask.Disposables
{
    /// <summary>
    /// Disposes a collection of disposables.
    /// </summary>
    public sealed class QQueueDisposable : QSingleDisposable<Queue<IDisposable>>
    {
        /// <inheritdoc />
        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose.</param>
        public QQueueDisposable(params IDisposable[] disposables)
            : this((IEnumerable<IDisposable>) disposables)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose.</param>
        public QQueueDisposable(IEnumerable<IDisposable> disposables)
        {
            var queue = new Queue<IDisposable>();
            foreach (var disposable in disposables)
                Add(disposable);

            CreatesDisposableContext(queue);
        }

        /// <inheritdoc />
        protected override void Dispose(Queue<IDisposable> context)
        {
            foreach (var disposable in context)
                disposable.Dispose();
        }

        /// <summary>
        /// Adds a disposable to the collection of disposables. If this instance is already disposed or disposing, then <paramref name="disposable"/> is disposed immediately.
        /// </summary>
        /// <param name="disposable">The disposable to add to our collection.</param>
        public void Add(IDisposable disposable)
        {
            if (!TryUpdateContext(x =>
            {
                // ReSharper disable AccessToDisposedClosure
                x.Enqueue(disposable);
                // ReSharper restore AccessToDisposedClosure
                return x;
            }))
                disposable.Dispose();
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose.</param>
        public static QQueueDisposable Create(params IDisposable[] disposables) =>
            new QQueueDisposable(disposables);

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose.</param>
        public static QQueueDisposable Create(IEnumerable<IDisposable> disposables) =>
            new QQueueDisposable(disposables);
    }
}