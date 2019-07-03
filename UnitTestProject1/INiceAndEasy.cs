using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject1
{
    public interface INiceAndEasy
    {
        void YouCan<T>(Action<T> touch);
    }
}
