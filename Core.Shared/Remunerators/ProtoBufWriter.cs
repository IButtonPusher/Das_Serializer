using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Das.Serializer.Remunerators
{
    public class ProtoBufWriter : BinaryWriterBase<ProtoBufWriter>, IProtoWriter
    {
        private readonly List<Int32> _objectStarts; 
        private readonly Stack<Int32> _objectStartStack; 
     
        private readonly Byte[] _sizeBuffer;
        private Byte[] _array;
        private readonly Dictionary<Int32, Int32> _objects; 
        private readonly Dictionary<Int32, Int32> _parents; 
        private Int32 _size;
        private Int32 _head;
        private Int32 _bufferTail;
        protected Stream _outStream;
        protected Int32 _stackDepth;
        private Int32 _capacity;
        private Int32 _nextResize;


        public new Stream OutStream
        {
            get => _outStream;
            set
            {
                _outStream = value;
                base.OutStream = value;

                _stackDepth = 0;
                _head = 0;
                
                _bufferTail = 0;
                _size = 0;
                _stackDepth = 0;
                _objects.Clear();
                _parents.Clear();
                _objectStarts.Clear();
                _objectStartStack.Clear();
            }
        }

        private static readonly Byte[] _negative32Fill = { Byte.MaxValue, Byte.MaxValue, 
            Byte.MaxValue, Byte.MaxValue, 1};

        public ProtoBufWriter(Int32 startSize)
        {
            _outStream = base.OutStream;
            _capacity = startSize;
            _nextResize = startSize / 2;
            _array = new Byte[startSize];
            _sizeBuffer = new Byte[startSize];
            _objects = new Dictionary<Int32, Int32>();
            _parents = new Dictionary<Int32, Int32>();

            _objectStartStack = new Stack<Int32>();
            _objectStarts = new List<Int32>();

        }

        public IProtoWriter Push()
        {
            _stackDepth++;

            _parents[_size] = 0;

            _objectStartStack.Push(_size);
            _objectStarts.Add(_size);

            return this;
        }

        public override IBinaryWriter Pop()
        {
            var started = _objectStartStack.Pop();

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
            
            var last = 0;

            for (var c = 0; c < _objectStarts.Count; c++)
            {
                var next = _objectStarts[c];
                if (next != last)
                    _outStream.Write(_array, last, next - last);

                var f = _parents[next] ;

                _outStream.Write(_sizeBuffer, f, _objects[next] - f);
                last = next;
            }

            if (last < _size)
                _outStream.Write(_array, last, _size - last);
        }


        [MethodImpl(256)]
        public override void WriteInt8(Byte value)
        {
            if (_stackDepth > 0)
            {
                if (_size >= _capacity)
                {
                    var capacity = (Int32) (_capacity * 200L / 100L);
                    if (capacity < _capacity + 4)
                        capacity = _capacity + 4;
                    SetCapacity(capacity);
                }

                _array[_size++] = value;
            }
            else
                _outStream.WriteByte(value);
        }

        private void UpdateSize()
        {
            var capacity = (Int32) (_capacity * 400L / 100L);
            if (capacity < _capacity + 4)
                capacity = _capacity + 4;
            SetCapacity(capacity);
        }

        public void UnsafeStackByte(Byte value) 
            =>_array[_size++] = value;


        [MethodImpl(256)]
        public sealed override void Append(Byte[] data)
        {
            if (_stackDepth == 0)
                _outStream.Write(data, 0 , data.Length);
            else
            {


                var l = data.Length;

                if (_size + l >= _capacity)
                {
                    var capacity = (Int32) (_capacity * 200L / 100L);
                    if (capacity < _capacity + 4)
                        capacity = _capacity + 4;
                    SetCapacity(capacity);
                }


                Buffer.BlockCopy(data, 0, _array, _size, l);

                _size += l;
            }
        }


        private void SetCapacity(Int32 capacity)
        {
            var objArray = new Byte[capacity];
            if (_size > 0)
            {
                if (_head < _size)
                {
                    Array.Copy(_array, _head, objArray, 0, _size);
                }
                else
                {
                    Array.Copy(_array, _head, objArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, objArray, _array.Length - _head, _size);
                }
            }
            _array = objArray;
            _capacity = capacity;
            _nextResize = capacity / 2;
            _head = 0;
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
            if (_stackDepth > 0)
            {
                if (_size > _nextResize)
                    UpdateSize();

                if (value > 0)
                {
                    if (value > 127)
                    {
                        if (value > 16256)
                        {
                            if (value > 2080768)
                            {

                            }
                            _array[_size++] = (Byte) ((value & 127) | 128);
                            _array[_size++] = (Byte) ((value & 16256) >> 7 | 128);
                            _array[_size++] = (Byte) ((value & 1040384) >> 14);

                            return;
                        }

                        _array[_size++] =(Byte) ((value & 127) | 128);
                        _array[_size++] =  (Byte) ((value & 16256) >> 7);

                        return;
                    }

                    _array[_size++] = (Byte) (value & 127);

                    return;
                }
            }
            else
            {
                if (value > 0)
                {
                    if (value > 127)
                    {
                        if (value > 16256)
                        {
                            if (value > 1040384)
                            {
                                if (value > 66584576)
                                {
                                    if (value > 2130706432)
                                    {
                                        _outStream.WriteByte((Byte) ((value & 127) | 128));
                                        _outStream.WriteByte((Byte) ((value & 16256) >> 7 | 128));
                                        _outStream.WriteByte((Byte) ((value & 1040384) >> 13 | 128));
                                        _outStream.WriteByte((Byte) ((value & 66584576) >> 19 | 128));
                                        _outStream.WriteByte((Byte) ((value & 1879048192) >> 28));

                                        return;
                                    }

                                    _outStream.WriteByte((Byte) ((value & 127) | 128));
                                    _outStream.WriteByte((Byte) ((value & 16256) >> 7 | 128));
                                    _outStream.WriteByte((Byte) ((value & 1040384) >> 13 | 128));
                                    _outStream.WriteByte((Byte) ((value & 66584576) >> 19 | 128));
                                    _outStream.WriteByte((Byte) ((value & 4261412864) >> 26));
                                    

                                    return;
                                }

                                _outStream.WriteByte((Byte) ((value & 127) | 128));
                                _outStream.WriteByte((Byte) ((value & 16256) >> 7 | 128));
                                _outStream.WriteByte((Byte) ((value & 1040384) >> 13 | 128));
                                _outStream.WriteByte((Byte) ((value & 66584576) >> 20));

                                return;
                            }

                            _outStream.WriteByte((Byte) ((value & 127) | 128));
                            _outStream.WriteByte((Byte) ((value & 16256) >> 7 | 128));
                            _outStream.WriteByte((Byte) ((value & 1040384) >> 14));

                            return;
                        }

                        _outStream.WriteByte((Byte) ((value & 127) | 128));
                        _outStream.WriteByte((Byte) ((value & 16256) >> 7));

                        return;
                    }

                    _outStream.WriteByte((Byte) (value & 127));

                    return;
                }
            }


            if (value >= 0) 
                return;
            for (var c = 0; c <= 4; c++)
            {
                var current = (Byte)(value | 128);
                value >>= 7;
                WriteInt8(current);
            }
            Write(_negative32Fill, 0, 5);

        }

        [MethodImpl(256)]
        public sealed override void Write(Byte[] vals)
            => Append(vals);


        public void Write(Byte[] buffer, Int32 count) => Write(buffer, 0, count);

        public override void Write(Byte[] buffer, Int32 index, Int32 count)
        {
            if (_stackDepth == 0)
            {
                _outStream.Write(buffer, index, count);
            }
            else
            {
                Buffer.BlockCopy(buffer, index, _array, _size, count);
                _size += count;
            }
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

        protected override ProtoBufWriter GetChildWriter(IPrintNode node, 
            IBinaryWriter parent,
            Int32 index)
            => Push() as ProtoBufWriter;

      
        
    }
}
