using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface INamedField 
    {
        String Name { get; }

        Type Type { get; }
    }
}
