using System;

namespace HardDev.AsTask.Disposables
{
    /// <summary>
    /// A singleton disposable that does nothing when disposed.
    /// </summary>
    public sealed class QNoopDisposable : IDisposable
    {
        private QNoopDisposable()
        {
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the instance of <see cref="QNoopDisposable"/>.
        /// </summary>
        public static QNoopDisposable Instance { get; } = new QNoopDisposable();
    }
}