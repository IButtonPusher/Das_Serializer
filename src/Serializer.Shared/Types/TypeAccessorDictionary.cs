using System;

namespace Das.Serializer.Types
{
   public static class TypeAccessorDictionary<T>
   {
      public static ITypeAccessor<T> Accessor = new TypePropertiesAccessor<T>(
         TypeManipulator.GetValidProperties(typeof(T)),
         TypeManipulator.CreatePropertyGetter,
         TypeManipulator.CreateSetMethod<T>);
   }
}
