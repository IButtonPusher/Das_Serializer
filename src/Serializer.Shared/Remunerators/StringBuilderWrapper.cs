using System;
using System.Collections.Generic;
using System.Text;

namespace Das.Serializer.Remunerators
{
    public abstract class StringBuilderWrapper : StringBase,
                                                 ITextRemunerable
    {
        private readonly StringBuilder _textRemunerableImplementation;

        public StringBuilderWrapper()
        {
            _textRemunerableImplementation = new StringBuilder();
        }

        public Int32 Capacity
        {
            get => _textRemunerableImplementation.Capacity;
            set => _textRemunerableImplementation.Capacity = value;
        }

        public Int32 Length => _textRemunerableImplementation.Length;

        public void Append(Char data1,
                           String data2)
        {
            _textRemunerableImplementation.Append(data1);
            _textRemunerableImplementation.Append(data2);
        }

        public void Append(ITextAccessor txt)
        {
            _textRemunerableImplementation.Append(txt);
        }

        void ITextRemunerable.Append(Char item)
        {
            _textRemunerableImplementation.Append(item);
        }

        public void AppendRepeatedly(Char item,
                                     Int32 count)
        {
            _textRemunerableImplementation.Append(item, count);
        }

        public bool Append<T>(IEnumerable<T> items,
                              Char separator) where T : IConvertible
        {
            _textRemunerableImplementation.Append(String.Join(separator.ToString(), items));
            return true;
        }

        public void Insert(Int32 index,
                           String str)
        {
            _textRemunerableImplementation.Insert(index, str);
        }

        public void Remove(Int32 startIndex,
                           Int32 length)
        {
            _textRemunerableImplementation.Remove(startIndex, length);
        }

        public abstract void PrintCurrentTabs();

        public abstract void TabIn();

        public abstract void TabOut();

        public abstract void NewLine();

        public abstract void IndentRepeatedly(Int32 count);


        public ITextAccessor ToImmutable()
        {
            return new StringAccessor(_textRemunerableImplementation.ToString());
        }

        public void Undispose()
        {
            //_textRemunerableImplementation.Undispose();
        }

        void IRemunerable<string, char>.Append(Char data)
        {
            _textRemunerableImplementation.Append(data);
        }

        public Boolean IsEmpty => _textRemunerableImplementation.Length == 0;

        public Char this[Int32 index] => _textRemunerableImplementation[index];

        public String this[Int32 start,
                           Int32 end] =>
            _textRemunerableImplementation.ToString().Substring(start, end - start);

        public void Append(String data)
        {
            _textRemunerableImplementation.Append(data);
        }

        public void Append(String data1,
                           String data2)
        {
            _textRemunerableImplementation.Append(data1);
            _textRemunerableImplementation.Append(data2);
        }

        public void Append(IEnumerable<string> datas)
        {
            _textRemunerableImplementation.Append(datas);
        }

        public void Append<T>(T data) where T : struct
        {
            _textRemunerableImplementation.Append(data);
        }

        public void Clear()
        {
            _textRemunerableImplementation.Clear();
        }

        public int IndexOf(String str,
                           Int32 startIndex)
        {
            return _textRemunerableImplementation.ToString().IndexOf(str, startIndex, 
                StringComparison.Ordinal);
        }

        void IStringRemunerable.Append(String data)
        {
            _textRemunerableImplementation.Append(data);
        }

        void IRemunerable<String>.Append(String str,
                                         Int32 cnt)
        {
            throw new NotSupportedException();
        }

        public sealed override String ToString()
        {
            return _textRemunerableImplementation.ToString();
        }

        public void Dispose()
        {
            //_textRemunerableImplementation.Length = 0;
            //_textRemunerableImplementation.Clear();
        }
    }
}
