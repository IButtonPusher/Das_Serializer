// Serializer_Tests_ProtocolBuffers_ComposedMessage
using Das.Serializer.Proto;
using Serializer.Tests.ProtocolBuffers;
using System;
using System.IO;
using System.Text;

public class Serializer_Tests_ProtocolBuffers_ComposedMessage : ProtoDynamicBase<ComposedMessage>
{
	private Encoding _utf8;

	public Serializer_Tests_ProtocolBuffers_ComposedMessage(Func<ComposedMessage> P_0)
		: base(P_0)
	{
		Encoding encoding = _utf8 = Encoding.UTF8;
	}

	public sealed override void Print(ComposedMessage P_0)
	{
		WriteInt8((byte)8);
		WriteInt32(P_0.A);
		WriteInt8((byte)18);
		ComposedMessage2 innerComposed = P_0.InnerComposed1;
		Push();
		WriteInt8((byte)8);
		WriteInt32(innerComposed.A);
		WriteInt8((byte)18);
		MultiPropMessage multiPropMessage = innerComposed.MultiPropMessage1;
		Push();
		WriteInt8((byte)16);
		WriteInt32(multiPropMessage.A);
		WriteInt8((byte)10);
		string s = multiPropMessage.S;
		byte[] bytes = _utf8.GetBytes(s);
		WriteInt32(bytes.Length);
		Write(bytes);
		Pop();
		WriteInt8((byte)26);
		MultiPropMessage multiPropMessage2 = innerComposed.MultiPropMessage2;
		Push();
		WriteInt8((byte)16);
		WriteInt32(multiPropMessage2.A);
		WriteInt8((byte)10);
		s = multiPropMessage2.S;
		bytes = _utf8.GetBytes(s);
		WriteInt32(bytes.Length);
		Write(bytes);
		Pop();
		Pop();
		WriteInt8((byte)26);
		ComposedMessage2 innerComposed2 = P_0.InnerComposed2;
		Push();
		WriteInt8((byte)8);
		WriteInt32(innerComposed2.A);
		WriteInt8((byte)18);
		MultiPropMessage multiPropMessage3 = innerComposed2.MultiPropMessage1;
		Push();
		WriteInt8((byte)16);
		WriteInt32(multiPropMessage3.A);
		WriteInt8((byte)10);
		s = multiPropMessage3.S;
		bytes = _utf8.GetBytes(s);
		WriteInt32(bytes.Length);
		Write(bytes);
		Pop();
		WriteInt8((byte)26);
		MultiPropMessage multiPropMessage4 = innerComposed2.MultiPropMessage2;
		Push();
		WriteInt8((byte)16);
		WriteInt32(multiPropMessage4.A);
		WriteInt8((byte)10);
		s = multiPropMessage4.S;
		bytes = _utf8.GetBytes(s);
		WriteInt32(bytes.Length);
		Write(bytes);
		Pop();
		Pop();
		Flush();
	}

	public sealed override ComposedMessage Scan(Stream P_0)
	{
		byte[] array = new byte[256];
		ComposedMessage composedMessage = BuildDefault();
		long length = P_0.Length;
		int positiveInt = default(int);
		do
		{
			switch (ProtoDynamicBase.GetColumnIndex(P_0))
			{
			case 1:
			{
				int num13 = composedMessage.A = ProtoDynamicBase.GetInt32(P_0);
				DebugWriteline(composedMessage, num13);
				continue;
			}
			case 2:
			{
				ComposedMessage2 composedMessage3 = new ComposedMessage2();
				int num7 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
				if ((int)P_0.Position < num7)
				{
					DebugWriteline("istart InnerComposed1", num7);
					do
					{
						switch (ProtoDynamicBase.GetColumnIndex(P_0))
						{
						case 1:
						{
							int num12 = composedMessage3.A = ProtoDynamicBase.GetInt32(P_0);
							DebugWriteline(composedMessage3, num12);
							continue;
						}
						case 2:
						{
							MultiPropMessage multiPropMessage4 = new MultiPropMessage();
							int num10 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
							if ((int)P_0.Position < num10)
							{
								DebugWriteline("istart MultiPropMessage1", num10);
								do
								{
									switch (ProtoDynamicBase.GetColumnIndex(P_0))
									{
									case 2:
									{
										int num11 = multiPropMessage4.A = ProtoDynamicBase.GetInt32(P_0);
										DebugWriteline(multiPropMessage4, num11);
										continue;
									}
									case 1:
									{
										positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
										P_0.Read(array, 0, positiveInt);
										string obj4 = multiPropMessage4.S = ProtoDynamicBase.Utf8.GetString(array, 0, positiveInt);
										DebugWriteline(multiPropMessage4, obj4);
										continue;
									}
									default:
										continue;
									case 0:
										break;
									}
									break;
								}
								while (P_0.Position < num10);
							}
							DebugWriteline("iend MultiPropMessage1", positiveInt);
							composedMessage3.MultiPropMessage1 = multiPropMessage4;
							DebugWriteline(composedMessage3, multiPropMessage4);
							continue;
						}
						case 3:
						{
							MultiPropMessage multiPropMessage3 = new MultiPropMessage();
							int num8 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
							if ((int)P_0.Position < num8)
							{
								DebugWriteline("istart MultiPropMessage2", num8);
								do
								{
									switch (ProtoDynamicBase.GetColumnIndex(P_0))
									{
									case 2:
									{
										int num9 = multiPropMessage3.A = ProtoDynamicBase.GetInt32(P_0);
										DebugWriteline(multiPropMessage3, num9);
										continue;
									}
									case 1:
									{
										positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
										P_0.Read(array, 0, positiveInt);
										string obj3 = multiPropMessage3.S = ProtoDynamicBase.Utf8.GetString(array, 0, positiveInt);
										DebugWriteline(multiPropMessage3, obj3);
										continue;
									}
									default:
										continue;
									case 0:
										break;
									}
									break;
								}
								while (P_0.Position < num8);
							}
							DebugWriteline("iend MultiPropMessage2", positiveInt);
							composedMessage3.MultiPropMessage2 = multiPropMessage3;
							DebugWriteline(composedMessage3, multiPropMessage3);
							continue;
						}
						default:
							continue;
						case 0:
							break;
						}
						break;
					}
					while (P_0.Position < num7);
				}
				DebugWriteline("iend InnerComposed1", positiveInt);
				composedMessage.InnerComposed1 = composedMessage3;
				DebugWriteline(composedMessage, composedMessage3);
				continue;
			}
			case 3:
			{
				ComposedMessage2 composedMessage2 = new ComposedMessage2();
				int num = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
				if ((int)P_0.Position < num)
				{
					DebugWriteline("istart InnerComposed2", num);
					do
					{
						switch (ProtoDynamicBase.GetColumnIndex(P_0))
						{
						case 1:
						{
							int num6 = composedMessage2.A = ProtoDynamicBase.GetInt32(P_0);
							DebugWriteline(composedMessage2, num6);
							continue;
						}
						case 2:
						{
							MultiPropMessage multiPropMessage2 = new MultiPropMessage();
							int num4 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
							if ((int)P_0.Position < num4)
							{
								DebugWriteline("istart MultiPropMessage1", num4);
								do
								{
									switch (ProtoDynamicBase.GetColumnIndex(P_0))
									{
									case 2:
									{
										int num5 = multiPropMessage2.A = ProtoDynamicBase.GetInt32(P_0);
										DebugWriteline(multiPropMessage2, num5);
										continue;
									}
									case 1:
									{
										positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
										P_0.Read(array, 0, positiveInt);
										string obj2 = multiPropMessage2.S = ProtoDynamicBase.Utf8.GetString(array, 0, positiveInt);
										DebugWriteline(multiPropMessage2, obj2);
										continue;
									}
									default:
										continue;
									case 0:
										break;
									}
									break;
								}
								while (P_0.Position < num4);
							}
							DebugWriteline("iend MultiPropMessage1", positiveInt);
							composedMessage2.MultiPropMessage1 = multiPropMessage2;
							DebugWriteline(composedMessage2, multiPropMessage2);
							continue;
						}
						case 3:
						{
							MultiPropMessage multiPropMessage = new MultiPropMessage();
							int num2 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
							if ((int)P_0.Position < num2)
							{
								DebugWriteline("istart MultiPropMessage2", num2);
								do
								{
									switch (ProtoDynamicBase.GetColumnIndex(P_0))
									{
									case 2:
									{
										int num3 = multiPropMessage.A = ProtoDynamicBase.GetInt32(P_0);
										DebugWriteline(multiPropMessage, num3);
										continue;
									}
									case 1:
									{
										positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
										P_0.Read(array, 0, positiveInt);
										string obj = multiPropMessage.S = ProtoDynamicBase.Utf8.GetString(array, 0, positiveInt);
										DebugWriteline(multiPropMessage, obj);
										continue;
									}
									default:
										continue;
									case 0:
										break;
									}
									break;
								}
								while (P_0.Position < num2);
							}
							DebugWriteline("iend MultiPropMessage2", positiveInt);
							composedMessage2.MultiPropMessage2 = multiPropMessage;
							DebugWriteline(composedMessage2, multiPropMessage);
							continue;
						}
						default:
							continue;
						case 0:
							break;
						}
						break;
					}
					while (P_0.Position < num);
				}
				DebugWriteline("iend InnerComposed2", positiveInt);
				composedMessage.InnerComposed2 = composedMessage2;
				DebugWriteline(composedMessage, composedMessage2);
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
		return composedMessage;
	}
}


//
// // Serializer_Tests_ProtocolBuffers_ComposedMessage
// using Das.Serializer.Proto;
// using Serializer.Tests.ProtocolBuffers;
// using System;
// using System.IO;
// using System.Text;
// public class Serializer_Tests_ProtocolBuffers_ComposedMessage : ProtoDynamicBase<ComposedMessage>
// {
// 	private Encoding _utf8;
//
// 	public Serializer_Tests_ProtocolBuffers_ComposedMessage(Func<ComposedMessage> P_0)
// 		: base(P_0)
// 	{
// 		Encoding encoding = _utf8 = Encoding.UTF8;
// 	}
//
// 	public sealed override void Print(ComposedMessage P_0)
// 	{
// 		WriteInt8((byte)8);
// 		WriteInt32(P_0.A);
// 		WriteInt8((byte)18);
// 		ComposedMessage2 innerComposed = P_0.InnerComposed1;
// 		Push();
// 		WriteInt8((byte)8);
// 		WriteInt32(innerComposed.A);
// 		WriteInt8((byte)18);
// 		MultiPropMessage multiPropMessage = innerComposed.MultiPropMessage1;
// 		Push();
// 		WriteInt8((byte)16);
// 		WriteInt32(multiPropMessage.A);
// 		WriteInt8((byte)10);
// 		string s = multiPropMessage.S;
// 		byte[] bytes = _utf8.GetBytes(s);
// 		WriteInt32(bytes.Length);
// 		Write(bytes);
// 		Pop();
// 		WriteInt8((byte)26);
// 		MultiPropMessage multiPropMessage2 = innerComposed.MultiPropMessage2;
// 		Push();
// 		WriteInt8((byte)16);
// 		WriteInt32(multiPropMessage2.A);
// 		WriteInt8((byte)10);
// 		s = multiPropMessage2.S;
// 		bytes = _utf8.GetBytes(s);
// 		WriteInt32(bytes.Length);
// 		Write(bytes);
// 		Pop();
// 		Pop();
// 		WriteInt8((byte)26);
// 		ComposedMessage2 innerComposed2 = P_0.InnerComposed2;
// 		Push();
// 		WriteInt8((byte)8);
// 		WriteInt32(innerComposed2.A);
// 		WriteInt8((byte)18);
// 		MultiPropMessage multiPropMessage3 = innerComposed2.MultiPropMessage1;
// 		Push();
// 		WriteInt8((byte)16);
// 		WriteInt32(multiPropMessage3.A);
// 		WriteInt8((byte)10);
// 		s = multiPropMessage3.S;
// 		bytes = _utf8.GetBytes(s);
// 		WriteInt32(bytes.Length);
// 		Write(bytes);
// 		Pop();
// 		WriteInt8((byte)26);
// 		MultiPropMessage multiPropMessage4 = innerComposed2.MultiPropMessage2;
// 		Push();
// 		WriteInt8((byte)16);
// 		WriteInt32(multiPropMessage4.A);
// 		WriteInt8((byte)10);
// 		s = multiPropMessage4.S;
// 		bytes = _utf8.GetBytes(s);
// 		WriteInt32(bytes.Length);
// 		Write(bytes);
// 		Pop();
// 		Pop();
// 		Flush();
// 	}
//
// 	public sealed override ComposedMessage Scan(Stream P_0)
// 	{
// 		byte[] array = new byte[256];
// 		ComposedMessage composedMessage = BuildDefault();
// 		long length = P_0.Length;
// 		do
// 		{
// 			switch (ProtoDynamicBase.GetColumnIndex(P_0))
// 			{
// 			case 1:
// 			{
// 				int num7 = composedMessage.A = ProtoDynamicBase.GetInt32(P_0);
// 				continue;
// 			}
// 			case 2:
// 			{
// 				ComposedMessage2 composedMessage3 = new ComposedMessage2();
// 				int num8 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
// 				if ((int)P_0.Position < num8)
// 				{
// 					do
// 					{
// 						switch (ProtoDynamicBase.GetColumnIndex(P_0))
// 						{
// 						case 1:
// 						{
// 							int num11 = composedMessage3.A = ProtoDynamicBase.GetInt32(P_0);
// 							continue;
// 						}
// 						case 2:
// 						{
// 							MultiPropMessage multiPropMessage4 = new MultiPropMessage();
// 							int num12 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
// 							if ((int)P_0.Position < num12)
// 							{
// 								do
// 								{
// 									switch (ProtoDynamicBase.GetColumnIndex(P_0))
// 									{
// 									case 2:
// 									{
// 										int num13 = multiPropMessage4.A = ProtoDynamicBase.GetInt32(P_0);
// 										continue;
// 									}
// 									case 1:
// 									{
// 										int positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
// 										P_0.Read(array, 0, positiveInt);
// 										string text4 = multiPropMessage4.S = ProtoDynamicBase.Utf8.GetString(array, 0, positiveInt);
// 										continue;
// 									}
// 									default:
// 										continue;
// 									case 0:
// 										break;
// 									}
// 									break;
// 								}
// 								while (P_0.Position < num12);
// 							}
// 							composedMessage3.MultiPropMessage1 = multiPropMessage4;
// 							continue;
// 						}
// 						case 3:
// 						{
// 							MultiPropMessage multiPropMessage3 = new MultiPropMessage();
// 							int num9 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
// 							if ((int)P_0.Position < num9)
// 							{
// 								do
// 								{
// 									switch (ProtoDynamicBase.GetColumnIndex(P_0))
// 									{
// 									case 2:
// 									{
// 										int num10 = multiPropMessage3.A = ProtoDynamicBase.GetInt32(P_0);
// 										continue;
// 									}
// 									case 1:
// 									{
// 										int positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
// 										P_0.Read(array, 0, positiveInt);
// 										string text3 = multiPropMessage3.S = ProtoDynamicBase.Utf8.GetString(array, 0, positiveInt);
// 										continue;
// 									}
// 									default:
// 										continue;
// 									case 0:
// 										break;
// 									}
// 									break;
// 								}
// 								while (P_0.Position < num9);
// 							}
// 							composedMessage3.MultiPropMessage2 = multiPropMessage3;
// 							continue;
// 						}
// 						default:
// 							continue;
// 						case 0:
// 							break;
// 						}
// 						break;
// 					}
// 					while (P_0.Position < num8);
// 				}
// 				composedMessage.InnerComposed1 = composedMessage3;
// 				continue;
// 			}
// 			case 3:
// 			{
// 				ComposedMessage2 composedMessage2 = new ComposedMessage2();
// 				int num = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
// 				if ((int)P_0.Position < num)
// 				{
// 					do
// 					{
// 						switch (ProtoDynamicBase.GetColumnIndex(P_0))
// 						{
// 						case 1:
// 						{
// 							int num4 = composedMessage2.A = ProtoDynamicBase.GetInt32(P_0);
// 							continue;
// 						}
// 						case 2:
// 						{
// 							MultiPropMessage multiPropMessage2 = new MultiPropMessage();
// 							int num5 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
// 							if ((int)P_0.Position < num5)
// 							{
// 								do
// 								{
// 									switch (ProtoDynamicBase.GetColumnIndex(P_0))
// 									{
// 									case 2:
// 									{
// 										int num6 = multiPropMessage2.A = ProtoDynamicBase.GetInt32(P_0);
// 										continue;
// 									}
// 									case 1:
// 									{
// 										int positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
// 										P_0.Read(array, 0, positiveInt);
// 										string text2 = multiPropMessage2.S = ProtoDynamicBase.Utf8.GetString(array, 0, positiveInt);
// 										continue;
// 									}
// 									default:
// 										continue;
// 									case 0:
// 										break;
// 									}
// 									break;
// 								}
// 								while (P_0.Position < num5);
// 							}
// 							composedMessage2.MultiPropMessage1 = multiPropMessage2;
// 							continue;
// 						}
// 						case 3:
// 						{
// 							MultiPropMessage multiPropMessage = new MultiPropMessage();
// 							int num2 = (int)P_0.Position + ProtoDynamicBase.GetPositiveInt32(P_0);
// 							if ((int)P_0.Position < num2)
// 							{
// 								do
// 								{
// 									switch (ProtoDynamicBase.GetColumnIndex(P_0))
// 									{
// 									case 2:
// 									{
// 										int num3 = multiPropMessage.A = ProtoDynamicBase.GetInt32(P_0);
// 										continue;
// 									}
// 									case 1:
// 									{
// 										int positiveInt = ProtoDynamicBase.GetPositiveInt32(P_0);
// 										P_0.Read(array, 0, positiveInt);
// 										string text = multiPropMessage.S = ProtoDynamicBase.Utf8.GetString(array, 0, positiveInt);
// 										continue;
// 									}
// 									default:
// 										continue;
// 									case 0:
// 										break;
// 									}
// 									break;
// 								}
// 								while (P_0.Position < num2);
// 							}
// 							composedMessage2.MultiPropMessage2 = multiPropMessage;
// 							continue;
// 						}
// 						default:
// 							continue;
// 						case 0:
// 							break;
// 						}
// 						break;
// 					}
// 					while (P_0.Position < num);
// 				}
// 				composedMessage.InnerComposed2 = composedMessage2;
// 				continue;
// 			}
// 			default:
// 				continue;
// 			case 0:
// 				break;
// 			}
// 			break;
// 		}
// 		while (P_0.Position < length);
// 		return composedMessage;
// 	}
// }
