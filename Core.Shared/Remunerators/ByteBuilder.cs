using System;
using System.Collections;
using System.Collections.Generic;

namespace Das.Serializer.Remunerators
{
    public class ByteBuilder : IEnumerable<Byte>
    {
        public Byte this[Int32 i]
        {
            // ReSharper disable once UnusedMember.Global
            get => _backingList[i];
            set => _backingList[i] = value;
        }

        private readonly List<Byte> _backingList;

        public Int32 Count => _backingList.Count;

        public ByteBuilder()
        {
            _backingList = new List<Byte>();
        }

        public void Append(IEnumerable<Byte> buffer) => _backingList.AddRange(buffer);


        public void Append(Byte b) => _backingList.Add(b);


        public void Clear() => _backingList.Clear();
        

        public void Append(Byte[] data, Int32 limit)
        {
            for (var c = 0; c < limit; c++)
                _backingList.Add(data[c]);
        }

        public void Append(IEnumerable<Byte> data, Int32 limit)
        {
            foreach (var b in data)
            {
                if (--limit < 0)
                    break;
                Append(b);
            }
        }

        public unsafe void Append(Byte* bytes, Int32 count)
        {
            for (var c = 0; c < count; c++)
                _backingList.Add(bytes[c]);
        }

        public IEnumerator<Byte> GetEnumerator()
        {
            return _backingList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _backingList).GetEnumerator();
        }
    }
}
