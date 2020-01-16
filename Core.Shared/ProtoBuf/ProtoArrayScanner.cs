using System;

namespace Das.Serializer
{
    public class ProtoArrayScanner : ProtoCollectionStructure, IProtoScanStructure
    {
        private Int32 _index;
        private readonly Int32 _length;
        private readonly Array _object;

        public ProtoArrayScanner(IProtoStructure structure, Int32 length, ITypeCore typeCore) 
            : base(structure, typeCore)
        {
            _length = length;
            _object = Array.CreateInstance(Type, length);
        }

        public sealed override Boolean SetValue(String propName, ref Object targetObj, 
            Object propVal, 
            SerializationDepth depth)
        {
            _object.SetValue(propVal, _index++);
            if (_index == _length)
            {
                //IsRepeating = false;
                targetObj = _object;
                return false;
            }

            return true;

        }

        // public Boolean IsRepeating(ref ProtoWireTypes wireType, ref TypeCode typeCodes, ref Type type)
        // {
        //     throw new NotImplementedException();
        // }
    }
}
