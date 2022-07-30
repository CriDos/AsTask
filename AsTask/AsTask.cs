using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HardDev.Awaiter;
using HardDev.Context;
using HardDev.Scheduler;

namespace HardDev;

public static class AsTask
{
    private static int _mainContextId = -1;
    private static int _backgroundContextId = -1;

    private static AbstractThreadPool _staticThreadPool;
    private static AbstractThreadPool _dynamicThreadPool;

    private static readonly Dictionary<int, ThreadContext> ContextById = new();
    private static readonly Dictionary<string, ThreadContext> ContextByName = new();

    private static bool _initialized;

    public static IAwaiter Initialize(SynchronizationContext mainContext = null, int maxStaticPool = -1, int maxDynamicPool = -1)
    {
        if (!_initialized)
        {
            _mainContextId = CreateContext("main", mainContext ?? SynchronizationContext.Current);
            _backgroundContextId = CreateContext("background");

            _staticThreadPool = new StaticThreadPool("main", maxStaticPool <= 0 ? Environment.ProcessorCount * 2 : maxStaticPool);
            _dynamicThreadPool = new DynamicThreadPool("main", maxDynamicPool <= 0 ? 64 : maxDynamicPool);

            _initialized = true;
        }

        return ToContext(_mainContextId);
    }

    #region Information

    public static string WhereAmI()
    {
        return GetCurrentContextType() switch
        {
            ThreadContextType.ThreadContext => $"ThreadContext(id={GetCurrentContextId()}; name={GetCurrentContextName()})",
            ThreadContextType.StaticThreadPool => "StaticThreadPool",
            ThreadContextType.DynamicThreadPool => "DynamicThreadPool",
            ThreadContextType.CustomThreadPool => $"CustomThreadPool(name={GetThreadPool().Name}; type={GetThreadPool().GetType().Name})",
            ThreadContextType.UndefinedContext => "UndefinedContext",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static ThreadContextType GetCurrentContextType()
    {
        if (IsThreadPool())
        {
            if (IsStaticThreadPool())
                return ThreadContextType.StaticThreadPool;

            if (IsDynamicThreadPool())
                return ThreadContextType.DynamicThreadPool;

            return ThreadContextType.CustomThreadPool;
        }

        return IsThreadContext() ? ThreadContextType.ThreadContext : ThreadContextType.UndefinedContext;
    }

    #endregion

    #region ThreadContext

    public static int CreateContext(string name = null, SynchronizationContext context = null)
    {
        if (name is null)
        {
            var latestId = 0;
            do
            {
                name = $"ThreadContext{latestId++}";
            } while (!ContextByName.ContainsKey(name));
        }
        else
        {
            if (ContextByName.ContainsKey(name))
                throw new ArgumentException($"ThreadContext name is already exists: {name}");
        }

        var threadContext = new ThreadContext(name, context);
        ContextById.Add(threadContext.Id, threadContext);
        ContextByName.Add(name, threadContext);

        return threadContext.Id;
    }

    public static void RemoveContext(int id)
    {
        if (!ContextById.TryGetValue(id, out var context))
            throw new ArgumentException($"ThreadContext id is not exists: {id}");

        ContextById.Remove(id);
        ContextByName.Remove(context.Name);

        context.Dispose();
    }

    public static void RemoveContext(string name)
    {
        if (!ContextByName.TryGetValue(name, out var context))
            throw new ArgumentException($"ThreadContext name is not exists: {name}");

        ContextByName.Remove(name);
        ContextById.Remove(context.Id);

        context.Dispose();
    }

    public static ThreadContext GetCurrentThreadContext()
    {
        var syn = SynchronizationContext.Current;
        if (syn != null)
            return ContextById.TryGetValue(syn.GetHashCode(), out var context) ? context : null;

        return null;
    }

    public static ThreadContext[] GetContextList()
    {
        return ContextById.Values.ToArray();
    }

    public static int? GetCurrentContextId()
    {
        return GetCurrentThreadContext()?.Id;
    }

    public static string GetCurrentContextName()
    {
        return GetCurrentThreadContext()?.Name;
    }

    public static bool ContainsContext(int id)
    {
        return ContextById.ContainsKey(id);
    }

    public static bool ContainsContext(string name)
    {
        return ContextByName.ContainsKey(name);
    }

    public static ThreadContext GetContext(string name)
    {
        return ContextByName.TryGetValue(name, out var context) ? context : null;
    }

    public static ThreadContext GetContext(int id)
    {
        return ContextById.TryGetValue(id, out var context) ? context : null;
    }

    public static bool IsThreadContext()
    {
        return GetCurrentThreadContext() != null;
    }

    public static IAwaiter ToContext(int id)
    {
        if (!ContextById.ContainsKey(id))
            throw new ArgumentException($"ThreadContext id is not exists: {id}");

        return ContextById[id].Awaiter;
    }

    public static IAwaiter ToContext(string name)
    {
        if (!ContextByName.ContainsKey(name))
            throw new ArgumentException($"ThreadContext name is not exists: {name}");

        return ContextByName[name].Awaiter;
    }

    public static Task ToContext(int id, Action action)
    {
        if (!ContextById.ContainsKey(id))
            throw new ArgumentException($"ThreadContext id is not exists: {id}");

        return ContextById[id].Post(action);
    }

    public static Task ToContext(string name, Action action)
    {
        if (!ContextByName.ContainsKey(name))
            throw new ArgumentException($"ThreadContext name is not exists: {name}");

        return ContextByName[name].Post(action);
    }

    #endregion

    #region MainContext

    public static IAwaiter ToMainContext()
    {
        return ToContext(_mainContextId);
    }

    public static Task ToMainContext(Action action)
    {
        return ToContext(_mainContextId, action);
    }

    public static bool IsMainContext()
    {
        return ContainsContext(_mainContextId);
    }

    #endregion

    #region BackgroundContext

    public static bool IsBackgroundContext()
    {
        return ContainsContext(_backgroundContextId);
    }

    public static IAwaiter ToBackgroundContext()
    {
        return ToContext(_backgroundContextId);
    }

    public static Task ToBackgroundContext(Action action)
    {
        return ToContext(_backgroundContextId, action);
    }

    #endregion

    #region ThreadPool

    public static IAwaiter ToStaticThreadPool()
    {
        return _staticThreadPool.Awaiter;
    }

    public static Task ToStaticThreadPool(Action action)
    {
        return _staticThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
    }

    public static IAwaiter ToDynamicThreadPool()
    {
        return _dynamicThreadPool.Awaiter;
    }

    public static Task ToDynamicThreadPool(Action action)
    {
        return _dynamicThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
    }

    public static AbstractThreadPool GetStaticThreadPool()
    {
        return _staticThreadPool;
    }

    public static AbstractThreadPool GetDynamicThreadPool()
    {
        return _dynamicThreadPool;
    }

    public static AbstractThreadPool GetThreadPool()
    {
        return (AbstractThreadPool) TaskScheduler.Current;
    }

    public static bool IsThreadPool()
    {
        return TaskScheduler.Current is AbstractThreadPool;
    }

    public static bool IsStaticThreadPool()
    {
        return TaskScheduler.Current == _staticThreadPool;
    }

    public static bool IsDynamicThreadPool()
    {
        return TaskScheduler.Current == _dynamicThreadPool;
    }

    #endregion
}