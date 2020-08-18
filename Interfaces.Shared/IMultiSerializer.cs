using System;
using System.Threading.Tasks;

// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public interface IMultiSerializer : IJsonSerializer, IBinarySerializer,
        IXmlSerializer, ISerializationState
    {
        IStateProvider StateProvider { get; }

        IProtoSerializer GetProtoSerializer<TPropertyAttribute>(
            ProtoBufOptions<TPropertyAttribute> options)
            where TPropertyAttribute : Attribute;

        void SetTypeSurrogate(Type looksLike, Type isReally);

        Boolean TryDeleteSurrogate(Type lookedLike, Type wasReally);
    }
}