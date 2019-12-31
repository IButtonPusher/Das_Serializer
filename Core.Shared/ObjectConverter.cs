using Das.Serializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;


namespace Das
{
    [SuppressMessage("ReSharper", "UseMethodIsInstanceOfType")]
    internal class ObjectConverter : SerializerCore, IObjectConverter
    {
        private readonly IStateProvider _dynamicFacade;
        private readonly INodeTypeProvider _nodeTypes;
        private readonly IInstantiator _instantiate;
        private readonly ITypeInferrer _types;
        private readonly IObjectManipulator _objects;

        private static readonly ThreadLocal<Dictionary<Object, Object>> References
            = new ThreadLocal<Dictionary<Object, Object>>(() 
                => new Dictionary<Object, Object>());

        [ThreadStatic]
        private static ISerializerSettings _currentSettings;

        [ThreadStatic]
        private static NodeTypes _currentNodeType;

         

        public ObjectConverter(IStateProvider dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _dynamicFacade = dynamicFacade;
            _nodeTypes = dynamicFacade.ScanNodeProvider.TypeProvider;
            _instantiate = dynamicFacade.ObjectInstantiator;
            _types = dynamicFacade.TypeInferrer;
            _objects = dynamicFacade.ObjectManipulator;
        }


        // ReSharper disable once UnusedParameter.Local
        private T ConvertEx<T>(Object obj, T newObject)
            => ConvertEx<T>(obj, _currentSettings);


        public T ConvertEx<T>(Object obj, ISerializerSettings settings)
        {
            _currentSettings = settings;

            if (obj is T already)
                return already;

            var outType = typeof(T);

            var outObj = _instantiate.BuildDefault(outType, settings.CacheTypeConstructors);
            _currentNodeType = _nodeTypes.GetNodeType(outType, settings.SerializationDepth);
            References.Value.Clear();
            
            outObj = Copy(obj, ref outObj);

            return (T) outObj;
        }

        public T ConvertEx<T>(Object obj) => ConvertEx<T>(obj, Settings);

        public Object ConvertEx(Object obj, Type newObjectType, ISerializerSettings settings)
        {
            _currentSettings = settings;
            var newObject = _dynamicFacade.ObjectInstantiator.BuildDefault(newObjectType,
                settings.CacheTypeConstructors);
            _currentSettings = settings;
            return ConvertEx(obj, newObject);
        }

        public T Copy<T>(T from) where T : class => Copy(from, Settings);

        public T Copy<T>(T from, ISerializerSettings settings) where T : class
        {
            _currentSettings = settings;
            var to = FromType(from);
            Copy(from, ref to, settings);
            return to;
        }

        private T FromType<T>(T example) where T : class
        {
            if (!IsInstantiable(typeof(T)))
                return ObjectInstantiator.BuildDefault(example.GetType(), _currentSettings.
                    CacheTypeConstructors) as T;

            return ObjectInstantiator.BuildDefault<T>(_currentSettings.CacheTypeConstructors);
        }

        public void Copy<T>(T from, ref T to, ISerializerSettings settings) where T : class
        {
            _currentSettings = settings;
            to = to ?? FromType(from);
            const SerializationDepth depth = SerializationDepth.Full;
            _currentNodeType = _nodeTypes.GetNodeType(typeof(T), depth);
            var o = (Object)to;

            References.Value.Clear();

            Copy(from, ref o);

            to = (T) o;
        }

        private Object Copy(Object from, ref Object to)
        {
            var settings = _currentSettings;

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
                    to = CopyObjects(from, ref to, toType);
                    break;
                case NodeTypes.Collection:
                    to = CopyLists(from, to, toType);
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
                            settings.CacheTypeConstructors);
                        toProp = Copy(fromProp, ref toProp);
                        props.Add(prop.Name, toProp);
                    }

                    _instantiate.TryGetPropertiesConstructor(toType, out var cInfo);
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
                    if (toType.IsAssignableFrom(fromType))
                    {
                        to = from;
                    }

                    break;
            }

            return to;
        }

        private Object CopyObjects(Object from, ref Object to, Type toType)
        {
            var references = References.Value;

            foreach (var propInfo in _dynamicFacade.TypeManipulator.GetPropertiesToSerialize(toType,
                _currentSettings))
            {
                if (!_objects.TryGetPropertyValue(from, propInfo.Name, out var nextFrom))
                    continue;

                Object nextTo;

                var pType = propInfo.Type;

                if (nextFrom == null)
                {
                    //we are copying but if we don't have the property why null it out on the other object?
                    //it's either already null or it has a value that may be useful...
                    //if (!_types.IsLeaf(pType, false))
                    //    _objects.SetPropertyValue(ref to, propInfo.Name, null);
                    continue;
                }

                _currentNodeType = _nodeTypes.GetNodeType(pType, _currentSettings.SerializationDepth);

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
                    _currentSettings.CacheTypeConstructors);
                
                references.Add(nextFrom, nextTo);
                nextTo = Copy(nextFrom, ref nextTo);
                references[nextFrom] = nextTo;

                ObjectManipulator.SetPropertyValue(ref to, propInfo.Name, nextTo);
            }

            return to;
        }

        private Object CopyLists(Object from, Object to, Type toType)
        {
            var toListType = TypeInferrer.GetGermaneType(toType);
            var fromList = from as IEnumerable;
            var tempTo = new List<Object>();
            _currentNodeType = _nodeTypes.GetNodeType(toListType,
                _currentSettings.SerializationDepth);

            if (fromList == null)
                return to;

            foreach (var fromItem in fromList)
            {
                var toItem = _instantiate.BuildDefault(toListType, 
                    _currentSettings.CacheTypeConstructors);
                toItem = Copy(fromItem, ref toItem);
                if (toItem != null)
                    tempTo.Add(toItem);
            }

            to = SpawnCollection(tempTo.ToArray(), toType, _currentSettings, toListType);

            return to;
        }

        public Object SpawnCollection(Object[] objects, Type collectionType,
            ISerializerSettings settings, Type collectionGenericArgs = null)
        {
            _currentSettings = settings;
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

            if (collectionType.GetConstructor(new[] {itemType}) != null)
                return Activator.CreateInstance(collectionType, objects);

            var gargs = itemType.GetGenericArguments();

            var buildDictionary = gargs.Length == 2 &&
                                  collectionType.IsAssignableFrom(collectionType) &&
                                  TryGetCtor(out var ctor);

            if (!buildDictionary)
                return BuildCollectionDynamically(collectionType, objects);

            var regularDic = typeof(Dictionary<,>).MakeGenericType(gargs);
            var dicObj = BuildCollectionDynamically(regularDic, objects);
            return Activator.CreateInstance(collectionType, dicObj);

            Boolean TryGetCtor(out ConstructorInfo c)
            {
                var otherDic = typeof(IDictionary<,>).MakeGenericType(gargs);
                c = collectionType.GetConstructor(new[] {otherDic});
                return ctor != null;
            }
        }

        private Object BuildCollectionDynamically(Type collectionType, Object[] objects)
        {
            var val = _instantiate.BuildDefault(collectionType,
                _currentSettings.CacheTypeConstructors);
            var addDelegate = _dynamicFacade.TypeManipulator.GetAdder(val as IEnumerable);

            foreach (var child in objects)
                addDelegate(val, child);

            return val;
        }
    }
}