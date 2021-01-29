// Serializer_Tests_ProtocolBuffers_PackedArrayTest

using System;
using System.Threading.Tasks;

//public class Serializer_Tests_ProtocolBuffers_CollectionsPropertyMessage : ProtoDynamicBase<CollectionsPropertyMessage>
//{
//	private Encoding _utf8;

//	public Serializer_Tests_ProtocolBuffers_CollectionsPropertyMessage(Func<CollectionsPropertyMessage> P_0)
//		: base(P_0)
//	{
//		Encoding encoding = _utf8 = Encoding.UTF8;
//	}

//	public sealed override void Print(CollectionsPropertyMessage P_0)
//	{
//		List<string>.Enumerator enumerator = P_0.List1.GetEnumerator();
//		try
//		{
//			while (enumerator.MoveNext())
//			{
//				if (_stackDepth != 0)
//				{
//					UnsafeStackByte(10);
//				}
//				else
//				{
//					_outStream.WriteByte(10);
//				}
//				string current = enumerator.Current;
//				string s = current;
//				byte[] bytes = _utf8.GetBytes(s);
//				WriteInt32(bytes.Length);
//				Write(bytes);
//			}
//		}
//		finally
//		{
//			enumerator.Dispose();
//		}
//		_outStream.WriteByte(18);
//		int[] array = P_0.Array1;
//		WriteInt32(GetPackedArrayLength32<int[]>(array));
//		WritePacked32<int[]>(array);
//		Flush();
//	}

//	public sealed override CollectionsPropertyMessage Scan(Stream P_0)
//	{
//		if (ProtoDynamicBase._readBytes == null)
//		{
//			ProtoDynamicBase._readBytes = new byte[256];
//		}
//		byte[] readBytes = ProtoDynamicBase._readBytes;
//		CollectionsPropertyMessage collectionsPropertyMessage = BuildDefault();
//		collectionsPropertyMessage.List1 = new List<string>();
//		int num = 0;
//		List<long> list = new List<long>();
//		List<int> list2 = new List<int>();
//		long length = P_0.Length;
//		do
//		{
//			switch (ProtoDynamicBase.GetColumnIndex(P_0))
//			{
//			case 1:
//			{
//				int positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
//				P_0.Read(readBytes, 0, positiveInt);
//				string @string = ProtoDynamicBase.Utf8.GetString(readBytes, 0, positiveInt);
//				collectionsPropertyMessage.List1.Add(@string);
//				continue;
//			}
//			case 2:
//				((CollectionsPropertyMessage)(object)collectionsPropertyMessage.Array1).Array1 = ProtoDynamicBase.ExtractPacked32(P_0, ProtoDynamicBase.GetPositiveInt32(P_0)).ToArray<int>();
//				num++;
//				list.Add(P_0.Position);
//				continue;
//			default:
//				continue;
//			case 0:
//				break;
//			}
//			break;
//		}
//		while (P_0.Position < length);
//		return collectionsPropertyMessage;
//	}
//}
