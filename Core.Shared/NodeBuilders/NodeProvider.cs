using System.Collections.Generic;
using System.Threading;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class NodeProvider<T> : TypeCore,
        INodeProvider<T> where T : INode, IEnumerable<T>
    {
        protected NodeProvider(INodeManipulator typeProvider, ISerializerSettings settings)
            : base(settings)
        {
            TypeProvider = typeProvider;
        }

        private static readonly ThreadLocal<List<T>> LetsAdd
            = new ThreadLocal<List<T>>(() => new List<T>());

        protected static Queue<T> Buffer => _buffer.Value;

        private static readonly ThreadLocal<Queue<T>> _buffer
            = new ThreadLocal<Queue<T>>(() => new Queue<T>());


        public INodeManipulator TypeProvider { get; }

        public void Put(T node)
        {
            if (node == null)
                return;

            var buffer = Buffer;
            var letsAdd = LetsAdd.Value;
            
            letsAdd.AddRange(node);

            node.Clear();
            foreach (var n in letsAdd)
                buffer.Enqueue(n);

            letsAdd.Clear();
        }
    }
}