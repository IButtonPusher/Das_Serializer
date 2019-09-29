using System;
using Interfaces.Shared.State;

namespace Das.Serializer
{
    public interface IJsonLoaner : ITextState, IMutableState
    {
    }
}