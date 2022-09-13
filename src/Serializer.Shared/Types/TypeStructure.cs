﻿using System;
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

         _fieldSetters = new ConcurrentDictionary<string, Action<object, object?>?>();

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

         Properties = new IPropertyAccessor[_propertyAccessors.Count];
         var current = 0;
         foreach (var kvp in _propertyAccessors)
         {
            Properties[current++] = kvp.Value;
         }

         foreach (var meth in type.GetMethods())
         {
            if (!meth.IsDefined(typeof(OnDeserializedAttribute), false))
               continue;

            _onDeserializedMethodName = meth.Name;
            break;
         }

         PropertyCount = _propertyAccessors.Count;
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
      }

      public Boolean OnDeserialized(Object obj,
                                    IObjectManipulator objectManipulator)
      {
         if (_onDeserializedMethodName == null)
            return false;


         objectManipulator.Method(obj, _onDeserializedMethodName, _onDeserializedArgs);
         return true;
      }


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
      }

      public Boolean SetFieldValue(String fieldName,
                                   Object targetObj,
                                   Object fieldVal)
      {
         var set = GetFieldSetter(fieldName);
         if (set == null)
            return false;

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
        
         return true;
      }

      public Boolean TrySetPropertyValue(String propName,
                                         PropertyNameFormat format,
                                         ref Object targetObj,
                                         Object? propVal)
      {
         if (!TryGetPropertyAccessor(propName, format, out var accessor) ||
             !accessor.CanWrite)
            return false;

         accessor.SetPropertyValue(ref targetObj, propVal);
         return true;
      }

      public Boolean TryGetAttribute<TAttribute>(String memberName,
                                                 PropertyNameFormat format,
                                                 out TAttribute value)
         where TAttribute : Attribute
      {
         if (TryGetPropertyAccessor(memberName, format, out var accessor))
            return accessor.TryGetAttribute(out value);


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

    

      public Boolean TryGetPropertyAccessor(String propName,
                                            out IPropertyAccessor accessor)
      {
         return _propertyAccessors.TryGetValue(propName, out accessor);
      }

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


    
      private static readonly StreamingContext _streamingContext = new(StreamingContextStates.Persistence);
      private static readonly Object[] _onDeserializedArgs = {_streamingContext};

      private readonly ConcurrentDictionary<String, Action<Object, Object?>?> _fieldSetters;
      private readonly String? _onDeserializedMethodName;
      private readonly Dictionary<String, IPropertyAccessor> _propertyAccessors;

      private readonly ITypeManipulator _types;
   }
}
