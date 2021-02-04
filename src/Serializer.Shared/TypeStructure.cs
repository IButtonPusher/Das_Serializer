using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Das.Serializer.Types;

namespace Das.Serializer
{
    public class TypeStructure : TypeCore,
                                 ITypeStructure
    {
        public TypeStructure(Type type,
                             Boolean isPropertyNamesCaseSensitive,
                             ISerializationDepth depth,
                             ITypeManipulator state)
                             //INodePool nodePool)
            : base(state.Settings)
        {
            Type = type;
            _xmlIgnores = new HashSet<String>();
            //_propertyValues = new ThreadLocal<PropertyValueIterator<IProperty>>(()
            //    => new PropertyValueIterator<IProperty>());

            Depth = depth.SerializationDepth;
            _types = state;
            //_nodePool = nodePool;

            if (type.IsDefined(typeof(SerializeAsTypeAttribute), false))
            {
                var serAs = type.GetCustomAttributes(typeof(SerializeAsTypeAttribute
                ), false).First() as SerializeAsTypeAttribute;

                if (serAs?.TargetType != null)
                    type = serAs.TargetType;
            }

            _getOnly = new Dictionary<String, Func<Object, Object>>();
            _propGetters = new Dictionary<String, Func<Object, Object>>();
            _propGetterList = new List<KeyValuePair<String, Func<Object, Object>>>();
            _getDontSerialize = new Dictionary<String, Func<Object, Object>>();
            _fieldGetters = new Dictionary<String, Func<Object, Object>>();
            _propertyAttributes = new DoubleDictionary<String, Type, Object>();
            _propertyInfos = new Dictionary<string, PropertyInfo>();

            _fieldSetters = new SortedList<String, Action<Object, Object?>>();

            var cmp = isPropertyNamesCaseSensitive
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;

            _propertySetters = new SortedList<String, PropertySetter>(cmp);
            _readOnlySetters = new SortedList<String, Action<Object, Object?>>(cmp);
            MemberTypes = new Dictionary<String, INamedField>(cmp);


            if (_types.IsLeaf(type, true) || IsCollection(type))
                return;

            CreatePropertyDelegates(type, depth);
            CreateFieldDelegates(type, depth);

            foreach (var meth in type.GetMethods())
            {
                if (!meth.IsDefined(typeof(OnDeserializedAttribute), false))
                    continue;

                _onDeserializedMethodName = meth.Name;
                break;
            }

            PropertyCount = _propertySetters.Count;
        }

        public Type Type { get; }

        public SerializationDepth Depth { get; }

        public Dictionary<String, INamedField> MemberTypes { get; }


        public Int32 PropertyCount { get; }


        public IEnumerable<KeyValuePair<PropertyInfo, object?>> IteratePropertyValues(Object o,
            ISerializationDepth depth)
        {
            var isRespectXmlIgnoreAttribute = depth.IsRespectXmlIgnore;
            var cnt = _propGetterList.Count;

            for (var c = 0; c < cnt; c++)
            {
                var kvp = _propGetterList[c];
                if (isRespectXmlIgnoreAttribute && _xmlIgnores.Contains(kvp.Key))
                    continue;
                var name = kvp.Key;
                var propInfo = _propertyInfos[name];
                var val = kvp.Value(o);

                yield return new KeyValuePair<PropertyInfo, Object?>(propInfo, val);
            }
        }

        public Boolean OnDeserialized(Object obj,
                                      IObjectManipulator objectManipulator)
        {
            if (_onDeserializedMethodName == null)
                return false;

            objectManipulator.Method(obj, _onDeserializedMethodName, new Object[0]);
            return true;
        }


        //public IPropertyValueIterator<IProperty> GetPropertyValues(Object o,
        //                                                           ISerializationDepth depth)
        //{
        //    var isRespectXmlIgnoreAttribute = depth.IsRespectXmlIgnore;
        //    var cnt = _propGetterList.Count;
        //    var res = PropertyValues;
        //    res.Clear();

        //    for (var c = 0; c < cnt; c++)
        //    {
        //        var kvp = _propGetterList[c];
        //        if (isRespectXmlIgnoreAttribute && _xmlIgnores.Contains(kvp.Key))
        //            continue;
        //        var name = kvp.Key;
        //        var val = kvp.Value(o);
        //        var type = MemberTypes[name].Type;

        //        var pooledProp = _nodePool.GetProperty(name, val, type, Type);

        //        res.Add(pooledProp);
        //    }

        //    return res;
        //}

        /// <summary>
        ///     Returns properties and/or fields depending on specified depth
        /// </summary>
        public IEnumerable<INamedField> GetMembersToSerialize(ISerializationDepth depth)
        {
            foreach (var kvp in GetValueGetters(depth))
            {
                yield return MemberTypes[kvp.Key];
            }
        }

        public object? GetValue(Object o,
                                String propertyName)
        {
            if (_propGetters.TryGetValue(propertyName, out var getter))
                return getter(o);
            if (_getOnly.TryGetValue(propertyName, out var getOnly))
                return getOnly(o);
            if (_getDontSerialize.TryGetValue(propertyName, out var notSerialized))
                return notSerialized(o);
            return null;
        }

        //IProperty? ITypeStructure.GetProperty(Object o,
        //                                           String propertyName)
        //{
        //    try
        //    {
        //        var val = GetPropertyValueImpl(o, propertyName, out var mInfo);
        //        if (mInfo == null)
        //            return null;

        //        //if (!MemberTypes.TryGetValue(propertyName, out var mInfo))
        //        //    return null;
        //        //var pType = mInfo.Type;

        //        //Object val;

        //        //if (_propGetters.TryGetValue(propertyName, out var getter))
        //        //    val = getter(o);
        //        //else if (_getOnly.TryGetValue(propertyName, out var getOnly))
        //        //    val = getOnly(o);
        //        //else if (_getDontSerialize.TryGetValue(propertyName, out var notSerialized))
        //        //    val = notSerialized(o);
        //        //else return null;

        //        var pType = mInfo.Type;
        //        var res = _nodePool.GetProperty(propertyName, val, pType, Type);

        //        return res;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new AggregateException(propertyName, ex);
        //    }
        //}

        public Boolean SetFieldValue(String fieldName,
                                     Object targetObj,
                                     Object fieldVal)
        {
            if (!_fieldSetters.TryGetValue(fieldName, out var set))
                return false;
            set(targetObj, fieldVal);
            return true;
        }

        public Boolean SetFieldValue<T>(String fieldName,
                                        Object targetObj,
                                        Object fieldVal)
        {
            if (!_fieldSetters.TryGetValue(fieldName, out var set))
                return false;
            set(targetObj, Convert.ChangeType(fieldVal, typeof(T)));
            return true;
        }

        public Boolean TrySetPropertyValue(String propName,
                                           ref Object targetObj,
                                           Object? propVal,
                                           SerializationDepth depth = SerializationDepth.AllProperties)
        {
            if (_propertySetters.TryGetValue(propName, out var setDel))
            {
                setDel(ref targetObj!, propVal);
                return true;
            }

            if (depth <= SerializationDepth.GetSetProperties)
                return false;

            if (_readOnlySetters.TryGetValue(propName, out var set) ||
                _fieldSetters.TryGetValue(propName, out set))
            {
                set(targetObj, propVal);
                return true;
            }

            var pi = targetObj.GetType().GetProperty(propName);
            if (pi == null)
                return false;

            if (_types.TryCreateReadOnlyPropertySetter(pi, out var pDel))
                _readOnlySetters.Add(pi.Name, pDel);
            else return false;

            pDel(targetObj, propVal);
            return true;
        }

        public void SetPropertyValueUnsafe(String propName,
                                           ref Object targetObj,
                                           Object propVal)
        {
            _propertySetters[propName](ref targetObj!, propVal);
        }


        public Boolean TryGetAttribute<TAttribute>(String memberName,
                                                   out TAttribute value)
            where TAttribute : Attribute
        {
            if (_propertyAttributes.TryGetValue(memberName, typeof(TAttribute), out var items)
                && items is TAttribute attr)
            {
                value = attr;
                return true;
            }

            value = default!;
            return false;
        }


        //protected PropertyValueIterator<IProperty> PropertyValues
        //    => _propertyValues.Value!;

        public object? GetPropertyValue(Object o,
                                        String propertyName)
        {
            return GetPropertyValueImpl(o, propertyName, out _);
        }

        //public Boolean SetPropertyValue(ref Object targetObj,
        //                                String propName,
        //                                Object? propVal)
        //{
        //    return SetValue(propName, ref targetObj, propVal, Depth);
        //}

        public override string ToString()
        {
            return GetType().Name + ": " + Type.FullName;
        }


        public bool TryGetPropertyValue(Object obj,
                                        String propertyName,
                                        out Object result)
        {
            result = GetPropertyValueImpl(obj, propertyName, out _)!;
            return result != null;
        }

        private void CreateFieldDelegates(Type type,
                                          ISerializationDepth depth)
        {
            if ((depth.SerializationDepth & SerializationDepth.PrivateFields) !=
                SerializationDepth.PrivateFields)
                return;

            foreach (var fld in type.GetFields(BindingFlags.Public | Const.NonPublic))
            {
                var delGet = _types.CreateFieldGetter(fld);
                _fieldGetters[fld.Name] = delGet;

                var delSet = _types.CreateFieldSetter(fld);
                _fieldSetters[fld.Name] = delSet;

                var member = new DasMember(fld.Name, fld.FieldType);
                MemberTypes[fld.Name] = member;
            }
        }

        private void CreatePropertyDelegates(Type type,
                                             ISerializationDepth depth)
        {
            foreach (var pi in _types.GetPublicProperties(type))
            {
                _propertyInfos.Add(pi.Name, pi);
                if (!pi.CanRead)
                    continue;

                var isSerialize = true;
                var attrs = pi.GetCustomAttributes(true);

                foreach (var attr in attrs)
                {
                    _propertyAttributes.Add(pi.Name, attr.GetType(), attr);
                }

                foreach (var attr in attrs)
                {
                    switch (attr)
                    {
                        case IgnoreDataMemberAttribute _:
                            isSerialize = false;
                            break;
                        case XmlIgnoreAttribute _:
                            _xmlIgnores.Add(pi.Name);
                            if (depth.IsRespectXmlIgnore)
                                isSerialize = false;
                            break;
                    }
                }

                if (!isSerialize)
                {
                    SetPropertyForDynamicAccess(type, pi);
                    continue;
                }

                var member = new DasMember(pi.Name, pi.PropertyType);

                MemberTypes[pi.Name] = member;

                var reallyWrite = false;

                if (pi.CanWrite)
                {
                    var dele = _types.CreatePropertyGetter(type, pi);
                    _propGetters.Add(pi.Name, dele);

                    var sm = _types.CreateSetMethod(pi);
                    //if (sm != null)
                    {
                        reallyWrite = true;
                        _propertySetters.Add(pi.Name, sm);
                    }
                }

                if (reallyWrite)
                    continue;

                if (!_getOnly.ContainsKey(pi.Name))
                    _getOnly.Add(pi.Name, _types.CreatePropertyGetter(type, pi));

                if ((depth.SerializationDepth & SerializationDepth.GetOnlyProperties)
                    != SerializationDepth.GetOnlyProperties)
                    continue;

                if (_types.TryCreateReadOnlyPropertySetter(pi, out var del))
                    _readOnlySetters.Add(pi.Name, del);
            }

            if (_propGetters.Count > 0)
                _propGetterList.AddRange(_propGetters.OrderBy(p => p.Key));
            else if (_getOnly.Count > 0)
                _propGetterList.AddRange(_getOnly.OrderBy(p => p.Key));
        }

        private Object? GetPropertyValueImpl(Object o,
                                             String propertyName,
                                             out INamedField? mInfo)
        {
            if (!MemberTypes.TryGetValue(propertyName, out mInfo))
                return null;

            if (_propGetters.TryGetValue(propertyName, out var getter))
                return getter(o);
            if (_getOnly.TryGetValue(propertyName, out var getOnly))
                return getOnly(o);
            if (_getDontSerialize.TryGetValue(propertyName, out var notSerialized))
                return notSerialized(o);

            mInfo = null;
            return null;
        }

        private IEnumerable<KeyValuePair<String, Func<Object, Object>>> GetValueGetters(
            ISerializationDepth depth)
        {
            var isSet = false;

            foreach (var kvp in _propGetters.OrderBy(p => p.Key))
            {
                isSet = true;
                yield return kvp;
            }

            if (!isSet || (depth.SerializationDepth & SerializationDepth.GetOnlyProperties) != 0)
                foreach (var kvp in _getOnly)
                {
                    yield return kvp;
                }

            if ((depth.SerializationDepth & SerializationDepth.PrivateFields) == 0)
                yield break;

            foreach (var kvp in _fieldGetters)
            {
                yield return kvp;
            }
        }

        private void SetPropertyForDynamicAccess(Type type,
                                                 PropertyInfo pi)
        {
            //even if a property will be excluded from serialization, we may still want
            //to set its value dynamically
            var gtor = _types.CreatePropertyGetter(type, pi);
            _getDontSerialize.Add(pi.Name, gtor);
            if (pi.CanWrite)
            {
                var sp = _types.CreateSetMethod(pi);
                _propertySetters.Add(pi.Name, sp);
            }

            var member = new DasMember(pi.Name, pi.PropertyType);
            MemberTypes[pi.Name] = member;
        }

        private readonly Dictionary<String, Func<Object, Object>> _fieldGetters;
        private readonly SortedList<String, Action<Object, Object?>> _fieldSetters;

        private readonly Dictionary<String, Func<Object, Object>> _getDontSerialize;

        private readonly Dictionary<String, Func<Object, Object>> _getOnly;
        //protected readonly INodePool _nodePool;

        private readonly String? _onDeserializedMethodName;

        private readonly DoubleDictionary<String, Type, Object> _propertyAttributes;
        private readonly Dictionary<String, PropertyInfo> _propertyInfos;

        private readonly SortedList<String, PropertySetter> _propertySetters;

        //private readonly ThreadLocal<PropertyValueIterator<IProperty>> _propertyValues;
        protected readonly List<KeyValuePair<String, Func<Object, Object>>> _propGetterList;

        private readonly Dictionary<String, Func<Object, Object>> _propGetters;
        private readonly SortedList<String, Action<Object, Object?>> _readOnlySetters;

        private readonly ITypeManipulator _types;
        private readonly HashSet<String> _xmlIgnores;
    }
}
