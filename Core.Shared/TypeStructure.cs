using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Das.Serializer;
using Serializer.Core;
using Das.Serializer.Objects;

namespace Das
{
    public class TypeStructure : TypeCore, ITypeStructure
    {
        public SerializationDepth Depth { get; }

        private readonly ITypeManipulator _types;
        private readonly HashSet<String> _xmlIgnores;

        private readonly Dictionary<String, Func<Object, Object>> _getOnly;
        private readonly Dictionary<String, Func<Object, Object>> _propGetters;

        private readonly Dictionary<String, Func<Object, Object>> _fieldGetters;

        private readonly Dictionary<String, Func<Object, Object>> _getDontSerialize;

        private readonly SortedList<String, PropertySetter> _propertySetters;
        private readonly SortedList<String, Action<Object, Object>> _readOnlySetters;
        private readonly SortedList<String, Action<Object, Object>> _fieldSetters;

        public ConcurrentDictionary<String, MemberInfo> MemberTypes { get; }

        private readonly String _onDeserializedMethodName;


        public Int32 PropertyCount { get; }

        public TypeStructure(Type type, Boolean isPropertyNamesCaseSensitive,
            ISerializationDepth depth, ITypeManipulator state)
            : base(state.Settings)
        {
            _xmlIgnores = new HashSet<String>();

            Depth = depth.SerializationDepth;
            _types = state;

            if (type.IsDefined(typeof(SerializeAsTypeAttribute), false))
            {
                var serAs = type.GetCustomAttributes(typeof(SerializeAsTypeAttribute
                ), false).First() as SerializeAsTypeAttribute;

                if (serAs?.TargetType != null)
                    type = serAs.TargetType;
            }

            _getOnly = new Dictionary<String, Func<Object, Object>>();
            _propGetters = new Dictionary<String, Func<Object, Object>>();
            _getDontSerialize = new Dictionary<String, Func<Object, Object>>();
            _fieldGetters = new Dictionary<String, Func<Object, Object>>();

            _fieldSetters = new SortedList<String, Action<Object, Object>>();

            var cmp = isPropertyNamesCaseSensitive
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;

            _propertySetters = new SortedList<String, PropertySetter>(cmp);
            _readOnlySetters = new SortedList<String, Action<Object, Object>>(cmp);
            MemberTypes = new ConcurrentDictionary<String, MemberInfo>(cmp);


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

        private void CreatePropertyDelegates(Type type, ISerializationDepth depth)
        {
            foreach (var pi in _types.GetPublicProperties(type))
            {
                if (!pi.CanRead)
                    continue;

                var isSerialize = true;
                var attrs = pi.GetCustomAttributes(true);

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

                MemberTypes.TryAdd(pi.Name, pi);

                var reallyWrite = false;

                if (pi.CanWrite)
                {
                    var dele = _types.CreatePropertyGetter(type, pi);
                    _propGetters.Add(pi.Name, dele);

                    var sm = _types.CreateSetMethod(pi);
                    if (sm != null)
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
        }

        private void CreateFieldDelegates(Type type, ISerializationDepth depth)
        {
            if ((depth.SerializationDepth & SerializationDepth.PrivateFields) !=
                SerializationDepth.PrivateFields)
                return;

            foreach (var fld in type.GetFields(BindingFlags.Public | Const.NonPublic))
            {
                var delGet = _types.CreateFieldGetter(fld);
                _fieldGetters.Add(fld.Name, delGet);

                var delSet = _types.CreateFieldSetter(fld);
                _fieldSetters.Add(fld.Name, delSet);

                MemberTypes.TryAdd(fld.Name, fld);
            }
        }

        private void SetPropertyForDynamicAccess(Type type, PropertyInfo pi)
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

            MemberTypes.TryAdd(pi.Name, pi);
        }
      

        public void OnDeserialized(Object obj, IObjectManipulator objectManipulator)
        {
            if (_onDeserializedMethodName != null)
                objectManipulator.Method(obj, _onDeserializedMethodName, new Object[0]);
        }

        public IEnumerable<NamedValueNode> GetPropertyValues(Object o, ISerializationDepth depth)
        {
            var isRespectXmlIgnoreAttribute = depth.IsRespectXmlIgnore;

            foreach (var kvp in GetValueGetters(depth))
            {
                if (isRespectXmlIgnoreAttribute && _xmlIgnores.Contains(kvp.Key))
                    continue;
                var name = kvp.Key;
                var val = kvp.Value(o);
                var type = _types.InstanceMemberType(MemberTypes[name]);

                yield return new NamedValueNode(name, val, type);
            }
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
            {
                foreach (var kvp in _getOnly)
                    yield return kvp;
            }

            if ((depth.SerializationDepth & SerializationDepth.PrivateFields) == 0)
                yield break;

            foreach (var kvp in _fieldGetters)
                yield return kvp;
        }

        /// <summary>
        /// Returns properties and/or fields depending on specified depth
        /// </summary>
        public IEnumerable<MemberInfo> GetMembersToSerialize(ISerializationDepth depth)
        {
            foreach (var kvp in GetValueGetters(depth))
                yield return MemberTypes[kvp.Key];
        }

        public NamedValueNode GetPropertyValue(Object o, String propertyName)
        {
            if (!MemberTypes.TryGetValue(propertyName, out var mInfo))
                return null;
            var pType = _types.InstanceMemberType(mInfo);

            if (_propGetters.TryGetValue(propertyName, out var getter))
                return new NamedValueNode(propertyName, getter(o), pType);
            if (_getOnly.TryGetValue(propertyName,out var getOnly))
                return new NamedValueNode(propertyName, getOnly(o), pType);
            if (_getDontSerialize.TryGetValue(propertyName, out var notSerialized))
                return new NamedValueNode(propertyName, notSerialized(o), pType);

            return null;
        }

        public Boolean SetFieldValue(String fieldName, Object targetObj, Object fieldVal)
        {
            if (!_fieldSetters.TryGetValue(fieldName, out var set))
                return false;
            set(targetObj, fieldVal);
            return true;
        }

        public Boolean SetFieldValue<T>(String fieldName, Object targetObj, Object fieldVal)
        {
            if (!_fieldSetters.TryGetValue(fieldName, out var set))
                return false;
            set(targetObj, Convert.ChangeType(fieldVal, typeof(T)));
            return true;
        }

        public Boolean SetValue(String propName, ref Object targetObj, Object propVal,
            SerializationDepth depth)
        {
            if (_propertySetters.TryGetValue(propName, out var setDel))
            {
                setDel(ref targetObj, propVal);
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
    }
}