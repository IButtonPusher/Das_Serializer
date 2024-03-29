﻿using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Properties;

public class PropertyAccessor<T> : PropertyAccessorBase,
                                   IPropertyAccessor<T>
{
   public PropertyAccessor(String propertyName,
                           Func<object, object>? getter,
                           PropertySetter<T>? setter,
                           PropertyInfo propInfo)
      : base(typeof(T), propertyName, getter, propInfo)
   {
      _setter = setter;
      CanWrite = setter != null;

           
   }

   public Boolean SetPropertyValue(ref T targetObj,
                                   Object? propVal)
   {
      if (_setter == null)
         return false;
      _setter(ref targetObj!, propVal);
      return true;
   }

  

   public bool TrySetValue(ref Object targetObj,
                           Object? propVal) =>
      SetPropertyValue(ref targetObj, propVal);

   public override Boolean CanWrite { get; }

   public bool SetPropertyValue(ref Object targetObj,
                                Object? propVal)
   {
      if (!(targetObj is T valid))
         return false;

      return SetPropertyValue(ref valid, propVal);
   }

   private readonly PropertySetter<T>? _setter;


        
}