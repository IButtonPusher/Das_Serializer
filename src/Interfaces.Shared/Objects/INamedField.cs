using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface INamedField //: IStronglyTyped
    {
        String Name { get; }

        Type Type { get; }
    }
}
