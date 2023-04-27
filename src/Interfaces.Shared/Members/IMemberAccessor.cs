using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IMemberAccessor : INamedField
{
   /// <summary>
   /// if false, do not try to make a deep copy of the object!
   /// </summary>
   Boolean IsMemberSerializable { get; }

   Object? GetValue(Object obj);

   Boolean TrySetValue(ref Object targetObj,
                       Object? propVal);

   //Boolean TryGetPropertyValue(Object obj,
   //                            out Object result);

   Boolean IsValidForSerialization(SerializationDepth depth);
}