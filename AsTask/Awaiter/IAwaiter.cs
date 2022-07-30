using System.Runtime.CompilerServices;

namespace HardDev.Awaiter;

/// <inheritdoc />
/// <summary>
/// Basic template for Awaiters.
/// </summary>
public interface IAwaiter : INotifyCompletion
{
    IAwaiter GetAwaiter();
    bool IsCompleted { get; }
    void GetResult();
}

/// <summary>
/// Basic template for Awaiters.
/// </summary>
/// <typeparam name="T">Type of result to return</typeparam>
public interface IAwaiter<out T> : INotifyCompletion
{
    bool IsCompleted { get; }
    IAwaiter<T> GetAwaiter();
    T GetResult();
}