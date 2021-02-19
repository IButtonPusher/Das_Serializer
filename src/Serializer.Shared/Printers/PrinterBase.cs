using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;

namespace Das.Printers
{
    public abstract class PrinterBase : TypeCore, ISerializationDepth
    {
        protected PrinterBase(ISerializerSettings settings,
                              ITypeInferrer typeInferrer,
                              INodeTypeProvider nodeTypes,
                              IObjectManipulator objectManipulator)
            : base(settings)
        {
            _typeInferrer = typeInferrer;
            _nodeTypes = nodeTypes;
            _objectManipulator = objectManipulator;

            _pathReferences = new HashSet<Object>();
            _pathStack = new List<String>();
            PathSeparator = '.';
            _pathObjects = new List<Object?>();
            PathAttribute = "$ref";
            IsTextPrinter = true;
            _isIgnoreCircularDependencies = settings.CircularReferenceBehavior
                                            == CircularReference.NoValidation;

            IsPrintNullProperties |= settings.CircularReferenceBehavior == CircularReference.IgnoreObject;

            _isOmitDefaultProperties = IsTextPrinter && settings.IsOmitDefaultValues;
        }


        Boolean ISerializationDepth.IsOmitDefaultValues => Settings.IsOmitDefaultValues;

        SerializationDepth ISerializationDepth.SerializationDepth
            => Settings.SerializationDepth;

        public abstract Boolean IsRespectXmlIgnore { get; }

        public abstract void PrintNode(String nodeName,
                                       Type? propType,
                                       Object? value);

        protected static IEnumerable<KeyValuePair<Object?, Type>> ExplodeIterator(IEnumerable? list,
            Type itemType)
        {
            if (list == null)
                yield break;

            foreach (var o in list)
            {
                yield return new KeyValuePair<object?, Type>(o, itemType);
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

                    var res = _nodeTypes.GetNodeType(objectType);

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
            PrintProperty(reference, PathAttribute, Const.StrType);
        }

        protected abstract void PrintCollection(Object? value,
                                                Type valType,
                                                Boolean knownEmpty);


        protected virtual void PrintCollectionObject(Object? o,
                                                     Type propType,
                                                     Int32 index)
        {
            PrintNode(index.ToString(), propType, o);
        }

        protected abstract void PrintFallback(Object? o,
                                              Type propType);

        protected Boolean PrintObject(Object? o,
                                      Type propType,
                                      NodeTypes nodeType)
        {
            var checkCircs = IsCheckCircularRefs(nodeType);

            if (checkCircs && TryHandleCircularReference(o, propType, nodeType))
                return true;

            switch (nodeType)
            {
                case NodeTypes.Primitive:
                    PrintPrimitive(o, propType);
                    break;
                case NodeTypes.Object:
                case NodeTypes.PropertiesToConstructor:
                    PrintReferenceType(o, propType);
                    break;
                case NodeTypes.Collection:
                    var isEmpty = o is ICollection {Count: 0};

                    PrintCollection(o, propType, isEmpty);
                    break;
                case NodeTypes.Fallback:
                    PrintFallback(o, propType);
                    break;
                case NodeTypes.Dynamic:
                    //should only be trying to print a dynamic if it's a prop of type object
                    //with a null value. Can't do anything with that
                    if (o != null)
                        PrintFallback(o, propType);
                    else
                        PrintReferenceType(o, propType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var willReturn = o != null;

            if (!checkCircs)
                return willReturn;

            _pathObjects.RemoveAt(_pathObjects.Count - 1);

            if (o != null)
                _pathReferences.Remove(o);

            return willReturn;
        }

        protected abstract void PrintPrimitive(Object? o,
                                               Type propType);

        protected virtual void PrintProperties(IEnumerable<KeyValuePair<PropertyInfo, Object?>> values,
                                               Action<PropertyInfo, Object?> exe)
        {
            foreach (var val in values)
            {
                exe(val.Key, val.Value);
            }
        }


        protected void PrintProperty(PropertyInfo prop,
                                     Object? propValue)
        {
            PrintProperty(propValue, prop.Name, prop.PropertyType);
        }

        protected virtual void PrintProperty(Object? propValue,
                                             String name,
                                             Type propertyType)
        {
            if (propertyType!.IsNestedPrivate && _typeInferrer.IsCollection(propertyType))
            {
                //force deferred to run
            }

            PrintNode(name, propertyType, propValue);
        }

        protected virtual void PrintReferenceType(Object? value,
                                                  Type valType)
        {
            var properyValues = _objectManipulator.GetPropertyResults(value, valType, this);
            PrintProperties(properyValues, PrintProperty);
        }

        protected virtual void PrintSeries(IEnumerable<KeyValuePair<Object?, Type>> values,
                                           Action<Object?, Type, Int32> print)
        {
            var idx = 0;
            foreach (var val in values)
            {
                print(val.Key, val.Value, idx++);
            }
        }

        protected void PushStack(String str)
        {
            _pathStack.Add(str);
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

        private Boolean TryHandleCircularReference(Object? o,
                                                   Type propType,
                                                   NodeTypes nodeType)
        {
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
                        PrintObject(null, propType, nodeType);


                    break;
                case CircularReference.ThrowException:
                    throw new CircularReferenceException(_pathStack, PathSeparator);

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
        private readonly IObjectManipulator _objectManipulator;

        //to map the order to property names
        private readonly List<Object?> _pathObjects;

        //to quickly detected if an object exists in this path
        private readonly HashSet<Object> _pathReferences;

        //to print the x/json path by property/index
        private readonly List<String> _pathStack;
        protected readonly ITypeInferrer _typeInferrer;

        protected Boolean IsPrintNullProperties;

        protected Boolean IsTextPrinter;
        protected String PathAttribute;

        protected Char PathSeparator;
    }
}
