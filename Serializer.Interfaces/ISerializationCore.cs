using System;

namespace Das.Serializer
{
    public interface ISerializationCore : IDynamicFacade,
        ITypeInferrer, ITypeManipulator, IInstantiator,
        IObjectManipulator
    {
    }
}