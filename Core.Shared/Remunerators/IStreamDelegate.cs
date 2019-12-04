using System;
using System.IO;

namespace Serializer.Core.Remunerators
{
    public interface IStreamDelegate
    {
        Stream OutStream { get; }
    }
}
