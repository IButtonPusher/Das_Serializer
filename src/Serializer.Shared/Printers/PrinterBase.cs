using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;
using Das.Serializer.Remunerators;

namespace Das.Printers
{
    public abstract class PrinterBase<TMany, TFew, TWriter> : IObjectPrinter<TMany, TFew, TWriter> //: //TypeCore, 
                                        //ISerializationDepth
        where TMany : IEnumerable<TFew>
        where TWriter : IRemunerable<TMany, TFew>
    {
        protected PrinterBase(//ISerializerSettings settings,
                              ITypeInferrer typeInferrer,
                              INodeTypeProvider nodeTypes,
                              IObjectManipulator objectManipulator,
                              Boolean isPrintNullProperties,
                              Char pathSeparator)
            //: base(settings)
        {
            //_settings = settings;
            _typeInferrer = typeInferrer;
            _nodeTypes = nodeTypes;
            _objectManipulator = objectManipulator;

            PathSeparator = pathSeparator;

            //_pathReferences = new HashSet<Object>();
            //_pathStack = new List<String>();
            //PathSeparator = '.';
            //_pathObjects = new List<Object?>();
            PathAttribute = "$ref";
            IsTextPrinter = true;
            //_isIgnoreCircularDependencies = settings.CircularReferenceBehavior
            //                                == CircularReference.NoValidation;

            IsPrintNullProperties = isPrintNullProperties;
            //IsPrintNullProperties |= settings.CircularReferenceBehavior == CircularReference.IgnoreObject;

            //_isOmitDefaultProperties = IsTextPrinter && settings.IsOmitDefaultValues;
        }


        //Boolean ISerializationDepth.IsOmitDefaultValues => _settings.IsOmitDefaultValues;

        //SerializationDepth ISerializationDepth.SerializationDepth
        //    => _settings.SerializationDepth;

        public abstract Boolean IsRespectXmlIgnore { get; }

        //public abstract void PrintNamedObject(String nodeName,
        //                                      Type? propType,
        //                                      Object? value,
        //                                      NodeTypes valueNodeType,
        //                                      ISerializerSettings settings,
        //                                      ICircularReferenceHandler circularReferenceHandler);


        [MethodImpl(256)]
        protected NodeTypes GetNodeTypeFromValueOrType(Object? value,
                                                       Type valuesType)
        {
            return _nodeTypes.GetNodeType(value?.GetType() ?? valuesType);
        }


        public abstract void PrintNamedObject(String nodeName,
                                              Type? propType,
                                              Object? nodeValue,
                                              NodeTypes nodeType,
                                              TWriter writer,
                                              ISerializerSettings settings,
                                              ICircularReferenceHandler circularReferenceHandler);

        public bool PrintObject(Object? o,
                                Type propType,
                                NodeTypes nodeType,
                                TWriter writer,
                                ISerializerSettings settings,
                                ICircularReferenceHandler circularReferenceHandler)
        {
            if (circularReferenceHandler.TryHandleCircularReference
                    <PrinterBase<TMany, TFew, TWriter>, TMany, TFew, TWriter>
                    (o, propType, nodeType,
                        settings, this, writer))
                //IsPrintNullProperties, PrintObject, PathSeparator))
                return true;

            //var checkCircs = IsCheckCircularRefs(nodeType);

            //if (checkCircs && TryHandleCircularReference(o, propType, nodeType, settings))
            //    return true;

            switch (nodeType)
            {
                case NodeTypes.Primitive:
                    PrintPrimitive(o, writer, propType);
                    break;

                case NodeTypes.Object:
                case NodeTypes.PropertiesToConstructor:
                    PrintReferenceType(o, propType, writer, settings, circularReferenceHandler);
                    break;

                case NodeTypes.Collection:
                    PrintCollection(o, propType, writer, settings, circularReferenceHandler);
                    break;

                case NodeTypes.Fallback:
                    PrintFallback(o, writer, propType);
                    break;

                case NodeTypes.Dynamic:
                    //should only be trying to print a dynamic if it's a prop of type object
                    //with a null value. Can't do anything with that
                    if (o != null)
                        PrintFallback(o, writer, propType);
                    else
                        PrintReferenceType(o, propType, writer, settings, circularReferenceHandler);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            var willReturn = o != null;

            //if (!checkCircs)
            //    return willReturn;

            circularReferenceHandler.PopPathObject();
            //_pathObjects.RemoveAt(_pathObjects.Count - 1);
            
            circularReferenceHandler.RemovePathReference(o);
            //if (o != null)
            //    _pathReferences.Remove(o);


            return willReturn;
        }


        //public Boolean PrintObject(Object? o,
        //                           Type propType,
        //                           NodeTypes nodeType,
        //                           ISerializerSettings settings,
        //                           ICircularReferenceHandler circularReferenceHandler)
        //{
        //    if (circularReferenceHandler.TryHandleCircularReference(o, propType, nodeType, 
        //            settings, this))
        //        //IsPrintNullProperties, PrintObject, PathSeparator))
        //        return true;

        //    //var checkCircs = IsCheckCircularRefs(nodeType);

        //    //if (checkCircs && TryHandleCircularReference(o, propType, nodeType, settings))
        //    //    return true;

        //    switch (nodeType)
        //    {
        //        case NodeTypes.Primitive:
        //            PrintPrimitive(o, propType);
        //            break;

        //        case NodeTypes.Object:
        //        case NodeTypes.PropertiesToConstructor:
        //            PrintReferenceType(o, propType, settings,  circularReferenceHandler);
        //            break;

        //        case NodeTypes.Collection:
        //            PrintCollection(o, propType, settings, circularReferenceHandler);
        //            break;

        //        case NodeTypes.Fallback:
        //            PrintFallback(o, propType);
        //            break;

        //        case NodeTypes.Dynamic:
        //            //should only be trying to print a dynamic if it's a prop of type object
        //            //with a null value. Can't do anything with that
        //            if (o != null)
        //                PrintFallback(o, propType);
        //            else
        //                PrintReferenceType(o, propType, settings, circularReferenceHandler);
        //            break;

        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }

        //    var willReturn = o != null;

        //    circularReferenceHandler.PopReferenceFromStack();
        //    circularReferenceHandler.RemoveObjectFromStack(o);

        //    //if (!checkCircs)
        //    //    return willReturn;

        //    //_pathObjects.RemoveAt(_pathObjects.Count - 1);

        //    //if (o != null)
        //    //    _pathReferences.Remove(o);

        //    return willReturn;
        //}

        public virtual void PrintReferenceType(Object? value,
                                       Type valType,
                                       TWriter writer,
                                       ISerializerSettings settings,
                                       ICircularReferenceHandler circularReferenceHandler)
        {
            var properyValues = _objectManipulator.GetPropertyResults(value, valType, settings);
            PrintProperties(properyValues, writer, PrintProperty, settings, circularReferenceHandler);
        }

        //public virtual void PrintReferenceType(Object? value,
        //                                       Type valType,
        //                                       ISerializerSettings settings,
        //                                       ICircularReferenceHandler circularReferenceHandler)
        //{
        //    var properyValues = _objectManipulator.GetPropertyResults(value, valType, settings);
        //    PrintProperties(properyValues, PrintProperty, settings, circularReferenceHandler);
        //}

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

        //protected Boolean IsObjectReferenced(Object o)
        //{
        //    return _pathReferences.Contains(o);
        //}


        protected Boolean IsWrapNeeded(Type declaredType,
                                       Type objectType,
                                       NodeTypes objectNodeType,
                                       ISerializerSettings settings)
        {
            switch (settings.TypeSpecificity)
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

                    if (objectNodeType == NodeTypes.Fallback)
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

        //protected void PopStack()
        //{
        //    _pathStack.RemoveAt(_pathStack.Count - 1);
        //}

        public virtual void PrintCircularDependency(Int32 index,
                                                    TWriter writer,
                                            ISerializerSettings settings,
                                            IEnumerable<String> pathStack,
                                            ICircularReferenceHandler circularReferenceHandler)
        {
            var reference = pathStack.Take(index + 1).ToString(PathSeparator, '[');
            PrintProperty(reference, PathAttribute, Const.StrType, writer, NodeTypes.Primitive, 
                settings, circularReferenceHandler);
        }

        //protected virtual void PrintCircularDependency(Int32 index,
        //                                               ISerializerSettings settings)
        //{
        //    //have to assume that we don't have a tag open for xml
        //    //since attribute properties are only for primitives
        //    var reference = _pathStack.Take(index + 1).ToString(PathSeparator, '[');
        //    PrintProperty(reference, PathAttribute, Const.StrType, NodeTypes.Primitive, settings);
        //}

        protected abstract void PrintCollection(Object? value,
                                                Type valType,
                                                TWriter writer,
                                                ISerializerSettings settings,
                                                ICircularReferenceHandler circularReferenceHandler);
        //Boolean knownEmpty);


        protected virtual void PrintCollectionObject(Object? o,
                                                     Type propType,
                                                     Int32 index,
                                                     TWriter writer,
                                                     NodeTypes germaneNodeType,
                                                     ISerializerSettings settings,
                                                     ICircularReferenceHandler circularReferenceHandler)
        {
            PrintNamedObject(index.ToString(), propType, o, germaneNodeType, 
                writer, settings, circularReferenceHandler);
        }

        protected abstract void PrintFallback(Object? o,
                                              TWriter Writer,
                                              Type propType);

        protected abstract void PrintPrimitive(Object? o,
                                               TWriter writer,
                                               Type propType);

        protected virtual void PrintProperties(IEnumerable<KeyValuePair<PropertyInfo, Object?>> values,
                                               TWriter writer,
                                               Action<PropertyInfo, Object?, TWriter, ISerializerSettings,
                                                   ICircularReferenceHandler> exe,
                                               ISerializerSettings settings,
                                               ICircularReferenceHandler circularReferenceHandler)
        {
            foreach (var val in values)
            {
                exe(val.Key, val.Value, writer, settings, circularReferenceHandler);
            }
        }


        protected void PrintProperty(PropertyInfo prop,
                                     Object? propValue,
                                     TWriter writer,
                                     ISerializerSettings settings,
                                     ICircularReferenceHandler circularReferenceHandler)
        {
            PrintProperty(propValue, prop.Name, prop.PropertyType, writer,
                GetNodeTypeFromValueOrType(propValue, prop.PropertyType), 
                settings, circularReferenceHandler);
        }

        protected virtual void PrintProperty(Object? propValue,
                                             String name,
                                             Type propertyType,
                                             TWriter writer,
                                             NodeTypes propertyNodeType,
                                             ISerializerSettings settings,
                                             ICircularReferenceHandler circularReferenceHandler)
        {
            //if (propertyNodeType == NodeTypes.Collection && !_typeInferrer.IsCollection(propertyType) ||
            //    (propertyNodeType != NodeTypes.Collection && _typeInferrer.IsCollection(propertyType)))
            //{}

            if (propertyNodeType == NodeTypes.Collection &&  propertyType.IsNestedPrivate)
                                                         //&& _typeInferrer.IsCollection(propertyType))
            {
                //force deferred to run
            }

            PrintNamedObject(name, propertyType, propValue, propertyNodeType, 
                writer, settings, circularReferenceHandler);
        }

        protected virtual void PrintSeries(IEnumerable<KeyValuePair<Object?, Type>> values,
                                           TWriter writer,
                                           Action<Object?, Type, Int32, TWriter, NodeTypes,
                                               ISerializerSettings, ICircularReferenceHandler> print,
                                           NodeTypes nodeTypeForValues,
                                           ISerializerSettings settings,
                                           ICircularReferenceHandler circularReferenceHandler)
        {
            var idx = 0;
            foreach (var val in values)
                print(val.Key, val.Value, idx++, writer, nodeTypeForValues, settings, circularReferenceHandler);
        }

        //protected void PushStack(String str)
        //{
        //    _pathStack.Add(str);
        //}

        //protected abstract Boolean ShouldPrintValue<T>(T item);

        //protected Boolean ShouldPrintValue<T>(T item)
        //{
        //    return !ReferenceEquals(null, item) &&
        //           (!_isOmitDefaultProperties ||
        //            !_typeInferrer.IsDefaultValue(item));
        //}

        //private Boolean IsCheckCircularRefs(NodeTypes nodeType)
        //{
        //    return nodeType != NodeTypes.Primitive && !_isIgnoreCircularDependencies;
        //}

        //private Boolean TryHandleCircularReference(Object? o,
        //                                           Type propType,
        //                                           NodeTypes nodeType,
        //                                           ISerializerSettings settings)
        //{
        //    if (o == null || _pathReferences.Add(o))
        //    {
        //        _pathObjects.Add(o);
        //        return false;
        //    }

        //    //uh oh... circular reference detected
        //    switch (settings.CircularReferenceBehavior)
        //    {
        //        case CircularReference.IgnoreObject:
        //            if (IsPrintNullProperties)
        //                PrintObject(null, propType, nodeType, settings);


        //            break;
        //        case CircularReference.ThrowException:
        //            throw new CircularReferenceException(_pathStack, PathSeparator);

        //        case CircularReference.SerializePath:
        //            var objIndex = _pathObjects.IndexOf(o);
        //            PrintCircularDependency(objIndex, settings);
        //            break;
        //        case CircularReference.NoValidation:
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }

        //    return true;
        //}

        //protected readonly Boolean _isIgnoreCircularDependencies;
        //private readonly Boolean _isOmitDefaultProperties;
        protected readonly INodeTypeProvider _nodeTypes;
        private readonly IObjectManipulator _objectManipulator;

        ////to map the order to property names
        //private readonly List<Object?> _pathObjects;

        ////to quickly detected if an object exists in this path
        //private readonly HashSet<Object> _pathReferences;

        //to print the x/json path by property/index
        //private readonly List<String> _pathStack;
        //protected readonly ISerializerSettings _settings;
        protected readonly ITypeInferrer _typeInferrer;

        public Boolean IsPrintNullProperties { get; }

        protected Boolean IsTextPrinter;
        protected String PathAttribute;

        public Char PathSeparator {get;}

        //public Boolean Equals(ISerializationDepth other)
        //{
        //    return this.AreEqual(other);
        //}
    }
}
