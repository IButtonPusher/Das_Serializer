using System;

namespace Das.Serializer;

public interface ITypeStructure<T> : ITypeStructure
{
   new IPropertyAccessor<T>[] Properties { get; }

}