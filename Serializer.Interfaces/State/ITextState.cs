using System;

namespace Das.Serializer
{
    /// <summary>
    /// Stateful and not thread safe
    /// </summary>
    public interface ITextState : ISerializationState, ITextContext
    {
        //IScannerBase<IEnumerable<Char>> Scanner { get; }

        ITextScanner Scanner { get; }
}
}
