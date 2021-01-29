using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    public class CoreProtoProvider : IProtoProvider
    {
        public IProtoProxy<T> GetProtoProxy<T>(Boolean allowReadOnly = false)
        {
            throw new NotImplementedException();
        }

        public IProtoProxy<T> GetAutoProtoProxy<T>(Boolean allowReadOnly = false)
        {
            throw new NotImplementedException();
        }

        public bool TryGetProtoField(PropertyInfo prop,
                                     Boolean isRequireAttribute,
                                     out IProtoFieldAccessor field)
        {
            throw new NotImplementedException();
        }


        public ProtoFieldAction GetProtoFieldAction(Type pType)
        {
            throw new NotImplementedException();
        }

        public T BuildDefaultValue<T>()
        {
            throw new NotImplementedException();
        }

        public void DumpProxies()
        {
            throw new NotSupportedException();
        }
    }
}
