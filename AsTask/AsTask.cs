using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HardDev.Awaiter;
using HardDev.Context;
using HardDev.Scheduler;

namespace HardDev
{
    public static class AsTask
    {
        public static int OptimalDegreeOfParallelism => Environment.ProcessorCount * 2;
        public static int MinDegreeOfParallelism => Math.Max(Environment.ProcessorCount - 1, 1);

        private const string NOT_INITIALIZE_MSG = "First need to initialize AsTask.";

        private static int _mainContextId = -1;
        private static int _backgroundContextId = -1;

        private static AbstractTaskScheduler _staticThreadPool;
        private static AbstractTaskScheduler _dynamicThreadPool;

        private static readonly Dictionary<int, ThreadContext> ContextById = new Dictionary<int, ThreadContext>();
        private static readonly Dictionary<string, ThreadContext> ContextByName = new Dictionary<string, ThreadContext>();

        private static bool? _isSupportMultithreading;
        private static bool _isInitialized;

        public static IAwaiter Initialize(SynchronizationContext mainContext = null, ThreadPriority backgroundPriority = ThreadPriority.Normal,
            int maxStaticPool = 0, ThreadPriority staticPriority = ThreadPriority.Normal,
            int maxDynamicPool = 64, ThreadPriority dynamicPriority = ThreadPriority.Normal
        )
        {
            if (!_isInitialized)
            {
                _mainContextId = CreateContext("MainContext", mainContext ?? SynchronizationContext.Current, ThreadPriority.Highest);
                _backgroundContextId = CreateContext("BackgroundContext", priority: backgroundPriority);
                _staticThreadPool = new StaticThreadPool("StaticThreadPool",
                    maxStaticPool <= 0 ? OptimalDegreeOfParallelism : maxStaticPool, staticPriority);
                _dynamicThreadPool = new DynamicThreadPool("DynamicThreadPool",
                    maxDynamicPool <= 0 ? 64 : maxDynamicPool, dynamicPriority);

                _isInitialized = true;
            }

            return ToContext(_mainContextId);
        }

        #region Information

        public static string WhereAmI()
        {
            switch (GetCurrentContextType())
            {
                case ThreadContextType.ThreadContext:
                    return $"ThreadContext(id={GetCurrentContextId()}; name={GetCurrentContextName()})";
                case ThreadContextType.StaticThreadPool:
                    return "StaticThreadPool";
                case ThreadContextType.DynamicThreadPool:
                    return "DynamicThreadPool";
                case ThreadContextType.UndefinedThreadPool:
                    return "UndefinedThreadPool";
                case ThreadContextType.UndefinedContext:
                    return "UndefinedContext";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ThreadContextType GetCurrentContextType()
        {
            if (IsThreadPool())
            {
                if (IsStaticThreadPool())
                    return ThreadContextType.StaticThreadPool;

                if (IsDynamicThreadPool())
                    return ThreadContextType.DynamicThreadPool;

                return ThreadContextType.UndefinedThreadPool;
            }

            return IsThreadContext() ? ThreadContextType.ThreadContext : ThreadContextType.UndefinedContext;
        }

        public static bool CheckSupportMultithreading()
        {
            if (_isSupportMultithreading.HasValue)
                return _isSupportMultithreading.Value;

            try
            {
                Task.Run(() => { _isSupportMultithreading = true; }).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                _isSupportMultithreading = false;
            }

            return _isSupportMultithreading.GetValueOrDefault();
        }

        #endregion

        #region ThreadContext

        public static int CreateContext(string name, SynchronizationContext context = null, ThreadPriority priority = ThreadPriority.Normal)
        {
            if (ContextByName.ContainsKey(name))
                throw new ArgumentException($"ThreadContext name is already exists: {name}");

            var threadContext = new ThreadContext(name, context, priority);
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

        public static ThreadContext CurrentThreadContext =>
            ContextById.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var context) ? context : null;

        public static ThreadContext[] GetContextList()
        {
            return ContextById.Values.ToArray();
        }

        public static int? GetCurrentContextId()
        {
            return CurrentThreadContext?.Id;
        }

        public static string GetCurrentContextName()
        {
            return CurrentThreadContext?.Name;
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
            return ContextById.ContainsKey(Thread.CurrentThread.ManagedThreadId);
        }

        public static bool IsThreadContext(int id)
        {
            return ContextById.ContainsKey(id);
        }

        public static IAwaiter ToContext(int id)
        {
            if (!ContextById.ContainsKey(id))
                throw new ArgumentException($"ThreadContext id is not exists: {id}");

            return ContextById[id].Awaiter;
        }

        public static Task ToContext(int id, Action action)
        {
            if (!ContextById.ContainsKey(id))
                throw new ArgumentException($"ThreadContext id is not exists: {id}");

            return ContextById[id].Post(action);
        }

        #endregion

        #region MainContext

        public static IAwaiter ToMainContext()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return ToContext(_mainContextId);
        }

        public static Task ToMainContext(Action action)
        {
            if (!_isInitialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return ToContext(_mainContextId, action);
        }

        public static bool IsMainContext()
        {
            return IsThreadContext(_mainContextId);
        }

        #endregion

        #region BackgroundContext

        public static bool IsBackgroundContext()
        {
            return IsThreadContext(_backgroundContextId);
        }

        public static IAwaiter ToBackgroundContext()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return ToContext(_backgroundContextId);
        }

        public static Task ToBackgroundContext(Action action)
        {
            if (!_isInitialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return ToContext(_backgroundContextId, action);
        }

        #endregion

        #region ThreadPool

        public static IAwaiter ToStaticThreadPool()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _staticThreadPool.Awaiter;
        }

        public static Task ToStaticThreadPool(Action action)
        {
            if (!_isInitialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _staticThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
        }

        public static IAwaiter ToDynamicThreadPool()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _dynamicThreadPool.Awaiter;
        }

        public static Task ToDynamicThreadPool(Action action)
        {
            if (!_isInitialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _dynamicThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
        }

        public static AbstractTaskScheduler GetStaticTaskScheduler()
        {
            return _staticThreadPool;
        }

        public static AbstractTaskScheduler GetDynamicTaskScheduler()
        {
            return _dynamicThreadPool;
        }

        public static bool IsThreadPool()
        {
            return TaskScheduler.Current is AbstractTaskScheduler;
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
}