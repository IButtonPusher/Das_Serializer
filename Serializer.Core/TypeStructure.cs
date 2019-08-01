using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Das.Serializer;
using Serializer.Core;
using Das.CoreExtensions;
using Das.Serializer.Objects;
using Serializer;

namespace Das
{
    public class TypeStructure : TypeCore, ITypeStructure
    {
        public SerializationDepth Depth { get; }

        private readonly ITypeManipulator _types;

        private readonly Dictionary<String, Func<Object, Object>> _getOnly;
        private readonly Dictionary<String, Func<Object, Object>> _propGetters;

        private readonly Dictionary<String, Func<Object, Object>> _fieldGetters;

        private readonly Dictionary<String, Func<Object, Object>> _getDontSerialize;

        private readonly SortedList<String, PropertySetter> _propertySetters;
        private readonly SortedList<String, Action<Object, Object>> _readOnlySetters;
        private readonly SortedList<String, Action<Object, Object>> _fieldSetters;

        public ConcurrentDictionary<String, MemberInfo> MemberTypes { get; private set; }

        private readonly String _onDeserializedMethodName;


        public Int32 PropertyCount { get; private set; }

        public TypeStructure(Type type, Boolean isPropertyNamesCaseSensitive,
            SerializationDepth depth, ITypeManipulator state)
            : base(state.Settings)
        {
            Depth = depth;
            _types = state;

            if (type.IsDefined(typeof(SerializeAsTypeAttribute), false))
            {
                var serAs = type.GetCustomAttributes(typeof(SerializeAsTypeAttribute
                ), false).First() as SerializeAsTypeAttribute;
                if (serAs?.TargetType != null)
                    type = serAs.TargetType;
            }

            _getOnly = new Dictionary<string, Func<Object, Object>>();
            _propGetters = new Dictionary<string, Func<Object, Object>>();
            _getDontSerialize = new Dictionary<string, Func<object, object>>();
            _fieldGetters = new Dictionary<string, Func<object, object>>();

            _fieldSetters = new SortedList<string, Action<object, object>>();

            var cmp = isPropertyNamesCaseSensitive
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;

            _propertySetters = new SortedList<string, PropertySetter>(cmp);
            _readOnlySetters = new SortedList<string, Action<object, object>>(cmp);
            MemberTypes = new ConcurrentDictionary<string, MemberInfo>(cmp);


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

        private void CreatePropertyDelegates(Type type, SerializationDepth depth)
        {
            foreach (var pi in _types.GetPublicProperties(type))
            {
                if (!pi.CanRead)
                    continue;

                if (!IsSerialize(pi))
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

                _getOnly.Add(pi.Name, _types.CreatePropertyGetter(type, pi));
                if ((depth & SerializationDepth.GetOnlyProperties)
                    != SerializationDepth.GetOnlyProperties)
                    continue;

                if (_types.TryCreateReadOnlyPropertySetter(pi, out var del))
                    _readOnlySetters.Add(pi.Name, del);
            }
        }

        private void CreateFieldDelegates(Type type, SerializationDepth depth)
        {
            if (!depth.ContainsFlag(SerializationDepth.PrivateFields))
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

        private static Boolean IsSerialize(PropertyInfo pi)
        {
            var attrs = pi.GetCustomAttributes(true);

            foreach (var attr in attrs)
            {
                if (attr is IgnoreDataMemberAttribute)
                {
                    return false;
                }
            }

            return true;
        }

        public void OnDeserialized(Object obj, IObjectManipulator objectManipulator)
        {
            if (_onDeserializedMethodName != null)
                objectManipulator.Method(obj, _onDeserializedMethodName, new object[0]);
        }

        public IEnumerable<NamedValueNode> GetPropertyValues(Object o, SerializationDepth depth)
        {
            var isSet = false;

            foreach (var propInfo in GetMembersToSerialize(depth))
            {
                var strName = propInfo.Name;
                if (!_propGetters.ContainsKey(strName))
                    continue;

                isSet = true;

                var type = _types.InstanceMemberType(MemberTypes[strName]);

                yield return new NamedValueNode(strName,
                    _propGetters[propInfo.Name](o), type);
            }

            if (!isSet || depth.ContainsFlag(SerializationDepth.GetOnlyProperties))
            {
                foreach (var kvp in _getOnly)
                {
                    var type = _types.InstanceMemberType(MemberTypes[kvp.Key]);

                    yield return new NamedValueNode(kvp.Key, kvp.Value(o), type);
                }
            }

            if (!depth.ContainsFlag(SerializationDepth.PrivateFields))
                yield break;

            foreach (var kvp in _fieldGetters)
            {
                var type = _types.InstanceMemberType(MemberTypes[kvp.Key]);
                yield return new NamedValueNode(kvp.Key, kvp.Value(o), type);
            }
        }

        /// <summary>
        /// Returns properties and/or fields depending on specified depth
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public IEnumerable<MemberInfo> GetMembersToSerialize(SerializationDepth depth)
        {
            var isSet = false;

            foreach (var kvp in _propGetters.OrderBy(p => p.Key))
            {
                isSet = true;
                yield return MemberTypes[kvp.Key];
            }

            if (!isSet || depth.ContainsFlag(SerializationDepth.GetOnlyProperties))
            {
                foreach (var kvp in _getOnly)
                {
                    yield return MemberTypes[kvp.Key];
                }
            }

            if (!depth.ContainsFlag(SerializationDepth.PrivateFields))
                yield break;

            foreach (var kvp in _fieldGetters)
                yield return MemberTypes[kvp.Key];
        }

        public NamedValueNode GetPropertyValue(Object o, String propertyName)
        {
            if (!MemberTypes.TryGetValue(propertyName, out var mInfo))
                return null;
            var pType = _types.InstanceMemberType(mInfo);

            if (_propGetters.ContainsKey(propertyName))
                return new NamedValueNode(propertyName, _propGetters[propertyName](o), pType);
            if (_getOnly.ContainsKey(propertyName))
                return new NamedValueNode(propertyName, _getOnly[propertyName](o), pType);
            if (_getDontSerialize.ContainsKey(propertyName))
                return new NamedValueNode(propertyName, _getDontSerialize[propertyName](o), pType);

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