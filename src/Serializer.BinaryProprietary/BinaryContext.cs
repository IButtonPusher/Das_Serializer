using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public class BinaryContext : SerializerCore,
                             IBinaryContext
{
   public BinaryContext(ISerializationCore dynamicFacade,
                        ISerializerSettings settings,
                        IBinaryNodeProvider binaryNodeProvider)
      : base(dynamicFacade, settings)
   {
      _nodeProvider = binaryNodeProvider; //new BinaryNodeProvider(dynamicFacade, settings);
      PrimitiveScanner = new BinaryPrimitiveScanner(dynamicFacade, settings);
   }

   IBinaryNodeProvider IBinaryContext.ScanNodeProvider => _nodeProvider;

   public IBinaryPrimitiveScanner PrimitiveScanner { get; }

   //public override IScanNodeProvider ScanNodeProvider => _nodeProvider;

   private readonly IBinaryNodeProvider _nodeProvider;
}