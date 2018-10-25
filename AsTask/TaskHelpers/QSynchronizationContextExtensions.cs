﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace HardDev.AsTask.TaskHelpers
{
    /// <summary>
    /// Provides extension methods for <see cref="SynchronizationContext"/>.
    /// </summary>
    public static class QSynchronizationContextExtensions
    {
        /// <summary>
        /// Synchronously executes a delegate on this synchronization context.
        /// </summary>
        /// <param name="this">The synchronization context.</param>
        /// <param name="action">The delegate to execute.</param>
        public static void Send(this SynchronizationContext @this, Action action)
        {
            @this.Send(state => ((Action) state)(), action);
        }

        /// <summary>
        /// Synchronously executes a delegate on this synchronization context and returns its result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="this">The synchronization context.</param>
        /// <param name="action">The delegate to execute.</param>
        public static T Send<T>(this SynchronizationContext @this, Func<T> action)
        {
            var result = default(T);
            @this.Send(state => { result = ((Func<T>) state)(); }, action);
            return result;
        }

        /// <summary>
        /// Asynchronously executes a delegate on this synchronization context.
        /// </summary>
        /// <param name="synchronizationContext">The synchronization context.</param>
        /// <param name="action">The delegate to execute.</param>
        public static Task PostAsync(this SynchronizationContext synchronizationContext, Action action)
        {
            var tcs = QTaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            synchronizationContext.Post(state =>
            {
                try
                {
                    ((Action) state)();
                    tcs.TrySetResult(null);
                }
                catch(OperationCanceledException ex)
                {
                    tcs.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, action);
            return tcs.Task;
        }

        /// <summary>
        /// Asynchronously executes a delegate on this synchronization context and returns its result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="this">The synchronization context.</param>
        /// <param name="action">The delegate to execute.</param>
        public static Task<T> PostAsync<T>(this SynchronizationContext @this, Func<T> action)
        {
            var tcs = QTaskCompletionSourceExtensions.CreateAsyncTaskSource<T>();
            @this.Post(state =>
            {
                try
                {
                    tcs.SetResult(((Func<T>) state)());
                }
                catch(OperationCanceledException ex)
                {
                    tcs.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, action);
            return tcs.Task;
        }

        /// <summary>
        /// Asynchronously executes an asynchronous delegate on this synchronization context.
        /// </summary>
        /// <param name="this">The synchronization context.</param>
        /// <param name="action">The delegate to execute.</param>
        public static Task PostAsync(this SynchronizationContext @this, Func<Task> action)
        {
            var tcs = QTaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            @this.Post(async state =>
            {
                try
                {
                    await ((Func<Task>) state)().ConfigureAwait(false);
                    tcs.TrySetResult(null);
                }
                catch(OperationCanceledException ex)
                {
                    tcs.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, action);
            return tcs.Task;
        }

        /// <summary>
        /// Asynchronously executes an asynchronous delegate on this synchronization context and returns its result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="this">The synchronization context.</param>
        /// <param name="action">The delegate to execute.</param>
        public static Task<T> PostAsync<T>(this SynchronizationContext @this, Func<Task<T>> action)
        {
            var tcs = QTaskCompletionSourceExtensions.CreateAsyncTaskSource<T>();
            @this.Post(async state =>
            {
                try
                {
                    tcs.SetResult(await ((Func<Task<T>>) state)().ConfigureAwait(false));
                }
                catch(OperationCanceledException ex)
                {
                    tcs.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, action);
            return tcs.Task;
        }
    }
}