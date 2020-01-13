using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Das.Serializer.Remunerators
{
    public class ProtoBufWriter2: BinaryWriterBase<ProtoBufWriter2>, IProtoWriter
    {
        private readonly List<Int32> _objectStarts; 
        private readonly Stack<Int32> _objectStartStack; 
     
        private readonly Byte[] _sizeBuffer;
        private Byte[] _array;
        private readonly Dictionary<Int32, Int32> _objects; 
        private readonly Dictionary<Int32, Int32> _parents; 
        private Int32 _size;
        private Int32 _head;
        private Int32 _tail;
        private Int32 _bufferTail;
        private Stream _outStream;

        public new Stream OutStream
        {
            get => _outStream;
            set
            {
                _outStream = value;
                _head = 0;
                _tail = 0;
                _bufferTail = 0;
                _size = 0;
                _objects.Clear();
                _parents.Clear();
                _objectStarts.Clear();
                _objectStartStack.Clear();
                base.OutStream = value;
            }
        }

        private static readonly Byte[] _negative32Fill = { Byte.MaxValue, Byte.MaxValue, 
            Byte.MaxValue, Byte.MaxValue, 1};

        public ProtoBufWriter2(Int32 startSize)
        {
            _outStream = base.OutStream;
            
            _array = new Byte[startSize];
            _sizeBuffer = new Byte[startSize];
            _objects = new Dictionary<Int32, Int32>();
            _parents = new Dictionary<Int32, Int32>();

            _objectStartStack = new Stack<Int32>();
            _objectStarts = new List<Int32>();

        }

        public IProtoWriter Push()
        {
            _parents[_size] = 0;

            _objectStartStack.Push(_size);
            _objectStarts.Add(_size);

            return this;
        }

        public override IBinaryWriter Pop()
        {
            var started = _objectStartStack.Pop();
            _objects[started] = _size;

            ///////////////////////////
            var value = _size - started + _parents[started];
            var cnt = 0;
            do
            {
                var current = (Byte) (value & 127);
                value >>= 7;
                if (value > 0)
                    current += 128; //8th bit to specify more bytes remain
                
                _sizeBuffer[_bufferTail] = current;
                cnt++;
            } while (value > 0);

            _parents[started] = _bufferTail;
            _bufferTail += cnt;
            _objects[started] = _bufferTail;

            ////////////////////////

            if (_objectStartStack.Count > 0)
                _parents[_objectStartStack.Peek()] += cnt;

            return this;
        }

        public override void Flush()
        {
            base.Flush();
            var first = (Int32)_outStream.Position;
            var last = 0;

            for (var c = 0; c < _objectStarts.Count; c++)
            {
                var next = _objectStarts[c];
                if (next != last)
                    _outStream.Write(_array, last + first, next - last);

                _outStream.Write(_sizeBuffer, _parents[next] + first, _objects[next] - _parents[next]);
                last = next;
            }

            if (last < _size)
                _outStream.Write(_array, last + first, _size - last);

            _head = _tail = 0;
             _size = 0;
        }


        [MethodImpl(256)]
        public override void WriteInt8(Byte value)
        {
            if (_size == _array.Length)
            {
                var capacity = (Int32) (_array.Length * 200L / 100L);
                if (capacity < _array.Length + 4)
                    capacity = _array.Length + 4;
                SetCapacity(capacity);
            }

            _array[_tail] = value;

            _tail++;
            ++_size;
        }


        [MethodImpl(256)]
        public sealed override void Append(Byte[] data)
        {
            var l = data.Length;

            var al = _array.Length;

            if (_size + l >= al)
            {
                var capacity = (Int32) (al * 200L / 100L);
                if (capacity < al + 4)
                    capacity = al + 4;
                SetCapacity(capacity);
            }


            Buffer.BlockCopy(data, 0, _array, _tail, l);

            _tail += l;
            _size += l;
        }


        private void SetCapacity(Int32 capacity)
        {
            var objArray = new Byte[capacity];
            if (_size > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, objArray, 0, _size);
                }
                else
                {
                    Array.Copy(_array, _head, objArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, objArray, _array.Length - _head, _tail);
                }
            }
            _array = objArray;
            _head = 0;
            _tail = _size == capacity ? 0 : _size;
        }

        public sealed override void WriteInt8(SByte value)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt16(Int16 val)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt16(UInt16 val)
        {
            throw new NotImplementedException();
        }

       

        public sealed override void WriteInt32(Int32 value)
        {
            if (value >= 0)
            {
                do
                {
                    var current = (Byte) (value & 127);
                    value >>= 7;
                    if (value > 0)
                        current += 128; //8th bit to specify more bytes remain
                    WriteInt8(current);
                } while (value > 0);
            }
            else
            {
                for (var c = 0; c <= 4; c++)
                {
                    
                    var current = (Byte)(value | 128);
                    value >>= 7;
                    WriteInt8(current);
                }
                Write(_negative32Fill, 0, 5);
            }
        }

        [MethodImpl(256)]
        public sealed override void Write(Byte[] vals)
            => Append(vals);

        public override void Write(Byte[] buffer, Int32 index, Int32 count)
        {
            Buffer.BlockCopy(buffer, index, _array, _tail, count);

            _tail += count;
            _size += count;
        }
                

        public sealed override void WriteInt32(Int64 val)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt64(Int64 value)
        {
            if (value >= 0)
            {
                do
                {
                    var current = (Byte) (value & 127);
                    value >>= 7;
                    if (value > 0)
                        current += 128; //8th bit to specify more bytes remain
                    WriteInt8(current);
                } while (value > 0);
            }
            else
            {
                for (var c = 0; c <= 4; c++)
                {
                    
                    var current = (Byte)(value | 128);
                    value >>= 7;
                    WriteInt8(current);
                }
                Write(_negative32Fill, 0, 5);
            }
        }

        public sealed override void WriteInt64(UInt64 val)
        {
            throw new NotImplementedException();
        }

        protected override ProtoBufWriter2 GetChildWriter(IPrintNode node, 
            IBinaryWriter parent,
            Int32 index)
            => Push() as ProtoBufWriter2;

      
        
    }
}
