using System;
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

        protected static Queue<T> Buffer => _buffer.Value;

        private static readonly ThreadLocal<Queue<T>> _buffer
            = new ThreadLocal<Queue<T>>(() => new Queue<T>());


        public INodeManipulator TypeProvider { get; }

        public void Put(T node)
        {
            if (node == null)
                return;

            //if (IsAnonymousType(node.Type))
            //   _dynamicFacade.DynamicTypes.InvalidateDynamicTypes();

            var buffer = Buffer;
            node.Clear();
            buffer.Enqueue(node);
        }
    }
}