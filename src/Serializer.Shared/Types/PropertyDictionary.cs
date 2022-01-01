using System;
using System.Collections.Concurrent;
using System.Reflection;
using Das.Serializer.Properties;
using Reflection.Common;

namespace Das.Serializer.Types
{
    public static class PropertyDictionary<TObject, TProperty>
    {
        public static ConcurrentDictionary<String, IPropertyAccessor<TObject, TProperty>> Properties { get; }
            = new ();
    }

    public static class PropertyDictionary<TObject>
    {
       public static ConcurrentDictionary<String, IPropertyAccessor<TObject>> Properties { get; }
          = new ();
    }

    public static class PropertyDictionary
    {

       private static readonly ConcurrentDictionary<PropertyInfo, SimplePropertyAccessor> _accessors = new();


       public static Object? GetPropertyValue(Object obj,
                                          String propertyName)
       {
          var accessor = GetPropertyAccessor(obj, propertyName);
          return accessor.GetPropertyValue(obj);
       }

       public static SimplePropertyAccessor GetPropertyAccessor(Object obj,
                                                                String propertyName)
       {
          var propInfo = obj.GetType().GetPropertyOrDie(propertyName);
          var accessor = GetPropertyAccessor(propInfo);
          return accessor;
       }

       public static SimplePropertyAccessor GetPropertyAccessor(PropertyInfo propInfo)
       {
          return _accessors.GetOrAdd(propInfo, BuildAccessor);
       }

       private static SimplePropertyAccessor BuildAccessor(PropertyInfo pi)
       {
          var getter = pi.CanRead ? TypeManipulator.CreateDynamicPropertyGetter(pi) : default;
          var setter = pi.CanWrite ? TypeManipulator.CreatePropertySetter(pi) : default;

          var accessor = new SimplePropertyAccessor(pi.DeclaringType!, pi.Name,
             getter, setter, pi);
          return accessor;
       }
    }

}
