using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HardDev.AsTask.TaskHelpers;

namespace HardDev.AsTask.Awaiters
{
    [StructLayout(LayoutKind.Auto)]
    public struct QTaskFactoryAwaiter : IAwaiter
    {
        public IAwaiter GetAwaiter() => this;
        public bool IsCompleted => TaskScheduler.Current == _taskFactory.Scheduler;

        private readonly TaskFactory _taskFactory;

        public QTaskFactoryAwaiter(TaskFactory taskFactory)
        {
            _taskFactory = taskFactory;
        }

        public void OnCompleted(Action action)
        {
            _taskFactory.Run(action).ConfigureAwait(false);
        }

        public void GetResult()
        {
        }
    }
}