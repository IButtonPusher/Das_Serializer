// Serializer_Tests_ProtocolBuffers_CollectionsPropertyMessage
using Das.Serializer.ProtoBuf;
using Serializer.Tests.ProtocolBuffers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class Serializer_Tests_ProtocolBuffers_CollectionsPropertyMessage : ProtoDynamicBase<CollectionsPropertyMessage>
{
	private Encoding _utf8;

	public Serializer_Tests_ProtocolBuffers_CollectionsPropertyMessage(Func<CollectionsPropertyMessage> P_0)
		: base(P_0)
	{
		Encoding encoding = _utf8 = Encoding.UTF8;
	}

	public sealed override void Print(CollectionsPropertyMessage P_0)
	{
		List<string>.Enumerator enumerator = P_0.List1.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				if (_stackDepth != 0)
				{
					UnsafeStackByte(10);
				}
				else
				{
					_outStream.WriteByte(10);
				}
				string current = enumerator.Current;
				Push();
				Pop();
			}
		}
		finally
		{
			enumerator.Dispose();
		}
		IEnumerator enumerator2 = P_0.Array1.GetEnumerator();
		while (enumerator2.MoveNext())
		{
			if (_stackDepth != 0)
			{
				UnsafeStackByte(18);
			}
			else
			{
				_outStream.WriteByte(18);
			}
			object current2 = enumerator2.Current;
			Push();
			Pop();
		}
		Flush();
	}

	public sealed override CollectionsPropertyMessage Scan(Stream P_0)
	{
		if (ProtoDynamicBase._readBytes == null)
		{
			ProtoDynamicBase._readBytes = new byte[256];
		}
		byte[] readBytes = ProtoDynamicBase._readBytes;
		CollectionsPropertyMessage collectionsPropertyMessage = BuildDefault();
		collectionsPropertyMessage.List1 = new List<string>();
		long length = P_0.Length;
		do
		{
			switch (ProtoDynamicBase.GetColumnIndex(P_0))
			{
			case 1:
			{
				int positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
				P_0.Read(readBytes, 0, positiveInt);
				string @string = ProtoDynamicBase.Utf8.GetString(readBytes, 0, positiveInt);
				collectionsPropertyMessage.List1.Add(@string);
				continue;
			}
			default:
				continue;
			case 0:
				break;
			}
			break;
		}
		while (P_0.Position < length);
		return collectionsPropertyMessage;
	}
}
