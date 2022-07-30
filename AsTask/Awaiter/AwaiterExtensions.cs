using HardDev.Context;

namespace HardDev.Awaiter;

public static class AwaiterExtensions
{
    public static IAwaiter GetAwaiter(this ThreadContext context)
    {
        return context.Awaiter;
    }
}