//using System;
//using System.Collections.Generic;

//namespace Das.Remunerators
//{
//	/// <summary>
//	/// temporary writer for binary data for when the size of an object has to 
//	/// precede the object's serialized bytes in the stream
//	/// </summary>
//	internal class BinaryStackWriter : BinarySaveBase
//	{
//		public override Int32 Index
//		{
//			get { return Data.Count; }
//		}

//		public List<Byte> Data { get; private set; }

//		public BinaryStackWriter()
//		{	
//			Data = new List<byte>(Int16.MaxValue);
//		}

//		public override void Append(byte[] data)
//		{			
//			Data.AddRange(data);
//		}

//		public override void Append(byte data)
//		{
//			Data.Add(data);
//		}

//		public override void Dispose()
//		{
//			throw new NotImplementedException();
//		}
//	}
//}

