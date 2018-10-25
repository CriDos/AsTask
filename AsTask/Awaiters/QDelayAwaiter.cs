using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HardDev.AsTask.Awaiters
{
    /// <summary>
    /// Awaiter waiting time in milliseconds.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct QDelayAwaiter : IAwaiter
    {
        public IAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;


        private readonly int _ms;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ms">If positive, number of milliseconds to wait</param>
        public QDelayAwaiter(int ms)
        {
            _ms = ms < 0 ? 0 : ms;
        }

        public async void OnCompleted(Action action)
        {
            await Task.Delay(_ms);
            action();
        }

        public void GetResult()
        {
        }
    }
}