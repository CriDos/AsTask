using System;
using System.Runtime.InteropServices;
using System.Threading;
using HardDev.Context;

namespace HardDev.Awaiter;

[StructLayout(LayoutKind.Auto)]
public readonly struct ThreadContextAwaiter : IAwaiter
{
    public IAwaiter GetAwaiter() => this;
    public bool IsCompleted => _context.Context == SynchronizationContext.Current;

    private readonly ThreadContext _context;

    public ThreadContextAwaiter(ThreadContext context)
    {
        _context = context;
    }

    public void OnCompleted(Action action)
    {
        _context.Post(action);
    }

    public void GetResult()
    {
    }
}