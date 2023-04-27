using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer;

public class RuntimeObject : IRuntimeObject
{
   public RuntimeObject(ITypeManipulator typeManipulator)
   {
      _typeManipulator = typeManipulator;
      _hash = base.GetHashCode();
      //Properties = new Dictionary<String, IRuntimeObject>();
      _properties = new Dictionary<String, Object?>();
      _propertyTypes = new Dictionary<String, Type>();
   }

   //public RuntimeObject(ITypeManipulator typeManipulator,
   //                     Object? primitiveValue)
   //   : this(typeManipulator)
   //{
   //   PrimitiveValue = primitiveValue;
   //   if (primitiveValue is { } notNull)
   //      _hash = notNull.GetHashCode();
   //}

   public Object? this[String key]
   {
      get
      {
         if (_properties.TryGetValue(key, out var res))
            return res;

         return default;
      }
   }

   //public IRuntimeObject? this[String key]
   //{
   //   get
   //   {
   //      if (Properties.TryGetValue(key, out var res))
   //         return res;

   //      return default;
   //   }
   //}

   //public Object? PrimitiveValue { get; set; }

   public IEnumerable<DasProperty> GetProperties()
   {
      foreach (var kvp in _properties)
      {
         var propVal = kvp.Value;
         Type propType;
         if (!ReferenceEquals(null, propVal))
            propType = propVal.GetType();
         else
         {
            if (!_propertyTypes.TryGetValue(kvp.Key, out propType))
               propType = typeof(Object);
         }

         yield return new DasProperty(kvp.Key, propType, propVal);

      }
   }

   //public Type GetObjectType() =>
   //   PrimitiveValue != null
   //      ? PrimitiveValue.GetType()
   //      : Const.ObjectType;

   public void AddPropertyValue(String propName,
                                Object? propVal,
                                Type propertyType)
   {
      _properties.Add(propName, propVal);
      if (ReferenceEquals(null, propVal))
         _propertyTypes.Add(propName, propertyType);
   }

   public void Clear()
   {
      _propertyTypes.Clear();
      _properties.Clear();
   }

   public Boolean TryGetPropertyValue(String propName,
                                      out Object? propVal)
   {
      if (_properties.TryGetValue(propName, out propVal))
         return true;

      return false;
   }

   public override bool Equals(Object? obj)

   {
      switch (obj)
      {
         case null:
            return false;

         case RuntimeObject robj:
            if (robj._properties.Count != _properties.Count)
               return false;

            foreach (var kvp in robj._properties)
            {
               if (!_properties.TryGetValue(kvp.Key, out var myValue))
                  return false;

               if (!Equals(kvp.Value, myValue))
                  return false;
            }

            return true;

         default:
            //if (PrimitiveValue != null)
            //   return Equals(PrimitiveValue, obj);

            var ts = _typeManipulator.GetTypeStructure(obj.GetType());

            if (ts.PropertyCount != _properties.Count)
               return false;

            foreach (var kvp in ts.IteratePropertyValues(obj, DepthConstants.AllProperties))
            {
               if (!_properties.TryGetValue(kvp.Key.Name, out var myValue))
                  return false;

               if (!Equals(kvp.Value, myValue))
                  return false;
            }

            return true;
      }
   }

   public override int GetHashCode() => _hash;

   public override String ToString()
   {
      //if (PrimitiveValue is { } p)
      //   return p.ToString();

      return GetType().Name + " - " + _properties.Count + " properties";
   }

   private readonly Int32 _hash;

   //public Dictionary<String, IRuntimeObject> Properties { get; }

   private readonly Dictionary<String, Object?> _properties;
   private readonly Dictionary<String, Type> _propertyTypes;
   private readonly ITypeManipulator _typeManipulator;
}
