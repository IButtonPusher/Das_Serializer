using System;
using System.Collections;
using System.Collections.Generic;

namespace Das.Streamers
{
	internal class BinaryIterator : IEnumerable<Byte[]>
	{
		private readonly Byte[] _bytes;

		public BinaryIterator(Byte[] bytes)
		{
			_bytes = bytes;
		}

		public IEnumerator<byte[]> GetEnumerator()
		{
			yield return _bytes;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
