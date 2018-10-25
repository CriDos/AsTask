namespace HardDev.AsTask.Context
{
    public enum QAsyncContextType
    {
        UndefinedThread,
        MainThread,
        AsyncContextThread,
        NormalThreadPool,
        BlockingThreadPool,
        UndefinedThreadPool
    }
}