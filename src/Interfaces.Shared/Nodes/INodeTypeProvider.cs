using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface INodeTypeProvider : ISettingsUser
{
   NodeTypes GetNodeType(INode node);

   NodeTypes GetNodeType(Type? type);
}