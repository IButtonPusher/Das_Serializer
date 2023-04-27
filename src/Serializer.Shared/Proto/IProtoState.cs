using System;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;

namespace Das.Serializer.Proto;

public interface IProtoState
{
   ProxiedInstanceField GetProxy(Type type);
}