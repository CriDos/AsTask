using System;
using System.Runtime.InteropServices;
using System.Threading;
using HardDev.AsTaskLib.Context;

namespace HardDev.AsTaskLib.Awaiter
{
    [StructLayout(LayoutKind.Auto)]
    public struct AsContextAwaiter : IAwaiter
    {
        public IAwaiter GetAwaiter() => this;
        public bool IsCompleted => _context.SynContext == SynchronizationContext.Current;

        private readonly AsContext _context;

        public AsContextAwaiter(AsContext context)
        {
            _context = context;
        }

        public void OnCompleted(Action action)
        {
            _context.PostAsync(action);
        }

        public void GetResult()
        {
        }
    }
}