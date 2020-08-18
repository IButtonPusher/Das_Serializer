using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    /// <summary>
    ///     Stateful and not thread safe
    /// </summary>
    public interface ITextState : ISerializationState, ITextContext
    {
        IScannerBase<Char[]> ArrayScanner { get; }

        ITextScanner Scanner { get; }
    }
}