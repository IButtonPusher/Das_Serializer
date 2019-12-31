using System;
using System.Diagnostics.CodeAnalysis;
using Das.Serializer.Scanners;

namespace Core.Shared.TextCommon
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class StringAccessor : ITextAccessor
    {
        private readonly String _accessing;

        public StringAccessor(String accessing)
        {
            _accessing = accessing;
        }
        public Char this[Int32 index] => _accessing[index];

        public String[] Split(Char splitter) => _accessing.Split(splitter);

        public String[] Split() => _accessing.Split();

        public String Right(Int32 numberOfChars) => _accessing.Substring(
            _accessing.Length - numberOfChars, numberOfChars);

        public Boolean Contains(Char c)
        {
            for (var i = 0; i < _accessing.Length; i++)
            {
                if (_accessing[i] == c)
                    return true;
            }

            return false;
        }

        public override String ToString() => _accessing;

        public String Substring(Int32 start, Int32 length) => _accessing.Substring(start, length);

        public String Remove(Int32 start, Int32 length) => _accessing.Remove(start, length);

        public String Substring(Int32 start) => _accessing.Substring(start);

        public String Trim() => _accessing.Trim();

        public Boolean Contains(String str, StringComparison comparison)
            => _accessing.IndexOf(str, comparison) >= 0;
        

        public Int32 Length => _accessing.Length;
        public Boolean IsNullOrWhiteSpace() => String.IsNullOrWhiteSpace(_accessing);
        public void CopyTo(Int32 sourceIndex, Char[] destination, Int32 destinationIndex, Int32 count)
            => _accessing.CopyTo(sourceIndex, destination, destinationIndex, count);
        

        public static implicit operator StringAccessor(String accessMe) 
            => new StringAccessor(accessMe);

        public static implicit operator String(StringAccessor accessMe)
            => accessMe?._accessing;
    }
}
