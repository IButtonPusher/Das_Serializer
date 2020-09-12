using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Das.Serializer
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class NaiveMemoryStream : Stream
    {
        // ReSharper disable once UnusedMember.Global
        public NaiveMemoryStream()
            : this(0)
        {
        }

        public NaiveMemoryStream(int capacity)
        {
            _buffer = capacity != 0 ? new byte[capacity] : new byte[0];
            _capacity = capacity;
            _expandable = true;
            _writable = true;
            _origin = 0; // Must be 0 for byte[]'s created by MemoryStream
            _isOpen = true;
        }

        public NaiveMemoryStream(byte[] buffer)
            : this(buffer, true)
        {
        }

        public NaiveMemoryStream(byte[] buffer, bool writable)
        {
            _buffer = buffer;
            _length = _capacity = buffer.Length;
            _writable = writable;
            _origin = 0;
            _isOpen = true;
        }

        public NaiveMemoryStream(byte[] buffer, int index, int count)
            : this(buffer, index, count, true)
        {
        }

        public NaiveMemoryStream(byte[] buffer, int index, int count, bool writable)
        {
            _buffer = buffer;
            _origin = _position = index;
            _length = _capacity = index + count;
            _writable = writable;
            _expandable = false;
            _isOpen = true;
        }

        public sealed override bool CanRead => _isOpen;

        public sealed override bool CanSeek => _isOpen;

        public sealed override bool CanWrite => _writable;

        // Gets & sets the capacity (number of bytes allocated) for this stream.
        // The capacity cannot be set to a value less than the current length
        // of the stream.
        // 
        public virtual int Capacity
        {
            get => _capacity - _origin;
            set
            {
                // MemoryStream has this invariant: _origin > 0 => !expandable (see ctors)
                if (_expandable && value != _capacity)
                {
                    if (value > 0)
                    {
                        var newBuffer = new byte[value];
                        if (_length > 0) Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
                        _buffer = newBuffer;
                    }
                    else
                    {
                        _buffer = new byte[0];
                    }

                    _capacity = value;
                }
            }
        }

        public Int32 IntLength => _length - _origin;

        public sealed override long Length => _length - _origin;

        public sealed override long Position
        {
            get => _position - _origin;
            set => _position = _origin + (int) value;
        }


        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _isOpen = false;
                    _writable = false;
                    _expandable = false;
                }
            }
            finally
            {
                // Call base.Close() to cleanup async IO resources
                base.Dispose(disposing);
            }
        }

        // returns a bool saying whether we allocated a new array.
        private bool EnsureCapacity(int value)
        {
            if (value > _capacity)
            {
                var newCapacity = Math.Max(value, 256);

                // We are ok with this overflowing since the next statement will deal
                // with the cases where _capacity*2 overflows.
                if (newCapacity < _capacity * 2) newCapacity = _capacity * 2;

                // We want to expand the array up to 0x7FFFFFC7
                // And we want to give the user the value that they asked for
                if ((uint) (_capacity * 2) > 0x7FFFFFC7) newCapacity = Math.Max(value, 0x7FFFFFC7);

                Capacity = newCapacity;
                return true;
            }

            return false;
        }

        public sealed override void Flush()
        {
        }

        public sealed override int Read(byte[] buffer, int offset, int count)
        {
            var n = _length - _position;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;

            if (n <= 8)
            {
                var byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _buffer[_position + byteCount];
            }
            else
            {
                Buffer.BlockCopy(_buffer, _position, buffer, offset, n);
            }

            _position += n;

            return n;
        }


        public sealed override int ReadByte()
        {
            if (_position >= _length)
                return -1;

            return _buffer[_position++];
        }


        public sealed override long Seek(long offset, SeekOrigin loc)
        {
            switch (loc)
            {
                case SeekOrigin.Begin:
                {
                    var tempPosition = unchecked(_origin + (int) offset);

                    _position = tempPosition;
                    break;
                }
                case SeekOrigin.Current:
                {
                    var tempPosition = unchecked(_position + (int) offset);

                    _position = tempPosition;
                    break;
                }
                case SeekOrigin.End:
                {
                    var tempPosition = unchecked(_length + (int) offset);

                    _position = tempPosition;
                    break;
                }
                default:
                    throw new ArgumentException(nameof(loc));
            }

            return _position;
        }

        // Sets the length of the stream to a given value.  The new
        // value must be nonnegative and less than the space remaining in
        // the array, int.MaxValue - origin
        // Origin is 0 in all cases other than a MemoryStream created on
        // top of an existing array and a specific starting offset was passed 
        // into the MemoryStream constructor.  The upper bounds prevents any 
        // situations where a stream may be created on top of an array then 
        // the stream is made longer than the maximum possible length of the 
        // array (int.MaxValue).
        // 
        public sealed override void SetLength(long value)
        {
            var newLength = _origin + (int) value;
            var allocatedNewArray = EnsureCapacity(newLength);
            if (!allocatedNewArray && newLength > _length)
                Array.Clear(_buffer, _length, newLength - _length);
            _length = newLength;
            if (_position > newLength)
                _position = newLength;
        }

        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            var i = _position + count;


            if (i > _length)
            {
                var mustZero = _position > _length;
                if (i > _capacity)
                {
                    var allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray) mustZero = false;
                }

                if (mustZero) Array.Clear(_buffer, _length, i - _length);
                _length = i;
            }

            if (count <= 8 && buffer != _buffer)
            {
                var byteCount = count;
                while (--byteCount >= 0) _buffer[_position + byteCount] = buffer[offset + byteCount];
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }

            _position = i;
        }

        public sealed override void WriteByte(byte value)
        {
            if (_position >= _length)
            {
                var newLength = _position + 1;
                var mustZero = _position > _length;
                if (newLength >= _capacity)
                {
                    var allocatedNewArray = EnsureCapacity(newLength);
                    if (allocatedNewArray) mustZero = false;
                }

                if (mustZero) Array.Clear(_buffer, _length, _position - _length);
                _length = newLength;
            }

            _buffer[_position++] = value;
        }

        public byte[] _buffer; // Either allocated internally or externally.

        private int _capacity; // length of usable portion of buffer for stream
        // Note that _capacity == _buffer.Length for non-user-provided byte[]'s

        private bool _expandable; // User-provided buffers aren't expandable.
        private bool _isOpen; // Is this stream open or closed?
        private int _length; // Number of bytes within the memory stream
        private readonly int _origin; // For user-provided arrays, start at this origin
        private int _position; // read/write head.
        private bool _writable; // Can user write to this stream?
    }
}