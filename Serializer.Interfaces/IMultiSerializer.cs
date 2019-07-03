using System;

// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public interface IMultiSerializer : IJsonSerializer, IBinarySerializer, 
        IXmlSerializer, ISerializationState
    {
        void SetTypeSurrogate(Type looksLike, Type isReally);

        Boolean TryDeleteSurrogate(Type lookedLike, Type wasReally);
       
    }
}
