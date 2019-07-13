using System;
using System.Threading.Tasks;

namespace HardDev.AsTaskLib.Context
{
    public static class AsExceptionHandler
    {
        /// <summary>
        /// The specified task exception handler
        /// </summary>
        private static Action<Task> _handler;

        public static void SetExceptionHandler(Action<Task> exceptionHandler)
        {
            _handler = exceptionHandler;
        }

        public static void AddUnhandledException(UnhandledExceptionEventHandler exceptionHandler)
        {
            AppDomain.CurrentDomain.UnhandledException += exceptionHandler;
        }

        public static void AddUnobservedTaskException(EventHandler<UnobservedTaskExceptionEventArgs> exceptionHandler)
        {
            TaskScheduler.UnobservedTaskException += exceptionHandler;
        }

        public static Task ExceptionHandler(this Task task, Action<Task> customHandler = null)
        {
            if (customHandler != null)
                task.ContinueWith(customHandler, TaskContinuationOptions.OnlyOnFaulted);

            if (_handler != null)
                task.ContinueWith(_handler, TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }
    }
}