using Das.Scanners;
using System;

namespace Das.Serializer
{
    /// <summary>
    /// Stateful and not thread safe
    /// </summary>
    public interface ITextState : ISerializationState, ITextContext
    {
        ITextScanner Scanner { get; }

        IScannerBase<Char[]> ArrayScanner { get; }
    }
}