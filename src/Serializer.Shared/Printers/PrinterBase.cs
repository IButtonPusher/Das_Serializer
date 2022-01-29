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
    public abstract class PrinterBase<TMany, TFew, TWriter> : IObjectPrinter<TMany, TFew, TWriter>
                                        
        where TMany : IEnumerable<TFew>
        where TWriter : IRemunerable<TMany, TFew>
    {
        protected PrinterBase(ITypeInferrer typeInferrer,
                              INodeTypeProvider nodeTypes,
                              IObjectManipulator objectManipulator,
                              Boolean isPrintNullProperties,
                              Char pathSeparator,
                              ITypeManipulator typeManipulator)
        {
            _typeInferrer = typeInferrer;
            _nodeTypes = nodeTypes;
            _objectManipulator = objectManipulator;
            _typeManipulator = typeManipulator;

            PathSeparator = pathSeparator;
            PathAttribute = "$ref";
            IsTextPrinter = true;

            IsPrintNullProperties = isPrintNullProperties;
        }

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
                (o, propType, nodeType, settings, this, writer))
            {
                return true;
            }


            switch (nodeType)
            {
                case NodeTypes.Primitive:
                    PrintPrimitive(o, writer, propType);
                    break;

                case NodeTypes.Object:
                case NodeTypes.PropertiesToConstructor:
                    PrintReferenceType(o, propType, nodeType, 
                        writer, settings, circularReferenceHandler);
                    break;

                case NodeTypes.Collection:
                    PrintCollection(o, propType, writer, 
                        settings, circularReferenceHandler);
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
                        PrintReferenceType(o, propType, nodeType,
                            writer, settings, circularReferenceHandler);
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


       

        public virtual void PrintReferenceType(Object? value,
                                               Type valType,
                                               NodeTypes nodeType,
                                               TWriter writer,
                                               ISerializerSettings settings,
                                               ICircularReferenceHandler circularReferenceHandler)
        {
            var properyValues = _objectManipulator.GetPropertyResults(value, valType, settings);
            PrintProperties(value, nodeType, properyValues, writer, 
                PrintProperty, settings, circularReferenceHandler);
        }

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
        
        protected abstract void PrintCollection(Object? value,
                                                Type valType,
                                                TWriter writer,
                                                ISerializerSettings settings,
                                                ICircularReferenceHandler circularReferenceHandler);
        
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

        protected virtual void PrintProperties(Object? obj,
                                               NodeTypes nodeType,
            IEnumerable<KeyValuePair<PropertyInfo, Object?>> values,
                                               TWriter writer,
                                               Action<PropertyInfo, Object?, TWriter, ISerializerSettings,
                                                   ICircularReferenceHandler> exe,
                                               ISerializerSettings settings,
                                               ICircularReferenceHandler circularReferenceHandler)
        {
            if (ReferenceEquals(null, obj))
                return;

            var ts = _typeManipulator.GetTypeStructure(obj.GetType());

            for (var c = 0; c < ts.Properties.Length; c++)
            {
                var prop = ts.Properties[c];
                if (!ShouldPrintValue(obj, nodeType, prop, settings, out var val))
                    continue;

                exe(prop.PropertyInfo, val, writer, settings, circularReferenceHandler);
                
            }

            //foreach (var val in values)
            //{
            //    exe(val.Key, val.Value, writer, settings, circularReferenceHandler);
            //}
        }

        protected abstract Boolean ShouldPrintValue(Object obj,
                                                    NodeTypes nodeType,
                                                    IPropertyAccessor prop,
                                                    ISerializerSettings settings,
                                                    out Object? value);


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

      
        protected readonly INodeTypeProvider _nodeTypes;
        private readonly IObjectManipulator _objectManipulator;
        protected readonly ITypeManipulator _typeManipulator;

      
        protected readonly ITypeInferrer _typeInferrer;

        public Boolean IsPrintNullProperties { get; }

        protected Boolean IsTextPrinter;
        protected String PathAttribute;

        public Char PathSeparator {get;}
    }
}
