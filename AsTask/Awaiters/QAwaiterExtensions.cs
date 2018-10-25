using System.Threading;

namespace HardDev.AsTask.Awaiters
{
    public static class QAwaiterExtensions
    {
        /// <summary>
        /// Waits for milliseconds.
        /// </summary>
        /// <param name="milliseconds">If positive, number of milliseconds to wait</param>
        public static IAwaiter GetAwaiter(this int milliseconds)
        {
            return new QDelayAwaiter(milliseconds);
        }

        public static IAwaiter GetAwaiter(this SynchronizationContext context)
        {
            return new QSynchronizationContextAwaiter(context);
        }
    }
}