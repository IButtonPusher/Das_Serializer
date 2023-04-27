using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.Properties;

namespace Das.Serializer.Types;

public class TypePropertiesAccessor<T> : ITypeAccessor<T>,
                                         IEnumerable<IPropertyAccessor<T>>
{
   public TypePropertiesAccessor(IEnumerable<PropertyInfo> properties,
                                 CreatePropertyGetterHandler getHandler,
                                 CreateSetMethodHandler setHandler)
   {
      _accessors = new Dictionary<string, IPropertyAccessor<T>>();

      foreach (var pi in properties)
      {
         var getter = pi.CanRead ? getHandler(typeof(T), pi) : default;
         var setter = pi.CanWrite ? setHandler(pi) : default;

         var accessor = new PropertyAccessor<T>(pi.Name,
            getter, setter, pi);
         _accessors[pi.Name] = accessor;
      }
   }

   public IEnumerator<IPropertyAccessor<T>> GetEnumerator()
   {
      foreach (var kvp in _accessors)
         yield return kvp.Value;
   }

   IEnumerator IEnumerable.GetEnumerator()
   {
      return GetEnumerator();
   }

   public IPropertyAccessor<T> this[String propertyName] =>
      _accessors.TryGetValue(propertyName, out var yay)
         ? yay
         : throw new MissingMemberException(propertyName);

   public bool TryGetPropertyAccessor(String propName,
                                      out IPropertyAccessor<T> accessor)
   {
      return _accessors.TryGetValue(propName, out accessor);
   }

   public delegate PropertySetter<T>? CreateSetMethodHandler(MemberInfo property);

   //private static readonly Type[] ParamTypes =
   //{
   //   Const.ObjectType.MakeByRefType(), Const.ObjectType
   //};

   private readonly Dictionary<String, IPropertyAccessor<T>> _accessors;
}