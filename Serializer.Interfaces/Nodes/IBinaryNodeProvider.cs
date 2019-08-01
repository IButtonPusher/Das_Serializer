using System;

namespace Das.Serializer
{
    public interface IBinaryNodeProvider : INodeProvider<IBinaryNode>
    {
        INodeSealer<IBinaryNode> Sealer { get; }

        IBinaryNode Get(String name, IBinaryNode parent, Type type);


        void ResolveCircularReference(IBinaryNode node, ref Byte distanceFromParent);
    }
}