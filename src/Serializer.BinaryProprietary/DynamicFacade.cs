using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Das.Serializer;

public class DynamicFacade : BaseDynamicFacade
{
   public DynamicFacade(ISerializerSettings settings) 
      : this(settings, new ConcurrentDictionary<Type, Type>())
   {
   }

   public DynamicFacade(ISerializerSettings settings,
                        IDictionary<Type, Type> typeSurrogates) : base(settings, typeSurrogates)
   {
      ScanNodeManipulator = new NodeManipulator(this, settings);
   }

   public override INodeManipulator ScanNodeManipulator { get; }
}