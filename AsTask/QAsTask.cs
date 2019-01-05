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
        public static int OptimalDegreeOfParallelism => Math.Max(Environment.ProcessorCount - 1, 1);
        public const int MAX_BLOCKING_THREAD_POOL = 32;

        private const string NOT_INITIALIZE_MSG = "First need to initialize AsTask.";

        private static QAsyncContextThread _mainContextThread;
        private static SynchronizationContext _mainContext;
        private static IAwaiter _mainAwaiter;

        private static int _backgroundThreadId;

        private static QNormalTaskScheduler _normaThreadPool;
        private static QBlockingTaskScheduler _blockingThreadPool;

        private static readonly Dictionary<int, QAsyncContextThread> AsyncContextThreads = new Dictionary<int, QAsyncContextThread>();
        private static readonly Dictionary<string, int> AsyncContextThreadIdByName = new Dictionary<string, int>();

        private static bool _initialized;
        private static bool? _isSupportMultithreading;

        #region Initialize

        public static void Initialize(bool enableOptimalParallelism = false, SynchronizationContext mainSynContext = null)
        {
            if (_initialized)
                return;

            if (enableOptimalParallelism)
                Initialize(OptimalDegreeOfParallelism, MAX_BLOCKING_THREAD_POOL, mainSynContext);
            else
                Initialize(Environment.ProcessorCount, MAX_BLOCKING_THREAD_POOL, mainSynContext);
        }

        public static void Initialize(int maxNormalThreadPool, int maxBlockingThreadPool, SynchronizationContext mainSynContext = null)
        {
            if (_initialized)
                return;

            if (!CheckSupportMultithreading())
                throw new PlatformNotSupportedException("Target platform not supported multithreading.");

            _initialized = true;

            try
            {
                _mainContext = mainSynContext ?? SynchronizationContext.Current;
                if (_mainContext == null)
                {
                    _mainContextThread = new QAsyncContextThread("MainContext");
                    _mainContext = _mainContextThread.Context.SynchronizationContext;
                    _mainAwaiter = _mainContextThread.Context.Awaiter;
                }
                else
                    _mainAwaiter = new QSynchronizationContextAwaiter(_mainContext);

                _normaThreadPool = new QNormalTaskScheduler(maxNormalThreadPool);
                _blockingThreadPool = new QBlockingTaskScheduler(maxBlockingThreadPool);

                _backgroundThreadId = CreateAsyncContextThread("BackgroundThread");
            }
            catch (Exception)
            {
                _initialized = false;
            }
        }

        #endregion

        #region Information

        public static string WhereAmI()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

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
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

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

        public static bool CheckSupportMultithreading()
        {
            if (_isSupportMultithreading.HasValue)
                return _isSupportMultithreading.Value;

            try
            {
                Task.Run(() => { _isSupportMultithreading = true; }).Wait();
            }
            catch (Exception)
            {
                _isSupportMultithreading = false;
            }

            return _isSupportMultithreading.GetValueOrDefault();
        }

        #endregion

        #region Exception Handling

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
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _mainAwaiter;
        }

        /// <summary>
        /// Post action to the main thread.
        /// </summary>
        public static Task ToMainThread(Action action)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _mainContext.PostAsync(action).ExceptionHandler();
        }

        /// <summary>
        /// Returns true if called from the Unity's main thread, and false otherwise.
        /// </summary>
        public static bool IsMainThread()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return SynchronizationContext.Current == _mainContext;
        }

        #endregion

        #region AsyncTaskContext

        public static int CreateAsyncContextThread(string name)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            if (AsyncContextThreadIdByName.ContainsKey(name))
                throw new ArgumentException($"AsyncContext name is already exists: {name}");

            var contextThread = new QAsyncContextThread(name);
            AsyncContextThreads.Add(contextThread.Id, contextThread);
            AsyncContextThreadIdByName.Add(name, contextThread.Id);
            return contextThread.Id;
        }

        public static void RemoveAsyncContextThread(string name)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            if (!AsyncContextThreadIdByName.TryGetValue(name, out var threadId))
                throw new ArgumentException($"AsyncContext name is not exists: {name}");

            AsyncContextThreads[threadId].Dispose();
            AsyncContextThreads.Remove(threadId);
            AsyncContextThreadIdByName.Remove(name);
        }

        public static int GetCurrentAsyncContextId()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            var context = QAsyncContext.Current;
            if (context != null)
                return context.Id;

            return -1;
        }

        public static string GetCurrentAsyncContextName()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return QAsyncContext.Current?.Name;
        }

        public static bool ContainsAsyncContextThread(int id)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return AsyncContextThreadIdByName.ContainsValue(id);
        }

        public static bool ContainsAsyncContextThread(string name)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return AsyncContextThreadIdByName.ContainsKey(name);
        }

        /// <summary>
        /// Returns true if called from the AsyncContextThread thread, and false otherwise.
        /// </summary>
        public static bool IsAsyncContextThread()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return QAsyncContext.Current != null;
        }

        /// <summary>
        /// Returns true if called from the AsyncContextThread thread, and false otherwise.
        /// </summary>
        public static bool IsAsyncContextThread(int id)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            if (!ContainsAsyncContextThread(id))
                return false;

            return QAsyncContext.Current == AsyncContextThreads[id].Context;
        }

        /// <summary>
        /// Switches execution to the AsyncContextThread
        /// </summary>
        public static IAwaiter ToAsyncContextThread(int id)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return AsyncContextThreads[id].Context.Awaiter;
        }

        /// <summary>
        /// Post action to the AsyncContextThread
        /// </summary>
        public static Task ToAsyncContextThread(int id, Action action)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return AsyncContextThreads[id].Context.SynchronizationContext.PostAsync(action).ExceptionHandler();
        }

        #endregion

        #region BackgroundThread

        /// <summary>
        /// Returns true if called from the background thread, and false otherwise.
        /// </summary>
        public static bool IsBackgroundThread()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return IsAsyncContextThread(_backgroundThreadId);
        }

        /// <summary>
        /// Switches execution to the background thread
        /// </summary>
        public static IAwaiter ToBackgroundThread()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return ToAsyncContextThread(_backgroundThreadId);
        }

        /// <summary>
        /// Post action to the background thread
        /// </summary>
        public static Task ToBackgroundThread(Action action)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return ToAsyncContextThread(_backgroundThreadId, action);
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

            return _normaThreadPool.Awaiter;
        }

        /// <summary>
        /// Start action to a normal thread pool.
        /// </summary>
        public static Task ToNormalThreadPool(Action action)
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _normaThreadPool.TaskFactory.StartNew(action).ExceptionHandler();
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

            return TaskScheduler.Current == _normaThreadPool;
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

        public static QNormalTaskScheduler GetNormalTaskScheduler()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _normaThreadPool;
        }

        public static QBlockingTaskScheduler GetBlockingTaskScheduler()
        {
            if (!_initialized)
                throw new InvalidOperationException(NOT_INITIALIZE_MSG);

            return _blockingThreadPool;
        }

        #endregion
    }
}