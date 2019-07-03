using System;
using System.Collections.Generic;
using System.Text;

namespace Serializer.Core.Remunerators
{
    public class BinaryStack : IBinaryStack
    {
        private readonly IBinaryStack _parent;
        private readonly Stack<IBinaryWriter> _activeNodes;
        private readonly List<IBinaryWriter> _completedNodes;

        public BinaryStack(IBinaryStack parent)
        {
            _parent = parent;
            _activeNodes = new Stack<IBinaryWriter>();
            _completedNodes = new List<IBinaryWriter>();
        }

        public void Push(IBinaryWriter writer) => _activeNodes.Push(writer);

       

        public IBinaryWriter Pop()
        {
            if (_activeNodes.Count == 0)
                return _parent.Pop();

            var completed = _activeNodes.Pop();
            _completedNodes.Add(completed);
            return completed;
        }

        public IBinaryWriter Push(Type type)
        {
            var list = new BinaryListWriter(type, this);
            _activeNodes.Push(list);
            return list;
        }
    }
}
