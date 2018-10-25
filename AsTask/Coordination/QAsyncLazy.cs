﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HardDev.AsTask.Coordination
{
    /// <summary>
    /// Flags controlling the behavior of <see cref="QAsyncLazy{T}"/>.
    /// </summary>
    [Flags]
    public enum AsyncLazyFlags
    {
        /// <summary>
        /// No special flags. The factory method is executed on a thread pool thread, and does not retry initialization on failures (failures are cached).
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Execute the factory method on the calling thread.
        /// </summary>
        ExecuteOnCallingThread = 0x1,

        /// <summary>
        /// If the factory method fails, then re-run the factory method the next time instead of caching the failed task.
        /// </summary>
        RetryOnFailure = 0x2,
    }

    /// <summary>
    /// Provides support for asynchronous lazy initialization. This type is fully threadsafe.
    /// </summary>
    /// <typeparam name="T">The type of object that is being asynchronously initialized.</typeparam>
    public sealed class QAsyncLazy<T>
    {
        /// <summary>
        /// The synchronization object protecting <c>_instance</c>.
        /// </summary>
        private readonly object _mutex;

        /// <summary>
        /// The factory method to call.
        /// </summary>
        private readonly Func<Task<T>> _factory;

        /// <summary>
        /// The underlying lazy task.
        /// </summary>
        private Lazy<Task<T>> _instance;

        /// <summary>
        /// The semi-unique identifier for this instance. This is 0 if the id has not yet been created.
        /// </summary>
        private int _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="QAsyncLazy{T}"/> class.
        /// </summary>
        /// <param name="factory">The asynchronous delegate that is invoked to produce the value when it is needed. May not be <c>null</c>.</param>
        /// <param name="flags">Flags to influence async lazy semantics.</param>
        public QAsyncLazy(Func<Task<T>> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            if ((flags & AsyncLazyFlags.RetryOnFailure) == AsyncLazyFlags.RetryOnFailure)
                _factory = RetryOnFailure(_factory);
            if ((flags & AsyncLazyFlags.ExecuteOnCallingThread) != AsyncLazyFlags.ExecuteOnCallingThread)
                _factory = RunOnThreadPool(_factory);

            _mutex = new object();
            _instance = new Lazy<Task<T>>(_factory);
        }

        /// <summary>
        /// Gets a semi-unique identifier for this asynchronous lazy instance.
        /// </summary>
        public int Id => QIdManager<QAsyncLazy<object>>.GetId(ref _id);

        /// <summary>
        /// Whether the asynchronous factory method has started. This is initially <c>false</c> and becomes <c>true</c> when this instance is awaited or after <see cref="Start"/> is called.
        /// </summary>
        public bool IsStarted
        {
            get
            {
                lock (_mutex)
                    return _instance.IsValueCreated;
            }
        }

        /// <summary>
        /// Starts the asynchronous factory method, if it has not already started, and returns the resulting task.
        /// </summary>
        public Task<T> Task
        {
            get
            {
                lock (_mutex)
                    return _instance.Value;
            }
        }

        private Func<Task<T>> RetryOnFailure(Func<Task<T>> factory)
        {
            return async () =>
            {
                try
                {
                    return await factory().ConfigureAwait(false);
                }
                catch
                {
                    lock (_mutex)
                    {
                        _instance = new Lazy<Task<T>>(_factory);
                    }

                    throw;
                }
            };
        }

        private Func<Task<T>> RunOnThreadPool(Func<Task<T>> factory)
        {
            return () => System.Threading.Tasks.Task.Run(factory);
        }

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="QAsyncLazy{T}"/> to be awaited.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TaskAwaiter<T> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="QAsyncLazy{T}"/> to be awaited.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
        {
            return Task.ConfigureAwait(continueOnCapturedContext);
        }

        /// <summary>
        /// Starts the asynchronous initialization, if it has not already started.
        /// </summary>
        public void Start()
        {
// ReSharper disable UnusedVariable
            var unused = Task;
// ReSharper restore UnusedVariable
        }

        internal enum LazyState
        {
            NotStarted,
            Executing,
            Completed
        }
    }
}