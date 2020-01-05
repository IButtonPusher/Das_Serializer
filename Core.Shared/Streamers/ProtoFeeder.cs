// Das.Streamers.ProtoFeeder
using Das.Serializer;
using Das.Streamers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

public class ProtoFeeder : BinaryFeeder, IProtoFeeder
{
	private Stack<Int32> _arrayIndeces;

	private ByteStream _byteStream;
    private ByteArray _byteArray;

	[ThreadStatic]
	private static Int32 _currentByte;

	[ThreadStatic]
	private static Int32 _push;

	public ByteStream ByteStream
	{
		get => _byteStream;
        set => SetByteStream(value);
    }

	public ProtoFeeder(IBinaryPrimitiveScanner primitiveScanner, ISerializationCore dynamicFacade, IByteArray bytes, ISerializerSettings settings)
		: base(primitiveScanner, dynamicFacade, bytes, settings)
	{
		ByteStream = (bytes as ByteStream);
		_arrayIndeces = new Stack<Int32>();
	}
    //
    // public sealed override Boolean HasMoreBytes
    // {
    //     get
    //     {
    //         var ok = _currentBytes.Index < _currentEndIndex;
    //         var rly = base.HasMoreBytes;
    //         if (ok != rly)
    //         { }
    //
    //         return rly;
    //     }
    // }

    public void SetStream(Stream stream)
	{
        _byteStream.SetStream(stream);
		_currentEndIndex = (Int32)_byteStream.Length - 1;
        _byteIndex = 0;
	}

    public void SetStream(Byte[] array, Int32 length)
    {
        if (_byteArray == null)
            _byteArray = new ByteArray(array);
        else
            _byteArray.Bytes = array;
        _currentEndIndex = length - 1;
        _currentBytes = _byteArray;
        _byteIndex = 0;
    }

	public void Push(Int32 length)
	{
		_arrayIndeces.Push(_currentEndIndex);
		_currentEndIndex = _byteIndex + length - 1;
	}

	[MethodImpl(256)]
	public Byte GetByte()
	{
		return _currentBytes[_byteIndex++];
	}

   

    public void Pop()
	{
		_currentEndIndex = _arrayIndeces.Pop();
	}

	private void SetByteStream(ByteStream stream)
	{
		_byteStream = stream;
	}

	public Object GetVarInt(Type type)
	{
		var result = 0L;
		var push = 0;
		Int32 currentByte;
		do
		{
			currentByte = _currentBytes[_byteIndex++];
			result += (currentByte & 0x7F) << push;
			if (push == 28 && result < 0)
			{
				_byteIndex += 5;
				break;
			}
			push += 7;
		}
		while ((currentByte & 0x80) != 0);
		return Convert.ChangeType(result, type);
	}

    // ReSharper disable once RedundantAssignment
    public void GetInt32(ref Int32 result)
	{
		_currentByte = 0;
		result = 0;
		_push = 0;
		while (true)
        {
            _currentByte = _currentBytes.GetNextByte();
            _byteIndex++;
			result += (_currentByte & 0x7F) << _push;
			if (_push == 28 && result < 0)
                break;
			
			_push += 7;
			if ((_currentByte & 0x80) == 0)
                return;
			
		}
		_byteIndex += 5;
	}

    [MethodImpl(256)]
    public void DumpInt32() => _currentBytes.DumpProtoVarInt(ref _byteIndex);
   

    public sealed override Byte[] GetBytes(Int32 count)
    {
        var res = _currentBytes.GetNextBytes(count);
        _byteIndex += count;
        return res;
    }

    public sealed override Int32 GetInt32()
	{
		var result = 0;
        _push = 0;
        _currentByte = 0;
		do
		{
            _currentByte= _currentBytes.GetNextByte(); 
            _byteIndex++;
			result += (_currentByte & 0x7F) << _push;
			if (_push == 28 && result < 0)
			{
				_byteIndex += 5;
				break;
			}
            _push += 7;
		}
		while ((_currentByte & 0x80) != 0);
		return result;
	}

	public override Int32 GetNextBlockSize()
	{
		return GetInt32();
	}
}
