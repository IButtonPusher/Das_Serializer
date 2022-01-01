using System;
using System.Threading.Tasks;

namespace Das.Serializer.Types
{
   public interface ITypeAccessor<T>
   {
      IPropertyAccessor<T> this[String propertyName] { get; }

      Boolean TryGetPropertyAccessor(String propName,
                                     out IPropertyAccessor<T> accessor);
   }

   public interface ITypeAccessor
   {
      Type Type { get; }

      IPropertyAccessor this[String propertyName] { get; }

      Boolean TryGetPropertyAccessor(String propName,
                                     out IPropertyAccessor accessor);
   }
}
