using HardDev.AsTaskLib.Context;

namespace HardDev.AsTaskLib.Awaiter
{
    public static class AwaiterExtensions
    {
        /// <summary>
        /// Waits for milliseconds.
        /// </summary>
        /// <param name="milliseconds">If positive, number of milliseconds to wait</param>
        public static IAwaiter GetAwaiter(this int milliseconds)
        {
            return new DelayAwaiter(milliseconds);
        }

        public static IAwaiter GetAwaiter(this AsContext context)
        {
            return context.Awaiter;
        }
    }
}