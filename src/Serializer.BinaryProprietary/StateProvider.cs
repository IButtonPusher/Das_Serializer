using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Das.Serializer.State;

namespace Das.Serializer;

public class StateProvider : SerializerCore,
                             IStateProvider
{
   public StateProvider(ISerializationCore dynamicFacade,
                        IBinaryContext binaryContext,
                        ISerializerSettings settings)
      : base(dynamicFacade, settings)
   {
      BinaryContext = binaryContext;
      ObjectConverter = new ObjectConverter(this, settings);
   }

   public IObjectConverter ObjectConverter { get; }

   public IBinaryLoaner BorrowBinary(ISerializerSettings settings)
   {
      var buffer = BinaryBuffer;
      var state = buffer.Count > 0
         ? buffer.Dequeue()
         : GetNewBinaryBorrowable(settings);
      state.UpdateSettings(settings);
      return state;
   }

   private BinaryBorrawable GetNewBinaryBorrowable(ISerializerSettings settings) =>
      new(ReturnToLibrary, settings, this, GetNewBinaryScanner(),
         BinaryContext.ScanNodeProvider,
         GetNewBinaryPrimitiveScanner());

   private BinaryPrimitiveScanner GetNewBinaryPrimitiveScanner() => new(BinaryContext, _settings);

   private BinaryScanner GetNewBinaryScanner() =>
      new(BinaryContext, Settings, TypeManipulator,
         ObjectManipulator, ObjectInstantiator);

   private static void ReturnToLibrary(IBinaryLoaner loaned)
   {
      BinaryBuffer.Enqueue(loaned);
   }

   public IBinaryContext BinaryContext { get; }

   private static Queue<IBinaryLoaner> BinaryBuffer => _binaryBuffer.Value!;

   protected static readonly ThreadLocal<Queue<IBinaryLoaner>> _binaryBuffer
      = new(() => new Queue<IBinaryLoaner>());
}