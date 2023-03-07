using System;
using System.Threading.Tasks;

namespace Serializer.Tests
{
    public interface INiceAndEasy
    {
        void YouCan<T>(Action<T> touch);
    }
}
