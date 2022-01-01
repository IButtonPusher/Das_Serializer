using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Types
{
   

   public class TypePropertiesAccessor : IEnumerable<IPropertyAccessor>, ITypeAccessor
   {
      public TypePropertiesAccessor(Type type,
                                    IEnumerable<PropertyInfo> properties)
      {
         Type = type;
         _accessors = new Dictionary<string, IPropertyAccessor>();

         foreach (var pi in properties)
         {
            var accessor = PropertyDictionary.GetPropertyAccessor(pi);

            //var getter = pi.CanRead ? getHandler(type, pi) : default;
            //var setter = pi.CanWrite ? TypeManipulator.CreatePropertySetter(pi) : default;

            //var accessor = new SimplePropertyAccessor(type, pi.Name,
            //   getter, setter, pi);
            _accessors[pi.Name] = accessor;
         }
      }

      public IEnumerator<IPropertyAccessor> GetEnumerator()
      {
         foreach (var kvp in _accessors)
            yield return kvp.Value;
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }

      

      public Type Type { get; }

      public IPropertyAccessor this[String propertyName] =>
         _accessors.TryGetValue(propertyName, out var yay)
            ? yay
            : throw new MissingMemberException(propertyName);

      public bool TryGetPropertyAccessor(String propName,
                                         out IPropertyAccessor accessor)
      {
         return _accessors.TryGetValue(propName, out accessor);
      }

      //private static readonly Type[] ParamTypes =
      //{
      //   Const.ObjectType.MakeByRefType(), Const.ObjectType
      //};

      private readonly Dictionary<String, IPropertyAccessor> _accessors;
   }

   public delegate PropertySetter? CreateSetMethodHandler(MemberInfo property);

   public delegate Func<Object, Object> CreatePropertyGetterHandler(Type targetType,
                                                                    PropertyInfo propertyInfo);
}
