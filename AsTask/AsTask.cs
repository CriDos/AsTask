using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTaskLib.Awaiter;
using HardDev.AsTaskLib.Context;
using HardDev.AsTaskLib.Scheduler;

namespace HardDev.AsTaskLib
{
    public static class AsTask
    {
        public static int OptimalDegreeOfParallelism => Environment.ProcessorCount * 2;
        public static int MinDegreeOfParallelism => Math.Max(Environment.ProcessorCount - 1, 1);

        private const string NOT_INITIALIZE_MSG = "First need to initialize AsTask.";

        private static int _mainContextId;
        private static int _backgroundContextId;

        private static ThreadPoolScheduler _normalThreadPool;
        private static ThreadPoolScheduler _blockingThreadPool;

        private static readonly object ContextLocker = new object();
        private static readonly Dictionary<int, AsContext> ContextById = new Dictionary<int, AsContext>();
        private static readonly Dictionary<string, AsContext> ContextByName = new Dictionary<string, AsContext>();

        private static volatile bool _initialized;
        private static bool? _isSupportMultithreading;

        #region Initialize

        public static void Initialize(int maxNormalThreadPool = 0, int maxBlockingThreadPool = 64, SynchronizationContext mc = null)
        {
            if (_initialized)
                return;

            if (!CheckSupportMultithreading())
                throw new PlatformNotSupportedException("Target platform not supported multithreading.");

            _initialized = true;

            try
            {
                _mainContextId = CreateContext("MainContext", mc ?? SynchronizationContext.Current);
                _backgroundContextId = CreateContext("BackgroundContext");

                _normalThreadPool = new ThreadPoolScheduler(maxNormalThreadPool <= 0 ? OptimalDegreeOfParallelism : maxNormalThreadPool);
                _blockingThreadPool = new ThreadPoolScheduler(maxBlockingThreadPool);
            }
            catch (Exception)
            {
                _initialized = false;
                throw;
            }
        }

        #endregion

        #region Information

        public static string WhereAmI()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            var contextName = GetCurrentContextName();
            switch (GetCurrentContextType())
            {
                case AsContextType.AsyncContext:
                    return $"AsContext(id={GetCurrentContextId()}; name={contextName})";
                case AsContextType.NormalThreadPool:
                    return "NormalThreadPool";
                case AsContextType.BlockingThreadPool:
                    return "BlockingThreadPool";
                case AsContextType.UndefinedThreadPool:
                    return "UndefinedThreadPool";
                case AsContextType.UndefinedContext:
                    return "UndefinedContext";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static AsContextType GetCurrentContextType()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            if (IsThreadPool())
            {
                if (IsNormalThreadPool())
                    return AsContextType.NormalThreadPool;

                if (IsBlockingThreadPool())
                    return AsContextType.BlockingThreadPool;

                return AsContextType.UndefinedThreadPool;
            }

            return IsAsContext() ? AsContextType.AsyncContext : AsContextType.UndefinedContext;
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

        #region AsContext

        public static int CreateContext(string name, SynchronizationContext context = null)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            lock (ContextLocker)
            {
                if (ContextByName.ContainsKey(name))
                    throw new ArgumentException($"AsContext name is already exists: {name}");

                var asContext = new AsContext(name, context);
                ContextById.Add(asContext.Id, asContext);
                ContextByName.Add(name, asContext);

                return asContext.Id;
            }
        }

        public static void RemoveContext(int id)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            lock (ContextLocker)
            {
                if (!ContextById.TryGetValue(id, out var context))
                    throw new ArgumentException($"AsContext id is not exists: {id}");

                ContextById.Remove(id);
                ContextByName.Remove(context.Name);

                context.AllowToExit();
                context.Dispose();
            }
        }

        public static void RemoveContext(string name)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            lock (ContextLocker)
            {
                if (!ContextByName.TryGetValue(name, out var context))
                    throw new ArgumentException($"AsContext name is not exists: {name}");

                ContextByName.Remove(name);
                ContextById.Remove(context.Id);

                context.AllowToExit();
                context.Dispose();
            }
        }

        public static int? GetCurrentContextId()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return AsContext.Current?.Id;
        }

        public static string GetCurrentContextName()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return AsContext.Current?.Name;
        }

        public static bool ContainsContext(int id)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            lock (ContextLocker)
            {
                return ContextById.ContainsKey(id);
            }
        }

        public static bool ContainsContext(string name)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            lock (ContextLocker)
            {
                return ContextByName.ContainsKey(name);
            }
        }

        public static AsContext GetContext(string name)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            lock (ContextLocker)
            {
                return ContextByName.ContainsKey(name) ? ContextByName[name] : null;
            }
        }

        /// <summary>
        /// Returns true if called from the custom context, and false otherwise.
        /// </summary>
        public static bool IsAsContext()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return AsContext.Current != null;
        }

        /// <summary>
        /// Returns true if called from the custom context, and false otherwise.
        /// </summary>
        public static bool IsAsContext(int id)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            if (!ContainsContext(id))
                return false;

            lock (ContextLocker)
            {
                return AsContext.Current == ContextById[id];
            }
        }

        /// <summary>
        /// Switches execution to the custom context
        /// </summary>
        public static IAwaiter ToContext(int id)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            lock (ContextLocker)
            {
                if (!ContextById.ContainsKey(id))
                    throw new ArgumentException($"AsContext id is not exists: {id}");

                return ContextById[id].Awaiter;
            }
        }

        /// <summary>
        /// Post action to the custom context
        /// </summary>
        public static Task ToContext(int id, Action action)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            lock (ContextLocker)
            {
                if (!ContextById.ContainsKey(id))
                    throw new ArgumentException($"AsContext id is not exists: {id}");

                return ContextById[id].PostAsync(action);
            }
        }

        #endregion

        #region MainContext

        /// <summary>
        /// Switches execution to the main context.
        /// </summary>
        public static IAwaiter ToMainContext()
        {
            return ToContext(_mainContextId);
        }

        /// <summary>
        /// Post action to the main context.
        /// </summary>
        public static Task ToMainContext(Action action)
        {
            return ToContext(_mainContextId, action);
        }

        /// <summary>
        /// Returns true if called from the main context, and false otherwise.
        /// </summary>
        public static bool IsMainContext()
        {
            return IsAsContext(_mainContextId);
        }

        #endregion

        #region BackgroundContext

        /// <summary>
        /// Returns true if called from the background context, and false otherwise.
        /// </summary>
        public static bool IsBackgroundContext()
        {
            return IsAsContext(_backgroundContextId);
        }

        /// <summary>
        /// Switches execution to the background context
        /// </summary>
        public static IAwaiter ToBackgroundContext()
        {
            return ToContext(_backgroundContextId);
        }

        /// <summary>
        /// Post action to the background context
        /// </summary>
        public static Task ToBackgroundContext(Action action)
        {
            return ToContext(_backgroundContextId, action);
        }

        #endregion

        #region ThreadPool

        /// <summary>
        /// Switches execution to a normal thread pool.
        /// </summary>
        public static IAwaiter ToNormalThreadPool()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _normalThreadPool.Awaiter;
        }

        /// <summary>
        /// Start action to a normal thread pool.
        /// </summary>
        public static Task ToNormalThreadPool(Action action)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _normalThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
        }

        /// <summary>
        /// Switches execution to a blocking operations thread pool.
        /// </summary>
        public static IAwaiter ToBlockingThreadPool()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _blockingThreadPool.Awaiter;
        }

        /// <summary>
        /// Start action to a blocking operations thread pool.
        /// </summary>
        public static Task ToBlockingThreadPool(Action action)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _blockingThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
        }

        /// <summary>
        /// Returns true if called from the thread pool, and false otherwise.
        /// </summary>
        public static bool IsThreadPool()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return IsNormalThreadPool() || IsBlockingThreadPool();
        }

        /// <summary>
        /// Returns true if called from the normal thread pool scheduler, and false otherwise.
        /// </summary>
        public static bool IsNormalThreadPool()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return TaskScheduler.Current == _normalThreadPool;
        }

        /// <summary>
        /// Returns true if called from the blocking thread pool scheduler, and false otherwise.
        /// </summary>
        public static bool IsBlockingThreadPool()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return TaskScheduler.Current == _blockingThreadPool;
        }

        public static ThreadPoolScheduler GetNormalTaskScheduler()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _normalThreadPool;
        }

        public static ThreadPoolScheduler GetBlockingTaskScheduler()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _blockingThreadPool;
        }

        #endregion
    }
}