using System;

namespace Das.Serializer.Types
{
    public static class EnumCache<T> where T : Enum
    {

        public static Boolean HasFlag(T values,
                                      T haveValue)
        {
            return values.HasFlag(haveValue);
        }
    }
}
