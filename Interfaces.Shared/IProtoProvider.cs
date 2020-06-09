using System;
using System.Reflection;

namespace Das.Serializer.ProtoBuf
{
    public interface IProtoProvider
    {
        IProtoProxy<T> GetProtoProxy<T>(Boolean allowReadOnly = false) 
            where T: class;

        Boolean TryGetProtoField(PropertyInfo prop, Boolean isRequireAttribute, 
            out IProtoFieldAccessor field);

        ProtoFieldAction GetProtoFieldAction(Type pType);

#if DEBUG

        void DumpProxies();

#endif
    }
}