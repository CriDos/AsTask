using System;
using System.Threading;
using HardDev.AsTask.Disposables;

namespace HardDev.AsTask.TaskHelpers
{
    /// <inheritdoc />
    /// <summary>
    /// Utility class for temporarily switching <see cref="T:System.Threading.SynchronizationContext" /> implementations.
    /// </summary>
    public sealed class QSynchronizationContextSwitcher : QSingleDisposable<object>
    {
        /// <summary>
        /// The previous <see cref="SynchronizationContext"/>.
        /// </summary>
        private readonly SynchronizationContext _oldContext;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:HardDev.AsTask.TaskHelpers.QSynchronizationContextSwitcher" /> class, installing the new <see cref="T:System.Threading.SynchronizationContext" />.
        /// </summary>
        /// <param name="newContext">The new <see cref="T:System.Threading.SynchronizationContext" />. This can be <c>null</c> to remove an existing <see cref="T:System.Threading.SynchronizationContext" />.</param>
        public QSynchronizationContextSwitcher(SynchronizationContext newContext)
        {
            CreatesDisposableContext(new object());
            _oldContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(newContext);
        }

        /// <inheritdoc />
        /// <summary>
        /// Restores the old <see cref="T:System.Threading.SynchronizationContext" />.
        /// </summary>
        protected override void Dispose(object context)
        {
            SynchronizationContext.SetSynchronizationContext(_oldContext);
        }

        /// <summary>
        /// Executes a synchronous delegate without the current <see cref="SynchronizationContext"/>. The current context is restored when this function returns.
        /// </summary>
        /// <param name="action">The delegate to execute.</param>
        public static void NoContext(Action action)
        {
            using (new QSynchronizationContextSwitcher(null))
                action();
        }

        /// <summary>
        /// Executes a synchronous or asynchronous delegate without the current <see cref="SynchronizationContext"/>. The current context is restored when this function synchronously returns.
        /// </summary>
        /// <param name="action">The delegate to execute.</param>
        public static T NoContext<T>(Func<T> action)
        {
            using (new QSynchronizationContextSwitcher(null))
                return action();
        }
    }
}