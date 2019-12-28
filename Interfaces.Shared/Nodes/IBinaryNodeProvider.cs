using System;

namespace Das.Serializer
{
    public interface IBinaryNodeProvider : IScanNodeProvider<IBinaryNode>
    {
        INodeSealer<IBinaryNode> Sealer { get; }

        IBinaryNode Get(String name, [NotNull]IBinaryNode parent, Type type);


        void ResolveCircularReference(IBinaryNode node, ref Byte distanceFromParent);
    }
}