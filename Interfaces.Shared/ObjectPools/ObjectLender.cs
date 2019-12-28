using System;
using System.Collections.Generic;
using System.Threading;

namespace Das.Serializer
{
    public abstract class ObjectLender<TObject, TCtorParam> : IDataLender<TObject,TCtorParam>
        where TObject : ILendable<TObject,TCtorParam>
    {
        private static readonly ThreadLocal<Queue<TObject>> _buffer
            = new ThreadLocal<Queue<TObject>>(() => new Queue<TObject>());

        protected static Queue<TObject> Buffer => _buffer.Value;

        public TObject Get(TCtorParam input)
        {
            var buffer = Buffer;
            var item = buffer.Count > 0
                ? buffer.Dequeue()
                : GetNew(input);

            item.ReturnToSender = Put;
            
            return item;
        }

        protected abstract Func<TCtorParam, TObject> GetNew { get; }

        public virtual void Put(TObject node)
        {
            if (node == null)
                return;

            var buffer = Buffer;
            
            buffer.Enqueue(node);
        }
    }
}
