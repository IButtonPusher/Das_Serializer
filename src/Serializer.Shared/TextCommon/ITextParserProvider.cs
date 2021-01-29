// ReSharper disable UnusedMember.Global

using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITextParserProvider
    {
        ITextParser TextParser { get; }
    }
}
