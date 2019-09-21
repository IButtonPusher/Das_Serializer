using Das.Serializer;
using Serializer.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;


namespace Das
{
    [SuppressMessage("ReSharper", "UseMethodIsInstanceOfType")]
    internal class ObjectConverter : SerializerCore, IObjectConverter
    {
        private readonly IStateProvider _dynamicFacade;
        private readonly IInstantiator _instantiate;
        private readonly ITypeInferrer _types;
        private readonly IObjectManipulator _objects;

        public ObjectConverter(IStateProvider dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _dynamicFacade = dynamicFacade;
            _instantiate = dynamicFacade.ObjectInstantiator;
            _types = dynamicFacade.TypeInferrer;
            _objects = dynamicFacade.ObjectManipulator;
        }


        // ReSharper disable once UnusedParameter.Local
        private T ConvertEx<T>(Object obj, T newObject, ISerializerSettings settings)
            => ConvertEx<T>(obj, settings);

        public T ConvertEx<T>(Object obj, ISerializerSettings settings)
        {
            if (obj is T already)
                return already;

            var outType = typeof(T);

            var outObj = _instantiate.BuildDefault(outType, settings.CacheTypeConstructors);
            var nodeType = _dynamicFacade.GetNodeType(outType, settings.SerializationDepth);
            outObj = Copy(obj, ref outObj, nodeType, new
                Dictionary<Object, Object>(), settings);

            return (T) outObj;
        }

        public T ConvertEx<T>(Object obj) => ConvertEx<T>(obj, Settings);

        public Object ConvertEx(Object obj, Type newObjectType, ISerializerSettings settings)
        {
            var newObject = _dynamicFacade.ObjectInstantiator.BuildDefault(newObjectType,
                settings.CacheTypeConstructors);
            return ConvertEx(obj, newObject, settings);
        }

        public T Copy<T>(T @from) where T : class => Copy(from, Settings);

        public T Copy<T>(T @from, ISerializerSettings settings) where T : class
        {
            var to = FromType(from, settings);
            Copy(from, ref to, settings);
            return to;
        }

        private T FromType<T>(T example, ISerializerSettings settings) where T : class
        {
            if (!IsInstantiable(typeof(T)))
                return BuildDefault(example.GetType(), settings.CacheTypeConstructors) as T;

            return BuildDefault<T>(settings.CacheTypeConstructors);
        }

        public void Copy<T>(T @from, ref T to, ISerializerSettings settings) where T : class
        {
            to = to ?? FromType(from, settings);
            var depth = SerializationDepth.Full;
            var nodeType = _dynamicFacade.GetNodeType(typeof(T), depth);
            var o = to as Object;

            Copy(from, ref o, nodeType, new Dictionary<Object, Object>(), settings);

            to = (T) o;
        }

        private Object Copy(Object from, ref Object to, NodeTypes nodeType,
            Dictionary<Object, Object> references, ISerializerSettings settings)
        {
            if (to == null)
            {
                to = from;
                return to;
            }

            var toType = to.GetType();
            var fromType = from.GetType();


            switch (nodeType)
            {
                case NodeTypes.Primitive:
                    if (toType.IsAssignableFrom(fromType))
                        return from;
                    return Convert.ChangeType(from, toType);
                case NodeTypes.Object:
                    to = CopyObjects(from, ref to, toType, references, settings);
                    break;
                case NodeTypes.Collection:
                    to = CopyLists(from, to, toType, references, settings);
                    break;
                case NodeTypes.PropertiesToConstructor:

                    var props = new Dictionary<String, Object>();
                    foreach (var prop in _types.GetPublicProperties(toType))
                    {
                        if (!_objects.TryGetPropertyValue(@from, prop.Name, out var fromProp))
                            continue;

                        var nextNode = _dynamicFacade.GetNodeType(prop.PropertyType,
                            settings.SerializationDepth);
                        var toProp = _instantiate.BuildDefault(prop.PropertyType,
                            settings.CacheTypeConstructors);
                        toProp = Copy(fromProp, ref toProp, nextNode, references, settings);
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

        private Object CopyObjects(Object from, ref Object to, Type toType,
            Dictionary<Object, Object> references, ISerializerSettings settings)
        {
            foreach (var propInfo in _dynamicFacade.GetPropertiesToSerialize(toType,
                settings.SerializationDepth))
            {
                if (!_objects.TryGetPropertyValue(from, propInfo.Name, out var nextFrom))
                    continue;

                Object nextTo;

                var pType = _dynamicFacade.InstanceMemberType(propInfo);

                if (nextFrom == null)
                {
                    if (!_types.IsLeaf(pType, false))
                        _objects.SetPropertyValue(ref to, propInfo.Name, null);
                    continue;
                }

                var nextNode = _dynamicFacade.GetNodeType(pType, settings.SerializationDepth);

                if (references.ContainsKey(nextFrom))
                {
                    nextTo = references[nextFrom];
                    SetPropertyValue(ref to, propInfo.Name, nextTo);
                    continue;
                }

                else if (nextNode == NodeTypes.Primitive)
                {
                    if (pType.IsAssignableFrom(nextFrom.GetType()))
                        SetPropertyValue(ref to, propInfo.Name, nextFrom);
                    continue;
                }
                else
                {
                    nextTo = BuildDefault(pType, settings.CacheTypeConstructors);
                    references.Add(nextFrom, nextTo);
                    nextTo = Copy(nextFrom, ref nextTo, nextNode, references, settings);
                    references[nextFrom] = nextTo;
                }

                SetPropertyValue(ref to, propInfo.Name, nextTo);
            }

            return to;
        }

        private Object CopyLists(Object from, Object to, Type toType,
            Dictionary<Object, Object> references, ISerializerSettings settings)
        {
            var toListType = GetGermaneType(toType);
            var fromList = from as IEnumerable;
            var tempTo = new List<Object>();
            var listNode = _dynamicFacade.GetNodeType(toListType, settings.SerializationDepth);

            if (fromList == null)
                return to;

            foreach (var fromItem in fromList)
            {
                var toItem = BuildDefault(toListType, settings.CacheTypeConstructors);
                toItem = Copy(fromItem, ref toItem, listNode, references, settings);
                if (toItem != null)
                    tempTo.Add(toItem);
            }

            to = SpawnCollection(tempTo.ToArray(), toType, settings, toListType);

            return to;
        }

        public Object SpawnCollection(Object[] objects, Type collectionType,
            ISerializerSettings settings, Type collectionGenericArgs = null)
        {
            var itemType = collectionGenericArgs ?? GetGermaneType(collectionType);

            if (collectionType.IsArray)
            {
                //build via initializer if possible
                var arr2 = Array.CreateInstance(itemType, objects.Length);
                var i = 0;

                foreach (var child in objects)
                    arr2.SetValue(child, i++);

                if (collectionType.IsArray)
                    return arr2;
            }

            if (collectionType.GetConstructor(new[] {itemType}) != null)
                return Activator.CreateInstance(collectionType, objects);

            var gargs = itemType.GetGenericArguments();

            var buildDictionary = gargs.Length == 2 &&
                                  collectionType.IsAssignableFrom(collectionType) &&
                                  TryGetCtor(out var ctor);

            if (!buildDictionary)
                return BuildCollectionDynamically(collectionType, objects, settings);

            var regularDic = typeof(Dictionary<,>).MakeGenericType(gargs);
            var dicObj = BuildCollectionDynamically(regularDic, objects, settings);
            return Activator.CreateInstance(collectionType, dicObj);

            Boolean TryGetCtor(out ConstructorInfo c)
            {
                var otherDic = typeof(IDictionary<,>).MakeGenericType(gargs);
                c = collectionType.GetConstructor(new[] {otherDic});
                return ctor != null;
            }
        }

        private Object BuildCollectionDynamically(Type collectionType, Object[] objects,
            ISerializerSettings settings)
        {
            var val = _instantiate.BuildDefault(collectionType, settings.CacheTypeConstructors);
            var addDelegate = _dynamicFacade.TypeManipulator.GetAdder(val as IEnumerable);

            foreach (var child in objects)
                addDelegate(val, child);

            return val;
        }
    }
}