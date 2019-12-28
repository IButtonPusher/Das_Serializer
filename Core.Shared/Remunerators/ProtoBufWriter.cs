using System;
using System.Collections.Generic;
using System.IO;
using Serializer.Core.Remunerators;

namespace Das.Serializer.Remunerators
{
    public class ProtoBufWriter: BinaryWriterBase<ProtoBufWriter>
    {
        private readonly Stack<Int32> _objectMarkersStack;
        private readonly Stack<Int32> _objectSizeStack;
        
        private Int32 _currentObjectStarted;
        private Int32 _stackDepth;

        private Byte[] _array;
        private Int32 _size;
        private Int32 _head;
        private Int32 _tail;

        public new Stream OutStream
        {
            get => base.OutStream;
            set => base.OutStream = value;
        }

        private static readonly Byte[] _negative32Fill = { Byte.MaxValue, Byte.MaxValue, 
            Byte.MaxValue, Byte.MaxValue, 1};

        public ProtoBufWriter(Int32 startSize)
        {
         
            _objectMarkersStack = new Stack<Int32>();
            _objectSizeStack = new Stack<Int32>();
            _array = new Byte[startSize];
        }

        protected ProtoBufWriter(ProtoBufWriter parent) : base(parent)
        {
            
        }

        public ProtoBufWriter GetChildWriter()
        {
            _stackDepth++;
            if (_stackDepth > 1)
                _objectMarkersStack.Push(_currentObjectStarted);

            _currentObjectStarted = _size;
            
            return this;
        }


        public override IBinaryWriter Pop()
        {
            _stackDepth--;

            var nest = _size; //_nestedObjectStack.Count;
            var len = nest - _currentObjectStarted;

            _objectSizeStack.Push(len);

            if (_stackDepth != 0)
                return this;

            var currentStart = 0;
            Int32 currentLength;
            Int32 writeBytes;

            while (_objectMarkersStack.Count > 0)
            {
                currentLength = _objectSizeStack.Pop();
                WriteInt32(currentLength);
                var currentEnd = _objectMarkersStack.Pop();

                writeBytes = currentEnd - currentStart;
                
                OutStream.Write(_array, _head, writeBytes);
                _size -= writeBytes;
                _head += writeBytes;

                currentStart = currentEnd;
            }

            currentLength = _objectSizeStack.Pop();
            WriteInt32(currentLength);
            
            OutStream.Write(_array, _head, _size);

            _head = _tail = 0;
            _size = 0;

            return this;
        }

        public override void WriteInt8(Byte value)
        {
            if (_stackDepth > 0)
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
            else
                OutStream.WriteByte(value);
        }


        public override void Append(Byte[] data)
        {
            if (_stackDepth == 0)
                base.Append(data);
            else
            {
                var l = data.Length;

                if (_size + l >= _array.Length)
                {
                    var capacity = (Int32) (_array.Length * 200L / 100L);
                    if (capacity < _array.Length + 4)
                        capacity = _array.Length + 4;
                    SetCapacity(capacity);
                }

                Buffer.BlockCopy(data, 0, _array, _tail, l);

                _tail += l;
                //_array[_tail] = value;
                //MoveNext(ref _tail);
                _size += l;


//                for (var c = 0; c < data.Length; c++)
//                    _nestedObjectStack.Enqueue(data[c]);
            }
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

       

        public override void Write(Byte[] buffer, Int32 index, Int32 count) 
            => OutStream.Write(buffer, index, count);

        public sealed override void WriteInt32(Int64 val)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt64(Int64 val)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt64(UInt64 val)
        {
            throw new NotImplementedException();
        }

        protected override ProtoBufWriter GetChildWriter(IPrintNode node, 
            IBinaryWriter parent,
            Int32 index)
            => GetChildWriter();

      
        
    }
}
