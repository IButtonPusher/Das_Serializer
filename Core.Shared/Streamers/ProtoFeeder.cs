// Das.Streamers.ProtoFeeder
using Das.Serializer;
using Das.Streamers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

public class ProtoFeeder : BinaryFeeder,IProtoFeeder
{
	private Stack<Int32> _arrayIndeces;

	private ByteStream _byteStream;

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

	public void SetStream(Stream stream)
	{
		ByteStream.SetStream(stream);
		_currentEndIndex = (Int32)ByteStream.Length - 1;
		Index = 0;
	}

	public void Push(Int32 length)
	{
		_arrayIndeces.Push(_currentEndIndex);
		_currentEndIndex = Index + length - 1;
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

	public void GetInt32(ref Int32 result)
	{
		_currentByte = 0;
		result = 0;
		_push = 0;
		while (true)
		{
			_currentByte = _currentBytes[_byteIndex++];
			result += (_currentByte & 0x7F) << _push;
			if (_push == 28 && result < 0)
			{
				break;
			}
			_push += 7;
			if ((_currentByte & 0x80) == 0)
			{
				return;
			}
		}
		_byteIndex += 5;
	}

	public sealed override Int32 GetInt32()
	{
		var result = 0;
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
		return result;
	}

	public override Int32 GetNextBlockSize()
	{
		return GetInt32();
	}
}
