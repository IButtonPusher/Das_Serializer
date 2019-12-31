using System;
using System.IO;

namespace Das.Serializer.Remunerators
{
    public interface IStreamDelegate
    {
        Stream OutStream { get; }
    }
}
