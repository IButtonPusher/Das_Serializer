using System;
using System.Threading.Tasks;
using Das.Serializer.Types;

namespace Das.Serializer.CodeGen
{
    public interface IProxyProvider
    {
        ISerializerTypeProxy<TType> GetJsonProxy<TType>(ISerializerSettings settings);
    }
}
