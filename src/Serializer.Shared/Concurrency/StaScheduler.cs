using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if !NET40
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace Das.Serializer.Concurrency
{
    public class StaScheduler : TaskScheduler
    {
        public StaScheduler(String staThreadName)
        {
            _staThreadName = staThreadName;
            _lockStartQuit = new Object();
            _taskQueue = new BlockingCollection<Task>();
            
            _cancellationSource = new CancellationTokenSource();

            _swSinceLastRun = Stopwatch.StartNew();
            Start();
        }

        public void Invoke(Action action)
        {
            InvocationBase.RunSync(() => Task.Factory.StartNew(action, CancellationToken.None,
                TaskCreationOptions.PreferFairness, this));
        }

        public T Invoke<T>(Func<T> action)
        {
            return InvocationBase.RunSync(() => Task.Factory.StartNew(action, CancellationToken.None,
                TaskCreationOptions.PreferFairness, this));
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return Enumerable.Empty<Task>();
        }

        protected override void QueueTask(Task task)
        {
            try
            {
                var needsStart = false;

                lock (_lockStartQuit)
                {
                    if (_cancellationSource.IsCancellationRequested)
                    {
                        needsStart = true;
                    }

                    _swSinceLastRun.Restart();
                }

                if (needsStart)
                    Start();

                _taskQueue.Add(task);
            }
            catch (OperationCanceledException)
            {
            }
        }

        protected override Boolean TryExecuteTaskInline(Task task,
                                                        Boolean taskWasPreviouslyQueued)
        {
            //if (taskWasPreviouslyQueued)
                return false;

            //return _isExecuting && TryExecuteTask(task);
        }


        private void RunOnCurrentThread()
        {
            Thread.CurrentThread.IsBackground = true;

            try
            {
                foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationSource.Token))
                {
                    try
                    {
                        Interlocked.Add(ref _running, 1);
                        TryExecuteTask(task);
                    }
                    finally
                    {
                        Interlocked.Add(ref _running, -1);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
           
        }

        private void Start()
        {
            lock (_lockStartQuit)
            {
                Interlocked.Exchange(ref _cancellationSource, new CancellationTokenSource());
                _swSinceLastRun.Restart();
            }

            var t = new Thread(RunOnCurrentThread) {Name = _staThreadName};

            

            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            PollForInactivityAsync().ConfigureAwait(false);
        }

        private async Task PollForInactivityAsync()
        {
            while (true)
            {
                await TaskEx.Delay(5000);

                if (Interlocked.Read(ref _running) > 0)
                    continue;


                lock (_lockStartQuit)
                {
                    if (_swSinceLastRun.ElapsedMilliseconds < 10000)
                        continue;

                    _cancellationSource.Cancel(false);
                    return;
                }


            }
        }


        private readonly Stopwatch _swSinceLastRun;

        private readonly String _staThreadName;
        private Int64 _running;

        private readonly BlockingCollection<Task> _taskQueue;
        private CancellationTokenSource _cancellationSource;
        private readonly Object _lockStartQuit;
    }
}
