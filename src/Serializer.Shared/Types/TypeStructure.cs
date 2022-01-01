using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Das.Extensions;
using Das.Serializer.Types;

namespace Das.Serializer
{
   public class TypeStructure : ITypeStructure
   {
      public TypeStructure(Type type,
                           ITypeManipulator state,
                           IEnumerable<IPropertyAccessor> propertyAccessors)
      {
         Type = type;

         _types = state;


         if (type.IsDefined(typeof(SerializeAsTypeAttribute), false))
         {
            var serAs = type.GetCustomAttributes(typeof(SerializeAsTypeAttribute), true)
                            .First() as SerializeAsTypeAttribute;

            if (serAs?.TargetType != null)
               type = serAs.TargetType;
         }

         _propertyAccessors = new Dictionary<string, IPropertyAccessor>();
         //_fieldGetters = new Dictionary<String, Func<Object, Object>>();


         _fieldSetters = new ConcurrentDictionary<string, Action<object, object?>?>();

         //var cmp = StringComparer.OrdinalIgnoreCase;

         //_propertySetters = new SortedList<String, PropertySetter>(cmp);
         //_readOnlySetters = new SortedList<String, Action<Object, Object?>>(cmp);
         //MemberTypes = new Dictionary<String, INamedField>(cmp);


         if (_types.IsLeaf(type, true) || _types.IsCollection(type))
         {
            Properties = new IPropertyAccessor[0];
            return;
         }

         foreach (var accessor in propertyAccessors)
         {
            _propertyAccessors[accessor.PropertyPath] = accessor;

            if (accessor.PropertyInfo.TryGetCustomAttribute<DataMemberAttribute>(out var dma) &&
                !string.IsNullOrEmpty(dma.Name))
            {
               _propertyAccessors.Add(dma.Name, accessor);
            }

            
         }

         //CreatePropertyDelegates(type, //depth, 
         //    state);

         Properties = new IPropertyAccessor[_propertyAccessors.Count];
         var current = 0;
         foreach (var kvp in _propertyAccessors)
         {
            Properties[current++] = kvp.Value;
         }

         //CreateFieldDelegates(type); //, depth);

         foreach (var meth in type.GetMethods())
         {
            if (!meth.IsDefined(typeof(OnDeserializedAttribute), false))
               continue;

            _onDeserializedMethodName = meth.Name;
            break;
         }

         PropertyCount = _propertyAccessors.Count;
         //PropertyCount = _propertySetters.Count;
      }

      public Type Type { get; }

      public IPropertyAccessor this[String propertyName] => 
         _propertyAccessors.TryGetValue(propertyName, out var yay)
            ? yay
            : throw new MissingMemberException(propertyName);

      public IPropertyAccessor[] Properties { get; }


      public Int32 PropertyCount { get; }


      public IEnumerable<KeyValuePair<PropertyInfo, object?>> IteratePropertyValues(Object o,
         ISerializationDepth depth)
      {
         var isRespectXmlIgnoreAttribute = depth.IsRespectXmlIgnore;

         foreach (var kvp in _propertyAccessors)
         {
            if (isRespectXmlIgnoreAttribute &&
                kvp.Value.TryGetAttribute<XmlIgnoreAttribute>(out _))
               continue;

            if (!kvp.Value.IsValidForSerialization(depth.SerializationDepth))
               continue;

            yield return new KeyValuePair<PropertyInfo, Object?>(
               kvp.Value.PropertyInfo,
               kvp.Value.GetPropertyValue(o));
         }


         //var cnt = _propGetterList.Count;

         //for (var c = 0; c < cnt; c++)
         //{
         //    var kvp = _propGetterList[c];
         //    if (isRespectXmlIgnoreAttribute && _xmlIgnores.Contains(kvp.Key))
         //        continue;
         //    var name = kvp.Key;
         //    var propInfo = _propertyInfos[name];
         //    var val = kvp.Value(o);

         //    yield return new KeyValuePair<PropertyInfo, Object?>(propInfo, val);
         //}
      }

      public Boolean OnDeserialized(Object obj,
                                    IObjectManipulator objectManipulator)
      {
         if (_onDeserializedMethodName == null)
            return false;


         objectManipulator.Method(obj, _onDeserializedMethodName, _onDeserializedArgs);
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
      ///    Returns properties and/or fields depending on specified depth
      /// </summary>
      public IEnumerable<INamedField> GetMembersToSerialize(SerializationDepth depth)
      {
         foreach (var kvp in _propertyAccessors)
         {
            if (!kvp.Value.IsValidForSerialization(depth))
               continue;

            yield return kvp.Value;
         }

         //foreach (var kvp in GetValueGetters(depth))
         //{
         //    yield return MemberTypes[kvp.Key];
         //}
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
         var set = GetFieldSetter(fieldName);
         if (set == null)
            return false;

         //if (!_fieldSetters.TryGetValue(fieldName, out var set))
         //    return false;
         set(targetObj, fieldVal);
         return true;
      }

      public Boolean SetFieldValue<T>(String fieldName,
                                      Object targetObj,
                                      Object fieldVal)
      {
         var set = GetFieldSetter(fieldName);
         if (set == null)
            return false;
         //if (!_fieldSetters.TryGetValue(fieldName, out var set))
         //    return false;
         //set(targetObj, Convert.ChangeType(fieldVal, typeof(T)));
         return true;
      }

      //public bool TryGetPropertyInfo(String propName,
      //                               PropertyNameFormat format,
      //                               out PropertyInfo propInfo)
      //{
      //    return TODO_IMPLEMENT_ME;
      //}

      public Boolean TrySetPropertyValue(String propName,
                                         PropertyNameFormat format,
                                         ref Object targetObj,
                                         Object? propVal)
         //SerializationDepth depth = SerializationDepth.AllProperties)
      {
         if (!TryGetPropertyAccessor(propName, format, out var accessor) ||
             !accessor.CanWrite)
            return false;

         accessor.SetPropertyValue(ref targetObj, propVal);
         return true;
         //if (!_propertyAccessors.TryGetValue(propName)

         //if (_propertySetters.TryGetValue(propName, out var setDel))
         //{
         //    setDel(ref targetObj!, propVal);
         //    return true;
         //}

         //if (depth <= SerializationDepth.GetSetProperties)
         //    return false;

         //if (_readOnlySetters.TryGetValue(propName, out var set) ||
         //    _fieldSetters.TryGetValue(propName, out set))
         //{
         //    set(targetObj, propVal);
         //    return true;
         //}

         //var pi = targetObj.GetType().GetProperty(propName);
         //if (pi == null)
         //    return false;

         //if (_types.TryCreateReadOnlyPropertySetter(pi, out var pDel))
         //    _readOnlySetters.Add(pi.Name, pDel);
         //else return false;

         //pDel(targetObj, propVal);
         //return true;
      }

      //public void SetPropertyValueUnsafe(String propName,
      //                                   ref Object targetObj,
      //                                   Object propVal)
      //{
      //    _propertySetters[propName](ref targetObj!, propVal);
      //}


      public Boolean TryGetAttribute<TAttribute>(String memberName,
                                                 PropertyNameFormat format,
                                                 out TAttribute value)
         where TAttribute : Attribute
      {
         if (TryGetPropertyAccessor(memberName, format, out var accessor))
            return accessor.TryGetAttribute(out value);


         //if (_propertyAttributes.TryGetValue(memberName, typeof(TAttribute), out var items)
         //    && items is TAttribute attr)
         //{
         //    value = attr;
         //    return true;
         //}

         value = default!;
         return false;
      }

      public bool TryGetPropertyAccessor(String propName,
                                         PropertyNameFormat format,
                                         out IPropertyAccessor accessor)
      {
         if (format != PropertyNameFormat.Default)
            propName = _types.ChangePropertyNameFormat(propName, format);

         return _propertyAccessors.TryGetValue(propName, out accessor);
      }


      public object? GetPropertyValue(Object o,
                                      String propertyName,
                                      PropertyNameFormat format = PropertyNameFormat.Default)
      {
         if (!TryGetPropertyAccessor(propertyName, format, out var accessor))
            return default;

         return accessor.GetPropertyValue(o);
      }

      public TProperty GetPropertyValue<TObject, TProperty>(TObject o,
                                                            String propertyName)
      {
         var accessor = PropertyDictionary<TObject, TProperty>.Properties.GetOrAdd(propertyName,
            n => _types.GetPropertyAccessor<TObject, TProperty>(n));

         return accessor.GetPropertyValue(ref o);
      }

      //public SerializationDepth Depth { get; }

      //public Dictionary<String, INamedField> MemberTypes { get; }

      //[MethodImpl(256)]
      //public bool TryGetPropertyInfo(String propName,
      //                               out PropertyInfo propInfo)
      //{
      //    return _propertyInfos.TryGetValue(propName, out propInfo);
      //}

      public Boolean TryGetPropertyAccessor(String propName,
                                            out IPropertyAccessor accessor)
      {
         return _propertyAccessors.TryGetValue(propName, out accessor);
      }


      //protected PropertyValueIterator<IProperty> PropertyValues
      //    => _propertyValues.Value!;

      //public object? GetPropertyValue(Object o,
      //                                String propertyName)
      //{
      //    return GetPropertyValueImpl(o, propertyName, out _);
      //}


      //public object? GetValue(Object o,
      //                        String propertyName)
      //{
      //    if (_propGetters.TryGetValue(propertyName, out var getter))
      //        return getter(o);
      //    if (_getOnly.TryGetValue(propertyName, out var getOnly))
      //        return getOnly(o);
      //    if (_getDontSerialize.TryGetValue(propertyName, out var notSerialized))
      //        return notSerialized(o);
      //    return null;
      //}

      public override string ToString()
      {
         return GetType().Name + ": " + Type.FullName;
      }

      private Action<Object, Object>? GetFieldSetter(String fieldName)
      {
         return _fieldSetters.GetOrAdd(fieldName, f =>
         {
            var fi = Type.GetField(f);
            if (fi == null)
               return null;

            return _types.CreateFieldSetter(fi);
         });
      }


      //public bool TryGetPropertyValue(Object obj,
      //                                String propertyName,
      //                                out Object result)
      //{
      //    result = GetPropertyValueImpl(obj, propertyName, out _)!;
      //    return result != null;
      //}

      //private void CreateFieldDelegates(Type type)
      //{
      //   foreach (var fld in type.GetFields(BindingFlags.Public | Const.NonPublic))
      //    {
      //        var delGet = _types.CreateFieldGetter(fld);
      //        _fieldGetters[fld.Name] = delGet;

      //        var delSet = _types.CreateFieldSetter(fld);
      //        _fieldSetters[fld.Name] = delSet;

      //        var member = new DasMember(fld.Name, fld.FieldType);
      //        MemberTypes[fld.Name] = member;
      //    }
      //}

      private static readonly StreamingContext _streamingContext = new(StreamingContextStates.Persistence);
      private static readonly Object[] _onDeserializedArgs = {_streamingContext};

      //private Object? GetPropertyValueImpl(Object o,
      //                                     String propertyName,
      //                                     out INamedField? mInfo)
      //{
      //    if (!MemberTypes.TryGetValue(propertyName, out mInfo))
      //        return null;

      //    if (_propGetters.TryGetValue(propertyName, out var getter))
      //        return getter(o);
      //    if (_getOnly.TryGetValue(propertyName, out var getOnly))
      //        return getOnly(o);
      //    if (_getDontSerialize.TryGetValue(propertyName, out var notSerialized))
      //        return notSerialized(o);

      //    mInfo = null;
      //    return null;
      //}

      //private IEnumerable<KeyValuePair<String, Func<Object, Object>>> GetValueGetters(
      //    ISerializationDepth depth)
      //{
      //    var isSet = false;

      //    foreach (var kvp in _propGetters.OrderBy(p => p.Key))
      //    {
      //        isSet = true;
      //        yield return kvp;
      //    }

      //    if (!isSet || (depth.SerializationDepth & SerializationDepth.GetOnlyProperties) != 0)
      //        foreach (var kvp in _getOnly)
      //        {
      //            yield return kvp;
      //        }

      //    if ((depth.SerializationDepth & SerializationDepth.PrivateFields) == 0)
      //        yield break;

      //    foreach (var kvp in _fieldGetters)
      //    {
      //        yield return kvp;
      //    }
      //}

      //private void SetPropertyForDynamicAccess(Type type,
      //                                         PropertyInfo pi)
      //{
      //    //even if a property will be excluded from serialization, we may still want
      //    //to set its value dynamically
      //    var gtor = _types.CreatePropertyGetter(type, pi);
      //    _getDontSerialize.Add(pi.Name, gtor);
      //    if (pi.CanWrite)
      //    {
      //        var sp = _types.CreateSetMethod(pi);
      //        if (sp != null)
      //            _propertySetters.Add(pi.Name, sp);
      //    }

      //    var member = new DasMember(pi.Name, pi.PropertyType);
      //    MemberTypes[pi.Name] = member;
      //}

      //private readonly Dictionary<String, Func<Object, Object>> _fieldGetters;
      private readonly ConcurrentDictionary<String, Action<Object, Object?>?> _fieldSetters;


      private readonly String? _onDeserializedMethodName;

      private readonly Dictionary<String, IPropertyAccessor> _propertyAccessors;
      //private readonly SortedList<String, Action<Object, Object?>> _readOnlySetters;

      private readonly ITypeManipulator _types;
   }
}
