using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;

namespace Das.Printers
{
    public abstract class PrinterBase : TypeCore, ISerializationDepth
    {
        protected PrinterBase(ISerializationState stateProvider,
                              ISerializerSettings settings)
        : base(settings)
        {
            //Settings = settings;
            _stateProvider = stateProvider;
            _typeInferrer = stateProvider.TypeInferrer;
            _nodeTypes = stateProvider.ScanNodeProvider.TypeProvider;
            _nodeProvider = stateProvider.ScanNodeProvider;
            _printNodePool = stateProvider.PrintNodePool;
            _pathReferences = new HashSet<Object>();
            _pathStack = new List<String>();
            PathSeparator = '.';
            _pathObjects = new List<Object?>();
            PathAttribute = "$ref";
            IsTextPrinter = true;
            _isIgnoreCircularDependencies = stateProvider.Settings.CircularReferenceBehavior
                                            == CircularReference.NoValidation;

            IsPrintNullProperties |= settings.CircularReferenceBehavior == CircularReference.IgnoreObject;

            _isOmitDefaultProperties = IsTextPrinter && settings.IsOmitDefaultValues;
        }


        protected PrinterBase(ISerializationState stateProvider)
            : this(stateProvider, stateProvider.Settings)
        {
        }

        Boolean ISerializationDepth.IsOmitDefaultValues => Settings.IsOmitDefaultValues;

        SerializationDepth ISerializationDepth.SerializationDepth
            => Settings.SerializationDepth;

        public abstract Boolean IsRespectXmlIgnore { get; }

        //public ISerializerSettings Settings { get; }


        /// <summary>
        ///     For type wrapping has to occur one of the following has to be true
        ///     1. _settings.TypeSpecificity == All
        ///     2. _settings.TypeSpecificity == Discrepancy and val.GetType() != propType
        ///     -Xml should print this like an attribute __type="System.Int16" within the
        ///     tag it was going to do anyways
        ///     -Json should print it as a wrapper around the object
        ///     { "__type": "System.Int16", "__val": *Object's Json* }
        ///     -Binary can just put the type name/length bytes beforehand
        /// </summary>
        public abstract void PrintNode(INamedValue node);

        protected static IEnumerable<ObjectNode> ExplodeList(IEnumerable? list,
                                                             Type itemType)
        {
            if (list == null)
                yield break;

            var index = 0;
            foreach (var o in list)
            {
                yield return new ObjectNode(o, itemType, index++);
            }
        }

        protected Boolean IsObjectReferenced(Object o)
        {
            return _pathReferences.Contains(o);
        }


        protected Boolean IsWrapNeeded(Type declaredType,
                                       Type objectType)
        {
            switch (Settings.TypeSpecificity)
            {
                case TypeSpecificity.All:
                    return true;
                case TypeSpecificity.None:
                    return false;
                case TypeSpecificity.Discrepancy:
                    if (declaredType.IsSealed)
                        return false;

                    if (declaredType == objectType)
                        return false;

                    if (_typeInferrer.IsUseless(declaredType))
                        return true;

                    var res = _nodeTypes.GetNodeType(objectType, Settings.SerializationDepth);

                    if (res == NodeTypes.Fallback)
                        return false;

                    if (_typeInferrer.IsCollection(declaredType) &&
                        _typeInferrer.IsInstantiable(declaredType))
                        //a List<IInterface> or IInterface[] does not need to be wrapped
                        //but IEnumerable<Int32> does need to be wrapped
                        //and also if declaredType is a collection but the objectType isn't a collection
                        //then this came from an iterator block which is awkward enough so wrap it up
                        return !_typeInferrer.IsCollection(objectType);

                    return true;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void PopStack()
        {
            _pathStack.RemoveAt(_pathStack.Count - 1);
        }

        protected virtual void PrintCircularDependency(Int32 index)
        {
            //have to assume that we don't have a tag open for xml
            //since attribute properties are only for primitives
            var reference = _pathStack.Take(index + 1).ToString(PathSeparator, '[');
            using (var node = _printNodePool.GetNamedValue(PathAttribute, reference, Const.StrType))
            {
                PrintProperty(node);
            }
        }

        protected abstract void PrintCollection(IPrintNode node);

        protected abstract void PrintFallback(IPrintNode node);

        /// <summary>
        ///     Every object passes through here.  Once we've gotten here we assume that any
        ///     type-wrapping has been performed already
        ///     1. Determine the right way to print this (as reference object, primitive etc)
        ///     2. Route to proper method
        /// </summary>
        protected Boolean PrintObject(IPrintNode node)
        {
            var o = node.Value;
            var nodeType = node.NodeType;

            var checkCircs = IsCheckCircularRefs(nodeType);

            if (checkCircs && TryHandleCircularReference(node))
                return true;

            switch (nodeType)
            {
                case NodeTypes.Primitive:
                    PrintPrimitive(node);
                    break;
                case NodeTypes.Object:
                case NodeTypes.PropertiesToConstructor:
                    PrintReferenceType(node);
                    break;
                case NodeTypes.Collection:
                    PrintCollection(node);
                    break;
                case NodeTypes.Fallback:
                    PrintFallback(node);
                    break;
                case NodeTypes.Dynamic:
                    //should only be trying to print a dynamic if it's a prop of type object
                    //with a null value. Can't do anything with that
                    if (o != null)
                        PrintFallback(node);
                    else
                        PrintReferenceType(node);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var willReturn = node.Value != null;

            if (!checkCircs)
                return willReturn;

            _pathObjects.RemoveAt(_pathObjects.Count - 1);

            if (o != null)
                _pathReferences.Remove(o);

            return willReturn;
        }


        /// <summary>
        ///     By the time we get here any wrapping should have happened already
        /// </summary>
        protected abstract void PrintPrimitive(IPrintNode node);

        protected virtual void PrintProperties<T>(IPropertyValueIterator<T> values,
                                                  Action<T> exe) where T : class, INamedValue
        {
            for (var c = 0; c < values.Count; c++)
            {
                var current = values[c];
                exe(current);
            }
        }


        protected void PrintProperty(INamedValue prop)
        {
            if (prop.Type!.IsNestedPrivate && _typeInferrer.IsCollection(prop.Type))
            {
                //force deferred to run
            }

            PrintNode(prop);

            prop.Dispose();

            //return printed;
        }

        /// <summary>
        ///     prints most objects as series of properties
        /// </summary>
        protected virtual void PrintReferenceType(IPrintNode node)
        {
            var properyValues = _stateProvider.ObjectManipulator.GetPropertyResults(node, this);
            PrintProperties<INamedValue>(properyValues, PrintProperty);
        }

        protected virtual void PrintSeries<T>(IEnumerable<T> values,
                                              Action<T> print)
        {
            foreach (var val in values)
            {
                print(val);
            }
        }

        protected void PushStack(String str)
        {
            _pathStack.Add(str);
        }

        protected Boolean ShouldPrintNode(INamedValue prop)
        {
            return ShouldPrintValue(prop.Value);
        }

        protected Boolean ShouldPrintValue<T>(T item)
        {
            return !ReferenceEquals(null, item) &&
                   (!_isOmitDefaultProperties ||
                    !_typeInferrer.IsDefaultValue(item));
        }

        private Boolean IsCheckCircularRefs(NodeTypes nodeType)
        {
            return nodeType != NodeTypes.Primitive && !_isIgnoreCircularDependencies;
        }

        /// <summary>
        ///     False if it's not a circular reference.  Handles the logic of what to do
        ///     if it is
        /// </summary>
        private Boolean TryHandleCircularReference(IPrintNode node)
        {
            var o = node.Value;

            if (o == null || _pathReferences.Add(o))
            {
                _pathObjects.Add(o);
                return false;
            }

            //uh oh... circular reference detected
            switch (Settings.CircularReferenceBehavior)
            {
                case CircularReference.IgnoreObject:
                    if (IsPrintNullProperties)
                        using (var nullRef = _printNodePool.GetPrintNode(node, null))
                        {
                            PrintObject(nullRef);
                        }

                    break;
                case CircularReference.ThrowException:
                    throw new CircularReferenceException(_pathStack, PathSeparator);
                    //var path = _pathStack.ToString(PathSeparator, '[');
                    //throw new InvalidOperationException($"Circular reference {PathSeparator}{path}");
                case CircularReference.SerializePath:
                    var objIndex = _pathObjects.IndexOf(o);
                    PrintCircularDependency(objIndex);
                    break;
                case CircularReference.NoValidation:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        protected readonly Boolean _isIgnoreCircularDependencies;
        private readonly Boolean _isOmitDefaultProperties;
        protected readonly INodeTypeProvider _nodeTypes;

        //to map the order to property names
        private readonly List<Object?> _pathObjects;

        //to quickly detected if an object exists in this path
        private readonly HashSet<Object> _pathReferences;

        //to print the x/json path by property/index
        private readonly List<String> _pathStack;
        protected readonly INodePool _printNodePool;


        private readonly ISerializationState _stateProvider;
        protected readonly ITypeInferrer _typeInferrer;
        protected IScanNodeProvider _nodeProvider;

        protected Boolean IsPrintNullProperties;

        protected Boolean IsTextPrinter;
        protected String PathAttribute;

        protected Char PathSeparator;
    }
}
