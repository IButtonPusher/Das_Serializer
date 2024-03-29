﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IBinaryNode : INode<IBinaryNode>, IEnumerable<IBinaryNode>
{
   Int32 BlockSize { get; set; }

   Int32 BlockStart { get; set; }

   IList<IBinaryNode> PendingReferences { get; }
}