using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Serializer.Concurrency
{
    public class ExclusiveSynchronizationContext : SynchronizationContext
    {
        public Exception? InnerException { get; set; }

        public void BeginMessageLoop()
        {
            while (!_done)
            {
                Tuple<SendOrPostCallback, Object>? task = null;
                lock (items)
                {
                    if (items.Count > 0) task = items.Dequeue();
                }

                if (task != null)
                {
                    task.Item1(task.Item2);
                    if (InnerException != null)
                        throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                }
                else
                    _workItemsWaiting.WaitOne();
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        public void EndMessageLoop()
        {
            Post(_ => _done = true, null!);
        }

        public override void Post(SendOrPostCallback d,
                                  Object state)
        {
            lock (items)
            {
                items.Enqueue(Tuple.Create(d, state));
            }

            _workItemsWaiting.Set();
        }

        public override void Send(SendOrPostCallback d,
                                  Object state)
        {
            throw new NotSupportedException("We cannot send to our same thread");
        }

        private readonly Queue<Tuple<SendOrPostCallback, Object>> items =
            new();

        private readonly AutoResetEvent _workItemsWaiting = new(false);
        private Boolean _done;
    }
}
