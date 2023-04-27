using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IScanNodeProvider<in TNode> : IScanNodeProvider
   where TNode : INode
{
   void Put(TNode node);
}

public interface IScanNodeProvider
{
   INodeTypeProvider TypeProvider { get; }
}