using System;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer
{
    public partial class ObjectConverter
    {
        public T Copy<T>(T from) where T : class
        {
            return Copy(from, Settings);
        }

        public T Copy<T>(T from,
                         ISerializerSettings settings)
            where T : class
        {
            var ttype = typeof(T);
            var ftype = from.GetType() ?? ttype;

            if (!IsInstantiable(ttype))
            {
                if (IsInstantiable(ftype))
                {
                    var fnodetype = _nodeTypes.GetNodeType(ftype);

                    if (ttype.IsAssignableFrom(ftype))
                    {
                        switch (fnodetype)
                        {
                            case NodeTypes.Primitive:
                                return from;
                            case NodeTypes.PropertiesToConstructor:
                            case NodeTypes.Dynamic:
                            {
                                var refs = new Dictionary<Object, Object>();
                                if (TryCopyObjectViaProperties(from, ftype, refs, settings) is T good)
                                    return good;
                                break;
                            }
                        }
                    }
                }
            }


            var to = FromType(from, settings);
            #pragma warning disable 8634
            Copy(from, ref to, settings);
            #pragma warning restore 8634
            return to;
        }


        private Object? TryCopyObjectViaProperties(Object from,
                                                   Type toType,
                                                   Dictionary<object, object> references,
                                                   ISerializerSettings settings)
        {  
            var values = GetPropertiesToConstructorArgs(from, toType, references,
                settings, out var cInfo);

            if (values != null)
                return cInfo.Invoke(values);

            return default;

        }

        public void Copy<T>(T from,
                            ref T to) where T : class
        {
            Copy(from, ref to, _settings); //_dynamicFacade.Settings);
        }

        private Object Copy(Object from,
                            ref Object? to,
                            Dictionary<object, object> references,
                            ISerializerSettings settings,
                            NodeTypes _currentNodeType)
        {
            if (to == null)
            {
                to = from;
                return to;
            }

            var toType = to.GetType();
            var fromType = from.GetType();

            if (typeof(MemberInfo).IsAssignableFrom(fromType) ||
                typeof(MethodInfo).IsAssignableFrom(fromType))
            {
                to = from;
                return to;
            }

            switch (_currentNodeType)
            {
                case NodeTypes.Primitive:
                    if (toType.IsAssignableFrom(fromType))
                        return from;
                    return Convert.ChangeType(from, toType);
                
                case NodeTypes.Object:
                case NodeTypes.Dynamic:
                    CopyMembers(from, ref to, toType, references, settings);
                    break;

                case NodeTypes.Collection:
                    to = CopyLists(from, to, toType, references, settings);
                    break;
                case NodeTypes.PropertiesToConstructor:

                    var values = GetPropertiesToConstructorArgs(from, toType, references,
                        settings, out var cInfo);

                    if (values != null)
                    {
                        if (values.Length >= 3 && values[1] == null)
                        {}
                        to = cInfo.Invoke(values);
                    }

                    break;
                default:
                    if (toType.IsAssignableFrom(fromType)) to = from;

                    break;
            }

            return to;
        }

           
        public void Copy<T>(T from,
                            ref T to,
                            ISerializerSettings settings) where T : class
        {
            var ttime = typeof(T);

            to ??= FromType(from, settings);

            if (to == null)
            {
                return;
            }
      


            var _currentNodeType = IsInstantiable(ttime)
                ? _nodeTypes.GetNodeType(ttime)
                : _nodeTypes.GetNodeType(to.GetType());
            var o = (Object) to;
        
            var refs = new Dictionary<Object, Object>();
            

            Copy(from, ref o, refs, settings, _currentNodeType);

            refs.Clear();

            if (o is T good)
                to = good;
            else throw new InvalidCastException(from.GetType() + " -> " + typeof(T).Name);
        }

        private void CopyMembers(Object from,
                                   ref Object to,
                                   Type toType,
                                   Dictionary<object, object> references,
                                   ISerializerSettings settings)
        {
            var fromType = from.GetType();

            foreach (var propInfo in _dynamicFacade.TypeManipulator.GetMembersToSerialize(fromType, //toType,
                settings.SerializationDepth))
            {
                var nextFrom = propInfo.GetValue(from);
                
                //if (!_objects.TryGetPropertyValue(from, propInfo.Name, out var nextFrom))
                //    continue;

                Object nextTo;

                var pType = propInfo.Type;

                if (nextFrom == null)
                    //we are copying but if we don't have the property why null it out on the other object?
                    //it's either already null or it has a value that may be useful...
                    //if (!_types.IsLeaf(pType, false))
                    //    _objects.SetPropertyValue(ref to, propInfo.Name, null);
                    continue;

                var nextFromType = nextFrom.GetType();
                if (typeof(Type).IsAssignableFrom(nextFromType))
                {
                    continue;
                }

                var _currentNodeType = _nodeTypes.GetNodeType(pType);

                if (references.TryGetValue(nextFrom, out var found))
                {
                    nextTo = found;
                    ObjectManipulator.SetPropertyValue(ref to, propInfo.Name, 
                        PropertyNameFormat.Default, nextTo);
                    continue;
                }

                if (_currentNodeType == NodeTypes.Primitive)
                {
                    if (pType.IsAssignableFrom(nextFromType))
                        ObjectManipulator.SetPropertyValue(ref to, propInfo.Name,
                            PropertyNameFormat.Default, nextFrom);
                    continue;
                }

                if (!propInfo.IsMemberSerializable)
                {
                    references[nextFrom] = nextFrom;
                    if (!propInfo.TrySetValue(ref to, nextFrom))
                    {

                    }
                    continue;
                }


                var mnextTo = _instantiate.BuildDefault(nextFromType,
                    settings.CacheTypeConstructors);

                if (mnextTo == null)
                {
                    mnextTo = _instantiate.BuildDefault(toType,
                        settings.CacheTypeConstructors);
                    if (mnextTo == null)
                        throw new NullReferenceException(nextFrom?.ToString());
                }

                nextTo = mnextTo;

                references.Add(nextFrom, nextTo);
                if (references.Count > 200)
                {}
                nextTo = Copy(nextFrom, ref nextTo!, references, settings, _currentNodeType);
                references[nextFrom] = nextTo;

                propInfo.TrySetValue(ref to, nextTo);
                //ObjectManipulator.SetPropertyValue(ref to, propInfo.Name, 
                //    PropertyNameFormat.Default, nextTo);
            }

            if ((settings.SerializationDepth & SerializationDepth.PrivateFields) == SerializationDepth.PrivateFields)
            {
                foreach (var field in _dynamicFacade.TypeManipulator.GetRecursivePrivateFields(fromType))
                {
                    var fromVal = field.GetValue(from);
                    if (fromVal == null)
                        continue;

                    if (references.TryGetValue(fromVal, out var found))
                    {
                        field.SetValue(to, found);
                        continue;
                    }

                    var nodeType = _nodeTypes.GetNodeType(field.FieldType);

                    //var toVal = IsInstantiable(toListType)
                    var toVal = FromType(fromVal, settings);
                    var fromValCopy = Copy(fromVal, ref toVal, references, settings, nodeType);


                    field.SetValue(to, fromValCopy);
                }
            }

            //return to;
        }

    }
}
