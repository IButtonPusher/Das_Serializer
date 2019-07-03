using Das.Streamers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Das.Serializer;
using Serializer.Core;
using Serializer.Core.Binary;

namespace Das.Scanners
{
    internal class BinaryScanner : SerializerCore, IBinaryScanner
    {
        #region construction

        public BinaryScanner(IBinaryContext state) : base(state, state.Settings)
        {
            _logger = new BinaryLogger();
            _state = state;
            _nodes = state.NodeProvider;
        }

        #endregion

        private IBinaryFeeder _feeder;
        private IBinaryNode _rootNode;
        private readonly IBinaryContext _state;
        private readonly IBinaryNodeProvider _nodes;
        private BinaryLogger _logger;

        public TOutput Deserialize<TOutput>(IByteArray source)
        {
            _feeder = new BinaryFeeder(_state.PrimitiveScanner, _state, source, Settings, _logger);
            return Deserialize<TOutput>();
        }

        public T Deserialize<T>(IEnumerable<byte[]> source)
        {
            _feeder = new BinaryFeeder(_state.PrimitiveScanner, _state, source, Settings, _logger);
            return Deserialize<T>();
        }

        private T Deserialize<T>()
        {
            var orgType = typeof(T);
            var retType = orgType;

            _rootNode = NewNode(null, null, retType);                     

            BuildNext(ref _rootNode);

            if (_rootNode.Type != orgType)
                return CastDynamic<T>(_rootNode.Value);

            if (_rootNode?.Value != null)
                _state.ObjectInstantiator.OnDeserialized(_rootNode.Value,
                    Settings.SerializationDepth);

            return (T)_rootNode.Value;
        }


        private void BuildReferenceObject(ref IBinaryNode node)
        {
            switch (node.BlockSize)
            {
                case 0:
                    if (node.Parent != null)
                        node.IsForceNullValue = true;
                    return;
                case 1:
                    //a reference object with a size of 1 byte is a circular dependency pointer
                    //a circular reference pointer
                    var distanceFromRoot = _feeder.GetCircularReferenceIndex();
                    _nodes.ResolveCircularReference(node, ref distanceFromRoot);
                    return;
            }          

            var propVals = _state.TypeManipulator.GetPropertiesToSerialize(
                node.Type, Settings.SerializationDepth);

            foreach (var prop in propVals)
            {
                Debug("*PROP* [" + prop.Name + "] " + prop.MemberType + 
                    " scanning " + _feeder.Index);

                var propType = InstanceMemberType(prop);

                if (IsLeaf(propType, true))
                {
                    var val = _feeder.GetPrimitive(propType);
                    Debug("@PRIMITIVE val is " + val + " for [" + prop.Name + "]");

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
            var germane = GetGermaneType(node.Type);
            
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

        private IBinaryNode NewNode(String name, IBinaryNode parent, Type type)
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
            child.BlockSize -= (_feeder.Index - sizeStart);
            //adjust starting point to after the size/type decl the 
            child.BlockStart = _feeder.Index;
            _nodes.TypeProvider.EnsureNodeType(child);

            return child;
        }

        private void BuildNext(ref IBinaryNode node)
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
                    _logger.Debug("Extracted primitive value " + node.Value);

                    break;
            }

            _nodes.Sealer.CloseNode(node);
            _nodes.Sealer.Imbue(node);
        }

        [Conditional("DEBUG")]
        public void Debug(String val)
        {
            _logger = _logger ?? (_logger = new BinaryLogger());
            _logger.Debug(val);
        }

        public void Invalidate()
        {
            _nodes.Put(_rootNode);
            _rootNode = default;
        }
    }
}
