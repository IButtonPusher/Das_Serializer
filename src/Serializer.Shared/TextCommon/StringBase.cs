using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public abstract class StringBase
    {
        protected static String Remove(ISet<Char> chars,
                                       String from)
        {
            if (String.IsNullOrEmpty(from) || chars.Count == 0)
                return from;

            var isNeeded = false;
            for (var c = 0; c < from.Length; c++)
                if (chars.Contains(from[c]))
                {
                    isNeeded = true;
                    break;
                }

            if (!isNeeded)
                return from;

            return String.Concat(ExtractSurvivors());

            IEnumerable<Char> ExtractSurvivors()
            {
                for (var c = 0; c < from.Length; c++)
                {
                    var f = from[c];
                    if (!chars.Contains(f))
                        yield return f;
                }
            }
        }
    }
}
