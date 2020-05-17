using System;
using System.Collections.Generic;
using System.Threading;

namespace Das.Serializer
{
    public abstract class NodeProvider<T> : TypeCore,
        IScanNodeProvider<T> where T : INode, IEnumerable<T>
    {
        public INodeTypeProvider TypeProvider { get; }

        protected NodeProvider(INodeTypeProvider typeProvider, ISerializerSettings settings)
            : base(settings)
        {
            TypeProvider = typeProvider;
        }

        private static readonly ThreadLocal<List<T>> LetsAdd
            = new ThreadLocal<List<T>>(() => new List<T>());

        protected static Queue<T> Buffer => _buffer.Value;

        private static readonly ThreadLocal<Queue<T>> _buffer
            = new ThreadLocal<Queue<T>>(() => new Queue<T>());
        

        public void Put(T node)
        {
            if (node == null)
                return;

            var buffer = Buffer;
            var letsAdd = LetsAdd.Value;
            
            letsAdd.AddRange(node);

            node.Clear();
            foreach (var n in letsAdd)
            {
                buffer.Enqueue(n);
                if (buffer.Count > 1000)
                    break;
            }

            letsAdd.Clear();
        }

    }

  
}