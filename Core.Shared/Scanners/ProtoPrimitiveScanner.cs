using System;
using System.Text;

namespace Das.Serializer.Scanners
{
    internal class ProtoPrimitiveScanner : BinaryPrimitiveScanner
    {
        public ProtoPrimitiveScanner(ISerializationCore dynamicFacade, ISerializerSettings settings) 
            : base(dynamicFacade, settings)
        {
            
        }

        public sealed override String GetString(Byte[] tempByte)
        {
            return Encoding.UTF8.GetString(tempByte);
        }
    }
}
