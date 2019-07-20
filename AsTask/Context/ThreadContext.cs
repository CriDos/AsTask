﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HardDev.AsTaskLib.Awaiter;

namespace HardDev.AsTaskLib.Context
{
    public sealed class ThreadContext : IDisposable
    {
        public readonly string Name;
        public readonly SynchronizationContext Context;
        public readonly IAwaiter Awaiter;
        public int Id => _thread.ManagedThreadId;

        private readonly Thread _thread;
        private readonly BlockingCollection<Action> _queueActions = new BlockingCollection<Action>();
        private int _outstandingOperations;

        private ThreadContext(string name)
        {
            Name = name;
            Awaiter = new ThreadContextAwaiter(this);
        }

        public ThreadContext(string name, Action action) : this(name)
        {
            Context = new SynContext(this);
            Context.OperationStarted();

            _thread = Thread.CurrentThread;
            
            Post(action);
        }

        public ThreadContext(string name, SynchronizationContext context) : this(name)
        {
            Context = context;
            Context.OperationStarted();

            _thread = Thread.CurrentThread;
        }

        public ThreadContext(string name, ThreadPriority priority = ThreadPriority.Normal,
            SynchronizationContext context = null) : this(name)
        {
            Context = context ?? new SynContext(this);
            Context.OperationStarted();

            _thread = new Thread(Execute) {Name = name, Priority = priority, IsBackground = true};
            _thread.Start();
        }

        public Task Post(Action action)
        {
            Context.OperationStarted();
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = new Action(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(null);
                }
                catch (OperationCanceledException ex)
                {
                    tcs.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    Context.OperationCompleted();
                }
            });

            Context.Post(state => callback(), null);

            return tcs.Task.ExceptionHandler();
        }

        public void Join()
        {
            _thread.Join();
        }

        public void Execute()
        {
            SynchronizationContext.SetSynchronizationContext(Context);
            foreach (var action in _queueActions.GetConsumingEnumerable())
                action();
        }

        private void Enqueue(SendOrPostCallback d, object state)
        {
            _queueActions.Add(() => d(state));
        }

        private void AllowToExit()
        {
            Context.OperationCompleted();
        }

        private void OperationStarted()
        {
            Interlocked.Increment(ref _outstandingOperations);
        }

        private void OperationCompleted()
        {
            if (Interlocked.Decrement(ref _outstandingOperations) <= 0)
                _queueActions.CompleteAdding();
        }

        public void Dispose()
        {
            AllowToExit();
            _queueActions.Dispose();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ThreadContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        public static bool operator ==(ThreadContext left, ThreadContext right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ThreadContext left, ThreadContext right)
        {
            return !Equals(left, right);
        }

        private bool Equals(ThreadContext other)
        {
            return string.Equals(Name, other.Name);
        }

        public override string ToString()
        {
            return $"{nameof(ThreadContext)}[{nameof(Name)}: {Name}, {nameof(Id)}: {Id}, {nameof(_outstandingOperations)}: {_outstandingOperations}]";
        }

        private sealed class SynContext : SynchronizationContext
        {
            private readonly ThreadContext _context;

            public SynContext(ThreadContext context)
            {
                _context = context;
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                _context.Enqueue(d, state);
            }

            public override void OperationStarted()
            {
                _context.OperationStarted();
            }

            public override void OperationCompleted()
            {
                _context.OperationCompleted();
            }
        }
    }
}