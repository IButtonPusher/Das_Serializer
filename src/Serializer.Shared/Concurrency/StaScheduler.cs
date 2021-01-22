using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Serializer.Concurrency
{
    public class StaScheduler : TaskScheduler
    {
        public StaScheduler(String staThreadName)
        {
            _staThreadName = staThreadName;
            _taskQueue = new BlockingCollection<Task>();
            Start();
        }

        public void BeginInvoke(Action action)
        {
            Task.Factory.StartNew(action, CancellationToken.None,
                TaskCreationOptions.PreferFairness, this);
        }

        public void Invoke(Action action)
        {
            InvocationBase.RunSync(() => Task.Factory.StartNew(action, CancellationToken.None,
                TaskCreationOptions.PreferFairness, this));
        }

        public void Invoke(Action action, Int32 priority)
        {
            throw new NotImplementedException();
        }

        public async Task InvokeAsync(Action action)
        {
            await Task.Factory.StartNew(action,
                CancellationToken.None, TaskCreationOptions.PreferFairness, this);
        }

        public async Task<T> InvokeAsync<T>(Func<T> action)
        {
            return await Task.Factory.StartNew(action, CancellationToken.None,
                TaskCreationOptions.PreferFairness, this);
        }

        public async Task<TOutput> InvokeAsync<TInput, TOutput>(TInput input,
                                                                Func<TInput, TOutput> action)
        {
            return await Task.Factory.StartNew(() => action(input), CancellationToken.None,
                TaskCreationOptions.PreferFairness, this);
        }

        public async Task InvokeAsync<TInput>(TInput input, Func<TInput, Task> action)
        {
            await action(input);
        }

        public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            var ran = await Task.Factory.StartNew(() => InnerInvokeAsync(action),
                CancellationToken.None,
                TaskCreationOptions.PreferFairness, this);
            return await ran;
        }

        public T Invoke<T>(Func<T> action)
        {
            return InvocationBase.RunSync(() => Task.Factory.StartNew(action, CancellationToken.None,
                TaskCreationOptions.PreferFairness, this));
            //return action();
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return Enumerable.Empty<Task>();
        }

        protected override void QueueTask(Task task)
        {
            try
            {
                _taskQueue.Add(task);
            }
            catch (OperationCanceledException)
            {
            }
        }

        protected override Boolean TryExecuteTaskInline(Task task, Boolean taskWasPreviouslyQueued)
        {
            if (taskWasPreviouslyQueued)
                return false;

            return _isExecuting && TryExecuteTask(task);
        }

        private static async Task<T> InnerInvokeAsync<T>(Func<Task<T>> action)
        {
            return await action();
        }

        private void RunOnCurrentThread()
        {
            _isExecuting = true;
            Thread.CurrentThread.IsBackground = true;

            try
            {
                foreach (var task in _taskQueue.GetConsumingEnumerable()) TryExecuteTask(task);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private void Start()
        {
            var t = new Thread(RunOnCurrentThread) {Name = _staThreadName};
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        [ThreadStatic]
        private static Boolean _isExecuting;

        private readonly String _staThreadName;

        private readonly BlockingCollection<Task> _taskQueue;
    }
}