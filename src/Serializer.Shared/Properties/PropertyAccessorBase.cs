using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Properties;

public abstract class PropertyAccessorBase : PropertyAccessorCore
{
   protected PropertyAccessorBase(Type declaringType,
                                  String propertyName,
                                  Func<object, object>? getter,
                                  PropertyInfo propInfo)
      : base(getter != null, declaringType, propInfo, propertyName)
   {
      _getter = getter;
      _propInfo = propInfo;
   }

   public Object? GetPropertyValue(Object obj)
   {
      if (!CanRead)
         throw new MemberAccessException();


      return _getter!(obj);
   }

   public override string ToString()
   {
      return DeclaringType.Name + "->" + PropertyPath + "( read: "
             + CanRead + " write: " + CanWrite + " )";
   }

   public Boolean TryGetPropertyValue(Object obj,
                                      out Object result)
   {
      if (_getter == null)
      {
         result = default!;
         return false;
      }

      result = _getter(obj);
      return true;
   }

   public object? GetValue(Object obj) => GetPropertyValue(obj);

       
       

   public Boolean IsValidForSerialization(SerializationDepth depth)
   {
      switch (depth)
      {
         case SerializationDepth.AllProperties:
         case SerializationDepth.GetOnlyProperties:
         case SerializationDepth.Full:
            return true;

         case SerializationDepth.GetSetProperties:
            return CanRead && CanWrite;

         default:
            return false;
      }
   }

   private readonly Func<object, object>? _getter;
   protected readonly PropertyInfo _propInfo;
}