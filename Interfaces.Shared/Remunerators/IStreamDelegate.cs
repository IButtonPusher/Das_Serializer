using System;
using System.IO;
using System.Threading.Tasks;

namespace Das.Serializer.Remunerators
{
    public interface IStreamDelegate
    {
        Stream OutStream { get; set; }
    }
}