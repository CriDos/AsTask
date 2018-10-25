using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTask.Awaiters;
using HardDev.AsTask.Context;
using HardDev.AsTask.TaskHelpers;
using HardDev.AsTask.TaskSchedulers;

namespace HardDev.AsTask
{
    public static class QAsTask
    {
        public const int MAX_BLOCKING_THREAD_POOL = 16;

        public static int OptimalDegreeOfParallelism { get; } = Math.Max(Environment.ProcessorCount - 1, 1);

        private static QAsyncContextThread _mainContextThread;
        private static SynchronizationContext _mainContext;
        private static IAwaiter _mainAwaiter;

        private static int _backgroundThreadId;

        private static QLimitedTaskScheduler _normaThreadPool;
        private static QLimitedTaskScheduler _blockingThreadPool;

        private static readonly Dictionary<int, QAsyncContextThread> AsyncContextThreads = new Dictionary<int, QAsyncContextThread>();
        private static readonly Dictionary<string, int> AsyncContextThreadIdByName = new Dictionary<string, int>();

        private static bool _initialized;


        public static void Initialize(bool enableOptimalParallelism = true, SynchronizationContext mainSynContext = null)
        {
            if (_initialized)
                return;

            if (enableOptimalParallelism)
                Initialize(OptimalDegreeOfParallelism);
            else
                Initialize(Environment.ProcessorCount);
        }

        public static void Initialize(int maxNormalThreadPool, SynchronizationContext mainSynContext = null)
        {
            if (_initialized)
                return;

            _mainContext = mainSynContext ?? SynchronizationContext.Current;
            if (_mainContext == null)
            {
                _mainContextThread = new QAsyncContextThread("MainContext");
                _mainContext = _mainContextThread.Context.SynchronizationContext;
                _mainAwaiter = _mainContextThread.Context.Awaiter;
            }
            else
                _mainAwaiter = new QSynchronizationContextAwaiter(_mainContext);

            _normaThreadPool = new QLimitedTaskScheduler(maxNormalThreadPool);
            _blockingThreadPool = new QLimitedTaskScheduler(MAX_BLOCKING_THREAD_POOL);

            _backgroundThreadId = CreateAsyncContextThread("BackgroundThread");

            _initialized = true;
        }

        public static string WhereAmI()
        {
            switch (GetCurrentContextType())
            {
                case QAsyncContextType.MainThread:
                    return "MainThread";
                case QAsyncContextType.AsyncContextThread:
                    return $"AsyncContextThread(id={GetCurrentAsyncContextId()}; name={QAsyncContext.Current.Name})";
                case QAsyncContextType.NormalThreadPool:
                    return "NormalThreadPool";
                case QAsyncContextType.BlockingThreadPool:
                    return "BlockingThreadPool";
                case QAsyncContextType.UndefinedThreadPool:
                    return "UndefinedThreadPool";
                case QAsyncContextType.UndefinedThread:
                    return "UndefinedThread";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static QAsyncContextType GetCurrentContextType()
        {
            if (IsMainThread())
                return QAsyncContextType.MainThread;

            if (IsAsyncContextThread())
                return QAsyncContextType.AsyncContextThread;

            if (IsThreadPool())
            {
                if (IsNormalThreadPool())
                    return QAsyncContextType.NormalThreadPool;

                if (IsBlockingThreadPool())
                    return QAsyncContextType.BlockingThreadPool;

                return QAsyncContextType.UndefinedThreadPool;
            }

            return QAsyncContextType.UndefinedThread;
        }

        #region Exceptions

        public static void AddUnhandledException(UnhandledExceptionEventHandler exceptionHandler)
        {
            AppDomain.CurrentDomain.UnhandledException += exceptionHandler;
        }


        public static void AddUnobservedTaskException(EventHandler<UnobservedTaskExceptionEventArgs> exceptionHandler)
        {
            TaskScheduler.UnobservedTaskException += exceptionHandler;
        }

        public static void SetExceptionHandler(Action<Task> exceptionHandler)
        {
            QSynchronousTaskExtensions.SetExceptionHandler(exceptionHandler);
        }

        #endregion

        #region MainThread

        /// <summary>
        /// Switches execution to the main thread.
        /// </summary>
        public static IAwaiter ToMainThread()
        {
            return _mainAwaiter;
        }

        /// <summary>
        /// Post action to the main thread.
        /// </summary>
        public static Task ToMainThread(Action action)
        {
            return _mainContext.PostAsync(action).ExceptionHandler();
        }

        /// <summary>
        /// Returns true if called from the Unity's main thread, and false otherwise.
        /// </summary>
        public static bool IsMainThread()
        {
            return SynchronizationContext.Current == _mainContext;
        }

        #endregion

        #region AsyncTaskContext

        public static int CreateAsyncContextThread(string name)
        {
            if (AsyncContextThreadIdByName.ContainsKey(name))
                throw new ArgumentException($"AsyncContext name is already exists: {name}");

            var contextThread = new QAsyncContextThread(name);
            AsyncContextThreads.Add(contextThread.Id, contextThread);
            AsyncContextThreadIdByName.Add(name, contextThread.Id);
            return contextThread.Id;
        }

        public static void RemoveAsyncContextThread(string name)
        {
            if (!AsyncContextThreadIdByName.TryGetValue(name, out var threadId))
                throw new ArgumentException($"AsyncContext name is not exists: {name}");

            AsyncContextThreads[threadId].Dispose();
            AsyncContextThreads.Remove(threadId);
            AsyncContextThreadIdByName.Remove(name);
        }

        public static int GetCurrentAsyncContextId()
        {
            var context = QAsyncContext.Current;
            if (context != null)
                return context.Id;

            return -1;
        }

        public static string GetCurrentAsyncContextName()
        {
            return QAsyncContext.Current?.Name;
        }

        public static bool ContainsAsyncContextThread(int id)
        {
            return AsyncContextThreadIdByName.ContainsValue(id);
        }

        public static bool ContainsAsyncContextThread(string name)
        {
            return AsyncContextThreadIdByName.ContainsKey(name);
        }

        /// <summary>
        /// Returns true if called from the AsyncContextThread thread, and false otherwise.
        /// </summary>
        public static bool IsAsyncContextThread()
        {
            return QAsyncContext.Current != null;
        }

        /// <summary>
        /// Returns true if called from the AsyncContextThread thread, and false otherwise.
        /// </summary>
        public static bool IsAsyncContextThread(int id)
        {
            if (!ContainsAsyncContextThread(id))
                return false;

            return QAsyncContext.Current == AsyncContextThreads[id].Context;
        }

        /// <summary>
        /// Switches execution to the AsyncContextThread
        /// </summary>
        public static IAwaiter ToAsyncContextThread(int id)
        {
            return AsyncContextThreads[id].Context.Awaiter;
        }

        /// <summary>
        /// Post action to the AsyncContextThread
        /// </summary>
        public static Task ToAsyncContextThread(int id, Action action)
        {
            return AsyncContextThreads[id].Context.SynchronizationContext.PostAsync(action).ExceptionHandler();
        }

        #endregion

        #region BackgroundThread

        /// <summary>
        /// Returns true if called from the background thread, and false otherwise.
        /// </summary>
        public static bool IsBackgroundThread()
        {
            return IsAsyncContextThread(_backgroundThreadId);
        }

        /// <summary>
        /// Switches execution to the background thread
        /// </summary>
        public static IAwaiter ToBackgroundThread()
        {
            return ToAsyncContextThread(_backgroundThreadId);
        }

        /// <summary>
        /// Post action to the background thread
        /// </summary>
        public static Task ToBackgroundThread(Action action)
        {
            return ToAsyncContextThread(_backgroundThreadId, action);
        }

        #endregion

        #region TaskPool

        /// <summary>
        /// Switches execution to a normal thread pool.
        /// </summary>
        public static IAwaiter ToNormalThreadPool()
        {
            return _normaThreadPool.Awaiter;
        }

        /// <summary>
        /// Start action to a normal thread pool.
        /// </summary>
        public static Task ToNormalThreadPool(Action action)
        {
            return _normaThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
        }

        /// <summary>
        /// Switches execution to a blocking operations thread pool.
        /// </summary>
        public static IAwaiter ToBlockingThreadPool()
        {
            return _blockingThreadPool.Awaiter;
        }

        /// <summary>
        /// Start action to a blocking operations thread pool.
        /// </summary>
        public static Task ToBlockingThreadPool(Action action)
        {
            return _blockingThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
        }

        /// <summary>
        /// Returns true if called from the thread pool, and false otherwise.
        /// </summary>
        public static bool IsThreadPool()
        {
            return IsNormalThreadPool() || IsBlockingThreadPool();
        }

        /// <summary>
        /// Returns true if called from the normal thread pool scheduler, and false otherwise.
        /// </summary>
        public static bool IsNormalThreadPool()
        {
            return TaskScheduler.Current == _normaThreadPool;
        }

        /// <summary>
        /// Returns true if called from the blocking thread pool scheduler, and false otherwise.
        /// </summary>
        public static bool IsBlockingThreadPool()
        {
            return TaskScheduler.Current == _blockingThreadPool;
        }

        public static TaskScheduler GetNormalTaskScheduler()
        {
            return _normaThreadPool;
        }

        public static TaskScheduler GetBlockingTaskScheduler()
        {
            return _blockingThreadPool;
        }

        #endregion
    }
}