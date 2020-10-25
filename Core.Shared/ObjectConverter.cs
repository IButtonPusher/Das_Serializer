using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das
{
    [SuppressMessage("ReSharper", "UseMethodIsInstanceOfType")]
    public class ObjectConverter : SerializerCore, IObjectConverter
    {
        public ObjectConverter(IStateProvider dynamicFacade, 
                               ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _dynamicFacade = dynamicFacade;
            _nodeTypes = dynamicFacade.ScanNodeProvider.TypeProvider;
            _instantiate = dynamicFacade.ObjectInstantiator;
            _types = dynamicFacade.TypeInferrer;
            _objects = dynamicFacade.ObjectManipulator;
        }


        public T ConvertEx<T>(Object obj, ISerializerSettings settings)
        {
            if (obj is T already)
                return already;

            //_currentSettings = settings;

            var outType = typeof(T);

            var outObj = _instantiate.BuildDefault(outType, settings.CacheTypeConstructors);
            _currentNodeType = _nodeTypes.GetNodeType(outType, settings.SerializationDepth);

            var refs = References.Value;
            refs.Clear();

            outObj = Copy(obj, ref outObj, refs, settings);

            return (T) outObj;
        }

        public T ConvertEx<T>(Object obj)
        {
            return ConvertEx<T>(obj, Settings);
        }

        public Object ConvertEx(Object obj, Type newObjectType, ISerializerSettings settings)
        {
            //_currentSettings = settings;
            var newObject = _dynamicFacade.ObjectInstantiator.BuildDefault(newObjectType,
                settings.CacheTypeConstructors) ?? throw new NullReferenceException(newObjectType.Name);
            //_currentSettings = settings;
            return ConvertEx(obj, newObject, settings);
        }

        public T Copy<T>(T from) where T : class
        {
            return Copy(from, Settings);
        }

        public T Copy<T>(T from, ISerializerSettings settings)
            where T : class
        {
            //_currentSettings = settings;
            var to = FromType(from, settings);
#pragma warning disable 8634
            Copy(from, ref to, settings);
#pragma warning restore 8634
            return to!;
        }

        public void Copy<T>(T from, ref T to, ISerializerSettings settings) where T : class
        {
            //_currentSettings = settings;
            to ??= FromType(from, settings);
            const SerializationDepth depth = SerializationDepth.Full;
            _currentNodeType = _nodeTypes.GetNodeType(typeof(T), depth);
            var o = (Object) to;

            var refs = References.Value;
            refs.Clear();

            Copy(from, ref o, refs, settings);

            if (o is T good)
                to = good;
            else throw new InvalidCastException(from.GetType() + " -> " + typeof(T).Name);

            //to = (T) o;
        }

        public void Copy<T>(T from, ref T to) where T : class
        {
            Copy(from, ref to, _dynamicFacade.Settings);
        }

        public Object SpawnCollection(Object[] objects, Type collectionType,
                                      ISerializerSettings settings, Type? collectionGenericArgs = null)
        {
            //_currentSettings = settings;
            var itemType = collectionGenericArgs ?? TypeInferrer.GetGermaneType(collectionType);

            if (collectionType.IsArray)
            {
                //build via initializer if possible
                var arr2 = Array.CreateInstance(itemType, objects.Length);
                var i = 0;

                foreach (var child in objects)
                    arr2.SetValue(child, i++);

                return arr2;
            }


            var ctor = collectionType.GetConstructor(new[] {itemType});

            if (ctor != null)
                return Activator.CreateInstance(collectionType, objects);


            var gargs = itemType.GetGenericArguments();

            var buildDictionary = gargs.Length == 2 &&
                                  collectionType.IsAssignableFrom(collectionType) &&
                                  TryGetCtor(out ctor);

            if (!buildDictionary)
                return BuildCollectionDynamically(collectionType, objects, settings);

            var regularDic = typeof(Dictionary<,>).MakeGenericType(gargs);
            var dicObj = BuildCollectionDynamically(regularDic, objects, settings);
            return Activator.CreateInstance(collectionType, dicObj);

            Boolean TryGetCtor(out ConstructorInfo c)
            {
                var otherDic = typeof(IDictionary<,>).MakeGenericType(gargs);
                c = collectionType.GetConstructor(new[] {otherDic})!;
                return ctor != null;
            }
        }

        private Object BuildCollectionDynamically(Type collectionType,
                                                  Object[] objects,
                                                  ISerializerSettings settings)
        {
            var val = _instantiate.BuildDefault(collectionType,
                settings.CacheTypeConstructors) ?? throw new MissingMethodException(
                collectionType.Name);

            if (objects.Length == 0)
                return val;

            switch (val)
            {
                case IList ilist:
                    foreach (var o in objects)
                        ilist.Add(o);

                    return val;

                case IEnumerable ienum:
                    var addDelegate = _dynamicFacade.TypeManipulator.GetAdder(ienum);

                    foreach (var child in objects)
                        addDelegate(val, child);

                    return val;

                default:
                    throw new InvalidOperationException(collectionType.Name);
            }
        }


        // ReSharper disable once UnusedParameter.Local
        private T ConvertEx<T>(Object obj, T newObject, ISerializerSettings settings)
        {
            return ConvertEx<T>(obj, settings);
        }

        private Object Copy(Object from, ref Object? to,
                            Dictionary<object, object> references,
                            ISerializerSettings settings)
        {
            //var settings = _currentSettings;

            if (to == null)
            {
                to = from;
                return to;
            }

            var toType = to.GetType();
            var fromType = from.GetType();


            switch (_currentNodeType)
            {
                case NodeTypes.Primitive:
                    if (toType.IsAssignableFrom(fromType))
                        return from;
                    return Convert.ChangeType(from, toType);
                case NodeTypes.Object:
                case NodeTypes.Dynamic:
                    to = CopyObjects(from, ref to, toType, references, settings);
                    break;
                case NodeTypes.Collection:
                    to = CopyLists(from, to, toType, references, settings);
                    break;
                case NodeTypes.PropertiesToConstructor:

                    var props = new Dictionary<String, Object>();
                    foreach (var prop in _types.GetPublicProperties(toType))
                    {
                        if (!_objects.TryGetPropertyValue(from, prop.Name, out var fromProp))
                            continue;

                        _currentNodeType = _nodeTypes.GetNodeType(prop.PropertyType,
                            settings.SerializationDepth);
                        var toProp = _instantiate.BuildDefault(prop.PropertyType,
                            settings.CacheTypeConstructors) ?? throw new NullReferenceException(prop.Name);
                        toProp = Copy(fromProp, ref toProp, references, settings);
                        props.Add(prop.Name, toProp);
                    }

                    if (!_instantiate.TryGetPropertiesConstructor(toType, out var cInfo))
                        throw new InvalidOperationException(toType + " " + NodeTypes.PropertiesToConstructor);

                    var values = new List<Object>();
                    foreach (var conParam in cInfo.GetParameters())
                    {
                        var search = _dynamicFacade.TypeInferrer.ToPropertyStyle(conParam.Name);

                        if (props.ContainsKey(search))
                            values.Add(props[search]);
                    }

                    to = cInfo.Invoke(values.ToArray());
                    break;
                default:
                    if (toType.IsAssignableFrom(fromType)) to = @from;

                    break;
            }

            return to;
        }

        private Object CopyLists(Object from,
                                 Object to,
                                 Type toType,
                                 Dictionary<object, object> references,
                                 ISerializerSettings settings)
        {
            var toListType = TypeInferrer.GetGermaneType(toType);
            var fromList = from as IEnumerable;
            var tempTo = new List<Object>();
            _currentNodeType = _nodeTypes.GetNodeType(toListType,
                settings.SerializationDepth);

            if (fromList == null)
                return to;

            foreach (var fromItem in fromList)
            {
                var toItem = _instantiate.BuildDefault(toListType,
                    settings.CacheTypeConstructors);
                //?? throw new NullReferenceException(toListType.Name);
                toItem = Copy(fromItem, ref toItem, references, settings);
                if (toItem != null)
                    tempTo.Add(toItem);
            }

            to = SpawnCollection(tempTo.ToArray(), toType, settings, toListType);

            return to;
        }

        private Object CopyObjects(Object from,
                                   ref Object to,
                                   Type toType,
                                   Dictionary<object, object> references,
                                   ISerializerSettings settings)
        {
            foreach (var propInfo in _dynamicFacade.TypeManipulator.GetPropertiesToSerialize(toType,
                settings))
            {
                if (!_objects.TryGetPropertyValue(from, propInfo.Name, out var nextFrom))
                    continue;

                Object nextTo;

                var pType = propInfo.Type;

                if (nextFrom == null)
                    //we are copying but if we don't have the property why null it out on the other object?
                    //it's either already null or it has a value that may be useful...
                    //if (!_types.IsLeaf(pType, false))
                    //    _objects.SetPropertyValue(ref to, propInfo.Name, null);
                    continue;

                _currentNodeType = _nodeTypes.GetNodeType(pType, settings.SerializationDepth);

                if (references.TryGetValue(nextFrom, out var found))
                {
                    nextTo = found;
                    ObjectManipulator.SetPropertyValue(ref to, propInfo.Name, nextTo);
                    continue;
                }

                if (_currentNodeType == NodeTypes.Primitive)
                {
                    if (pType.IsAssignableFrom(nextFrom.GetType()))
                        ObjectManipulator.SetPropertyValue(ref to, propInfo.Name, nextFrom);
                    continue;
                }


                nextTo = _instantiate.BuildDefault(nextFrom.GetType(),
                    settings.CacheTypeConstructors) ?? throw new NullReferenceException(nextFrom?.ToString());

                references.Add(nextFrom, nextTo);
                nextTo = Copy(nextFrom, ref nextTo!, references, settings);
                references[nextFrom] = nextTo;

                ObjectManipulator.SetPropertyValue(ref to, propInfo.Name, nextTo);
            }

            return to;
        }

        private T FromType<T>(T example, ISerializerSettings settings) where T : class
        {
            if (!IsInstantiable(typeof(T)))
                return (ObjectInstantiator.BuildDefault(example.GetType(), settings.CacheTypeConstructors) as T)!;

            return ObjectInstantiator.BuildDefault<T>(settings.CacheTypeConstructors);
        }

        private static readonly ThreadLocal<Dictionary<Object, Object>> References
            = new ThreadLocal<Dictionary<Object, Object>>(()
                => new Dictionary<Object, Object>());

        //[ThreadStatic]
        //private static ISerializerSettings? _currentSettings;

        [ThreadStatic] private static NodeTypes _currentNodeType;

        private readonly IStateProvider _dynamicFacade;
        private readonly IInstantiator _instantiate;
        private readonly INodeTypeProvider _nodeTypes;
        private readonly IObjectManipulator _objects;
        private readonly ITypeInferrer _types;
    }
}