using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public class BinaryState : BaseState, IBinaryState
{
   internal BinaryState(ISerializationCore stateProvider,
                        ISerializerSettings settings,
                        //Func<IBinaryState, BinaryScanner> getScanner,
                        //Func<BinaryScanner> getScanner,
                        BinaryScanner binaryScanner,
                        IBinaryNodeProvider binaryNodeProvider,
                        IBinaryPrimitiveScanner primitiveScanner)
      //Func<ISerializationCore, ISerializerSettings, IBinaryPrimitiveScanner> getPrimitiveScanner)
      : base(stateProvider, settings)
   {
      _settings = settings;
      PrimitiveScanner = primitiveScanner; //getPrimitiveScanner(stateProvider, settings);
      _nodeProvider = binaryNodeProvider;
      //_nodeProvider = stateProvider.ScanNodeProvider as IBinaryNodeProvider
      //                ?? throw new InvalidCastException(stateProvider.ScanNodeProvider.GetType().Name);

      //_scanner = getScanner(this);
      _scanner = binaryScanner;
      //Scanner = _scanner;
   }


   public IBinaryScanner Scanner => _scanner;

   public IBinaryPrimitiveScanner PrimitiveScanner { get; }

   IBinaryNodeProvider IBinaryContext.ScanNodeProvider => _nodeProvider;

   public override void Dispose()
   {
      Scanner.Invalidate();
   }

   //public override IScanNodeProvider ScanNodeProvider => _nodeProvider;

   public override ISerializerSettings Settings
   {
      get => _settings;
      set
      {
         _settings = value;
         _scanner.Settings = value;
      }
   }

   private readonly IBinaryNodeProvider _nodeProvider;

   private readonly BinaryScanner _scanner;
   //private ISerializerSettings _settings;
}