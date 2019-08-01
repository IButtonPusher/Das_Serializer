using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Das.CoreExtensions;
using Das.Serializer;
using Das.Serializer.Objects;
using Serializer;
using Serializer.Core;
using Serializer.Core.Printers;


namespace Das.Printers
{
    internal abstract class PrinterBase<TEFormat> : SerializerCore, ISerializationDepth
    {
        #region fields

        private readonly ISerializationState _stateProvider;
        protected readonly Boolean _isIgnoreCircularDependencies;
        private readonly Boolean _isElideDefaultProperties;

        //to quickly detected if an object exists in this path
        private readonly HashSet<Object> _pathReferences;

        //to print the x/json path by property/index
        private readonly List<String> _pathStack;

        //to map the order to property names
        private readonly List<Object> _pathObjects;

        protected Char PathSeparator;
        protected String PathAttribute;

        protected Boolean IsPrintNullProperties;

        protected TEFormat SequenceSeparator;

        protected Boolean IsTextPrinter;

        #endregion

        #region construction

        public PrinterBase(ISerializationState stateProvider,
            ISerializerSettings settings) : base(stateProvider, settings)
        {
            _stateProvider = stateProvider;
            _pathReferences = new HashSet<object>();
            _pathStack = new List<String>();
            PathSeparator = '.';
            _pathObjects = new List<object>();
            PathAttribute = "$ref";
            IsTextPrinter = true;
            _isIgnoreCircularDependencies = stateProvider.Settings.CircularReferenceBehavior
                                            == CircularReference.NoValidation;

            _isElideDefaultProperties = IsTextPrinter && settings.IsOmitDefaultValues;
        }


        protected PrinterBase(ISerializationState stateProvider)
            : this(stateProvider, stateProvider.Settings)
        {
        }

        #endregion

        #region top level printing

        /// <summary>
        /// For type wrapping has to occur one of the following has to be true
        /// 1. _settings.TypeSpecificity == All
        /// 2. _settings.TypeSpecificity == Discrepancy and val.GetType() != propType
        /// -Xml should print this like an attribute __type="System.Int16" within the 
        /// tag it was going to do anyways
        /// -Json should print it as a wrapper around the object 
        /// { "__type": "System.Int16", "__val": *Object's Json* }
        /// -Binary can just put the type name/length bytes beforehand
        /// </summary>
        public abstract Boolean PrintNode(NamedValueNode node);

        /// <summary>
        /// Every object passes through here.  Once we've gotten here we assume that any 
        /// type-wrapping has been performed already
        /// 1. Determine the right way to print this (as reference object, primitive etc)
        /// 2. Route to proper method
        /// </summary>
        protected Boolean PrintObject(PrintNode node)
        {
            var o = node.Value;
            var nodeType = node.NodeType;

            var checkCircs = IsCheckCircularRefs(nodeType);

            if (checkCircs && TryHandleCircularReference(node))
                return false;

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
                    throw new NotImplementedException();
            }

            var willReturn = node.Value != null;

            if (!checkCircs)
                return willReturn;

            _pathObjects.RemoveAt(_pathObjects.Count - 1);
            _pathReferences.Remove(o);

            return willReturn;
        }

        protected Boolean IsObjectReferenced(Object o) => _pathReferences.Contains(o);

        private Boolean IsCheckCircularRefs(NodeTypes nodeType) =>
            nodeType != NodeTypes.Primitive && !_isIgnoreCircularDependencies;

        /// <summary>
        /// False if it's not a circular reference.  Handles the logic of what to do
        /// if it is
        /// </summary>        
        protected Boolean TryHandleCircularReference(PrintNode node)
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
                    {
                        var nullRef = new PrintNode(node.Name, null, node.Type, node.NodeType);
                        PrintObject(nullRef);
                    }

                    break;
                case CircularReference.ThrowException:
                    var path = _pathStack.ToString(PathSeparator, '[');
                    throw new InvalidOperationException($"Circular reference {PathSeparator}{path}");
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

        protected virtual void PrintCircularDependency(Int32 index)
        {
            //have to assume that we don't have a tag open for xml
            //since attribute properties are only for primitives
            var reference = _pathStack.Take(index + 1).ToString(PathSeparator, '[');
            PrintProperty(new NamedValueNode(PathAttribute, reference, Const.StrType));
        }

        /// <summary>
        /// prints most objects as series of properties
        /// </summary>
        protected virtual void PrintReferenceType(PrintNode node)
        {
            var properyValues = _stateProvider.ObjectManipulator.GetPropertyResults(node, this);
            PrintSeries(properyValues, PrintProperty);
        }

        protected void PushStack(String str) => _pathStack.Add(str);

        protected void PopStack() => _pathStack.RemoveAt(_pathStack.Count - 1);

        protected abstract void PrintCollection(PrintNode node);

        #endregion

        #region print helper methods

        protected Boolean IsWrapNeeded(Type declaredType, Type objectType)
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

                    if (IsUseless(declaredType))
                        return true;

                    var res = _stateProvider.GetNodeType(objectType, Settings.SerializationDepth);

                    if (res == NodeTypes.Fallback)
                        return false;

                    if (IsCollection(declaredType) && IsInstantiable(declaredType))
                    {
                        //a List<IInterface> or IInterface[] does not need to be wrapped
                        //but IEnumerable<Int32> does need to be wrapped
                        //and also if declaredType is a collection but the objectType isn't a collection
                        //then this came from an iterator block which is awkward enough so wrap it up
                        return !IsCollection(objectType);
                    }

                    return true;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual void PrintSeries<T>(IEnumerable<T> values, Func<T, Boolean> meth)
        {
            foreach (var val in values)
                meth(val);
        }

        protected IEnumerable<ObjectNode> ExplodeList(IEnumerable list, Type itemType)
        {
            if (list == null)
                yield break;

            var index = 0;
            foreach (var o in list)
            {
                yield return new ObjectNode(o, itemType, index++);
            }
        }

        protected Boolean PrintProperty(NamedValueNode prop)
        {
            if (prop.Type.IsNestedPrivate && IsCollection(prop.Type))
            {
                //force deferred to run
            }

            if (_isElideDefaultProperties && IsDefaultValue(prop.Value))
                return false;

            var printed = PrintNode(prop);

            return printed;
        }

        #endregion

        #region abstract methods

        /// <summary>
        /// By the time we get here any wrapping should have happened already
        /// </summary>
        protected abstract void PrintPrimitive(PrintNode node);

        protected abstract void PrintFallback(PrintNode node);

        #endregion

        bool ISerializationDepth.IsOmitDefaultValues
        {
            get => Settings.IsOmitDefaultValues;
            set => throw new NotSupportedException();
        }

        SerializationDepth ISerializationDepth.SerializationDepth
        {
            get => Settings.SerializationDepth;
            set => throw new NotSupportedException();
        }
    }
}