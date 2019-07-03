using System;
using System.Collections.Generic;
using System.Threading;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class NodeProvider<T> : TypeCore, 
        INodeProvider<T> where T : INode, IEnumerable<T>
    {
        private readonly IDynamicFacade _dynamicFacade;

        protected NodeProvider(IDynamicFacade dynamicFacade, INodeManipulator typeProvider,
            ISerializerSettings settings)
            : base(settings)
        {
            _dynamicFacade = dynamicFacade;
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

            if (IsAnonymousType(node.Type))
                _dynamicFacade.DynamicTypes.InvalidateDynamicTypes();

            var buffer = Buffer;
            node.Clear();
            buffer.Enqueue(node);
        }

        
    }
}
