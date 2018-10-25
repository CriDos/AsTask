using System;
using System.Runtime.InteropServices;
using System.Threading;
using HardDev.AsTask.TaskHelpers;

namespace HardDev.AsTask.Awaiters
{
    [StructLayout(LayoutKind.Auto)]
    public struct QSynchronizationContextAwaiter : IAwaiter
    {
        public IAwaiter GetAwaiter() => this;
        public bool IsCompleted => _context == SynchronizationContext.Current;

        private readonly SynchronizationContext _context;

        public QSynchronizationContextAwaiter(SynchronizationContext context)
        {
            _context = context;
        }

        public void OnCompleted(Action action)
        {
            _context.PostAsync(action).ExceptionHandlerWR();
        }

        public void GetResult()
        {
        }
    }
}