using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HardDev.AsTaskLib.Awaiter
{
    [StructLayout(LayoutKind.Auto)]
    public struct TaskFactoryAwaiter : IAwaiter
    {
        public IAwaiter GetAwaiter() => this;
        public bool IsCompleted => TaskScheduler.Current == _taskFactory.Scheduler;

        private readonly TaskFactory _taskFactory;

        public TaskFactoryAwaiter(TaskFactory taskFactory)
        {
            _taskFactory = taskFactory;
        }

        public void OnCompleted(Action action)
        {
            _taskFactory.StartNew(action, _taskFactory.CancellationToken,
                _taskFactory.CreationOptions | TaskCreationOptions.DenyChildAttach,
                _taskFactory.Scheduler ?? TaskScheduler.Default);
        }

        public void GetResult()
        {
        }
    }
}