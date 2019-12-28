using Das.Streamers;
using System;
using Serializer.Core;

namespace Das.Serializer.Scanners
{
    internal class BinaryScanner : SerializerCore, IBinaryScanner
    {
        public BinaryScanner(IBinaryContext state) : base(state, state.Settings)
        {
            _state = state;
            _nodes = state.ScanNodeProvider;
            _nodeManipulator = state.ScanNodeManipulator;
        }

        private IBinaryFeeder _feeder;
        private IBinaryNode _rootNode;
        protected readonly IBinaryContext _state;
        protected readonly IBinaryNodeProvider _nodes;

        private static readonly NullNode NullNode = NullNode.Instance;
        private readonly INodeManipulator _nodeManipulator;

       

        public virtual T Deserialize<T>(IBinaryFeeder source)
        {
            _feeder = source;
            return Deserialize<T>();
        }

        private T Deserialize<T>()
        {
            var orgType = typeof(T);
            var retType = orgType;

            _rootNode = NewNode(Const.Empty, NullNode.Instance, retType);

            BuildNext(ref _rootNode);

            if (_rootNode.Type != orgType)
                return ObjectManipulator.CastDynamic<T>(_rootNode.Value);

            if (_rootNode.Value != null)
                _state.ObjectInstantiator.OnDeserialized(_rootNode, Settings);

            return (T) _rootNode.Value;
        }


        protected virtual void BuildReferenceObject(ref IBinaryNode node)
        {
            switch (node.BlockSize)
            {
                case 0:
                    if (NullNode != node.Parent)
                        node.IsForceNullValue = true;
                    return;
                case 1:
                    //a reference object with a size of 1 byte is
                    //a circular reference pointer
                    var distanceFromRoot = _feeder.GetCircularReferenceIndex();
                    _nodes.ResolveCircularReference(node, ref distanceFromRoot);
                    return;
            }

            var propVals = _state.TypeManipulator.GetPropertiesToSerialize(
                node.Type, Settings);

            foreach (var prop in propVals)
            {
                var propType = prop.Type;

                if (IsLeaf(propType, true))
                {
                    var val = _feeder.GetPrimitive(propType);

                    _nodes.Sealer.Imbue(node, prop.Name, val);
                }
                else
                {
                    var child = NewNode(prop.Name, node, propType);
                    BuildNext(ref child);
                }
            }
        }

        private void BuildFallbackObject(ref IBinaryNode node)
        {
            var fbType = node.Type;
            var nodeSize = node.BlockSize;
            var val = _feeder.GetFallback(fbType, ref nodeSize);
            node.Value = val;
            node.BlockSize = nodeSize;
        }

        private void BuildCollection(ref IBinaryNode node)
        {
            var germane = TypeInferrer.GetGermaneType(node.Type);

            var index = 0;
            var blockEnd = node.BlockStart + node.BlockSize;

            if (IsLeaf(germane, true))
            {
                while (_feeder.Index < blockEnd)
                {
                    var res = _feeder.GetPrimitive(germane);
                    _nodes.Sealer.Imbue(node, index.ToString(), res);
                    index++;
                }

                return;
            }

            while (_feeder.Index < blockEnd)
            {
                var child = NewNode(index.ToString(), node, germane);
                BuildNext(ref child);
                index++;
            }
        }

        private IBinaryNode NewNode(String name, [NotNull]IBinaryNode parent, Type type)
        {
            var child = _nodes.Get(name, parent, type);
            child.BlockStart = _feeder.Index;
            if (child.NodeType == NodeTypes.Primitive &&
                Settings.TypeSpecificity != TypeSpecificity.All)
                return child;

            //reference type - read size prefix
            child.BlockSize = _feeder.GetNextBlockSize();
            var isUnwrapped = _feeder.GetPrimitive<Boolean>();

            child.BlockSize -= 5;

            if (!isUnwrapped)
                return child;

            var sizeStart = _feeder.Index;

            //envelope is providing type explicitly.  Re-calibrate
            child.Type = _feeder.GetNextType();
            child.NodeType = NodeTypes.None;

            //substract the type wrapping from the effective size of the block
            child.BlockSize -= _feeder.Index - sizeStart;
            //adjust starting point to after the size/type decl the 
            child.BlockStart = _feeder.Index;
            _nodeManipulator.EnsureNodeType(child);

            return child;
        }

        protected void BuildNext(ref IBinaryNode node)
        {
            switch (node.NodeType)
            {
                case NodeTypes.Object:
                case NodeTypes.PropertiesToConstructor:
                    BuildReferenceObject(ref node);
                    break;

                case NodeTypes.Fallback:
                    BuildFallbackObject(ref node);
                    break;
                case NodeTypes.Collection:
                    BuildCollection(ref node);
                    break;

                case NodeTypes.Dynamic:
                    //should unwrap then recurse back here with a specific NodeType
                    BuildReferenceObject(ref node);
                    return;

                case NodeTypes.Primitive:
                    node.Value = _feeder.GetPrimitive(node.Type);

                    break;
            }

            _nodes.Sealer.CloseNode(node);
            _nodes.Sealer.Imbue(node);
        }

        public void Invalidate()
        {
            _nodes.Put(_rootNode);
            _rootNode = default;
        }
    }
}