using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Serializer;

public abstract class NodeProvider<T> : TypeCore,
                                        IScanNodeProvider<T>
   where T : INode, IEnumerable<T>
{
   protected NodeProvider(INodeTypeProvider typeProvider,
                          ISerializerSettings settings)
      : base(settings)
   {
      TypeProvider = typeProvider;
   }

   public INodeTypeProvider TypeProvider { get; }


   public void Put(T node)
   {
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      if (node == null)
         return;

      var buffer = Buffer;
      var letsAdd = LetsAdd.Value!;

      letsAdd.AddRange(node);

      node.Clear();
      foreach (var n in letsAdd)
      {
         buffer.Enqueue(n);
         if (buffer.Count > 100)
            break;
      }

      letsAdd.Clear();
   }

   protected static Queue<T> Buffer => _buffer.Value!;

   private static readonly ThreadLocal<List<T>> LetsAdd
      = new(() => new List<T>());

   private static readonly ThreadLocal<Queue<T>> _buffer
      = new(() => new Queue<T>());
}