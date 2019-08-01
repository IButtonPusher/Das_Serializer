using System;
using System.Threading.Tasks;

namespace Serializer
{
    public interface ISaveToFile
    {
        Task Save();
    }
}