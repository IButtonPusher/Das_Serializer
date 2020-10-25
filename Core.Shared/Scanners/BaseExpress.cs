using System;
using System.Runtime.CompilerServices;

namespace Das.Serializer.Scanners
{
    public abstract class BaseExpress
    {

        [MethodImpl(256)]
        protected static Boolean AdvanceUntil(Char target, 
                                              ref Int32 currentIndex, 
                                              String json)
        {
            for (; currentIndex < json.Length; currentIndex++)
            {
                var current = json[currentIndex];

                if (current == target)
                    return true;

                if (current == ']' || current == '}')
                    return false;
            }

            return false;
        }

        [MethodImpl(256)]
        protected static Char AdvanceUntilAny(Char[] targets, 
                                              ref Int32 currentIndex, 
                                              String json)
        {
            for (; currentIndex < json.Length; currentIndex++)
            {
                var current = json[currentIndex];

                for (var k = 0; k < targets.Length; k++)
                    if (current == targets[k])
                        return current;
            }

            throw new InvalidOperationException();
        }
    }
}
