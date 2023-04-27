using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IPropertyAccessor : IPropertyBase,
                                     IMemberAccessor
{
   Object? GetPropertyValue(Object obj);

   Boolean SetPropertyValue(ref Object targetObj,
                            Object? propVal);

   Boolean TryGetPropertyValue(Object obj,
                               out Object result);

   //Boolean IsValidForSerialization(SerializationDepth depth);
}