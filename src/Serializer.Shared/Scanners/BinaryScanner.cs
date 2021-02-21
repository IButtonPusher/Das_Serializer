using System;
using System.Threading.Tasks;
using Das.Streamers;

namespace Das.Serializer
{
    public class BinaryScanner : SerializerCore, IBinaryScanner
    {
        public BinaryScanner(IBinaryContext state,
                             ISerializerSettings settings,
                             ITypeManipulator typeManipulator,
                             IObjectManipulator objectManipulator,
                             IInstantiator objectInstantiator)
            : base(state, settings)
        {
            //_state = state;
            _typeManipulator = typeManipulator;
            _objectManipulator = objectManipulator;
            _objectInstantiator = objectInstantiator;
            _nodes = state.ScanNodeProvider;
            _nodeManipulator = state.ScanNodeManipulator;
        }


        public virtual T Deserialize<T>(IBinaryFeeder source)
        {
            _feeder = source;
            return Deserialize<T>();
        }

        public void Invalidate()
        {
            var rn = _rootNode;
            if (rn == null)
                return;
            _nodes.Put(rn);
            _rootNode = default;
        }

        private IBinaryFeeder Feeder => _feeder ?? throw new NullReferenceException(nameof(_feeder));

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
                    node.Value = Feeder.GetPrimitive(node.Type!);

                    break;
            }

            _nodes.Sealer.CloseNode(node);
            _nodes.Sealer.Imbue(node);
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
                    var distanceFromRoot = Feeder.GetCircularReferenceIndex();
                    _nodes.ResolveCircularReference(node, ref distanceFromRoot);
                    return;
            }

            var propVals = _typeManipulator.GetPropertiesToSerialize(
                node.Type!, Settings);

            foreach (var prop in propVals)
            {
                var propType = prop.Type;

                if (IsLeaf(propType, true))
                {
                    var val = Feeder.GetPrimitive(propType);

                    _nodes.Sealer.Imbue(node, prop.Name, val!);
                }
                else
                {
                    var child = NewNode(prop.Name, node, propType);
                    BuildNext(ref child);
                }
            }
        }

        private void BuildCollection(ref IBinaryNode node)
        {
            var germane = TypeInferrer.GetGermaneType(node.Type!);
            var feeder = Feeder;

            var index = 0;
            var blockEnd = node.BlockStart + node.BlockSize;

            if (IsLeaf(germane, true))
            {
                while (feeder.Index < blockEnd)
                {
                    var res = feeder.GetPrimitive(germane);
                    _nodes.Sealer.Imbue(node, index.ToString(), res!);
                    index++;
                }

                return;
            }

            while (feeder.Index < blockEnd)
            {
                var child = NewNode(index.ToString(), node, germane);
                BuildNext(ref child);
                index++;
            }
        }

        private void BuildFallbackObject(ref IBinaryNode node)
        {
            var fbType = node.Type;
            var nodeSize = node.BlockSize;
            var val = Feeder.GetFallback(fbType!, ref nodeSize);
            node.Value = val;
            node.BlockSize = nodeSize;
        }

        private T Deserialize<T>()
        {
            var orgType = typeof(T);
            var retType = orgType;

            _rootNode = NewNode(Const.Empty, NullNode.Instance, retType);

            BuildNext(ref _rootNode);

            if (_rootNode.Type != orgType)
                return _objectManipulator.CastDynamic<T>(_rootNode.Value!);

            if (_rootNode.Value != null)
                _objectInstantiator.OnDeserialized(_rootNode, Settings);

            return (T) _rootNode.Value!;
        }

        private IBinaryNode NewNode(String name,
                                    [NotNull] IBinaryNode parent,
                                    Type type)
        {
            var child = _nodes.Get(name, parent, type);
            var feeder = Feeder;

            child.BlockStart = feeder.Index;
            if (child.NodeType == NodeTypes.Primitive &&
                Settings.TypeSpecificity != TypeSpecificity.All)
                return child;

            //reference type - read size prefix
            child.BlockSize = feeder.GetNextBlockSize();
            var isUnwrapped = feeder.GetPrimitive<Boolean>();

            child.BlockSize -= 5;

            if (!isUnwrapped)
                return child;

            var sizeStart = feeder.Index;

            //envelope is providing type explicitly.  Re-calibrate
            child.Type = feeder.GetNextType();
            child.NodeType = NodeTypes.None;

            //substract the type wrapping from the effective size of the block
            child.BlockSize -= feeder.Index - sizeStart;
            //adjust starting point to after the size/type decl the 
            child.BlockStart = feeder.Index;
            _nodeManipulator.EnsureNodeType(child);

            return child;
        }

        private static readonly NullNode NullNode = NullNode.Instance;
        private readonly INodeManipulator _nodeManipulator;
        protected readonly IBinaryNodeProvider _nodes;
        private readonly IInstantiator _objectInstantiator;

        private readonly IObjectManipulator _objectManipulator;

        //protected readonly IBinaryContext _state;
        private readonly ITypeManipulator _typeManipulator;
        private IBinaryFeeder? _feeder;
        private IBinaryNode? _rootNode;
    }
}
