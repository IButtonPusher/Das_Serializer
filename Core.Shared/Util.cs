using System;
using System.Linq;
using System.Reflection;

namespace Das
{
    public static class DasUtil
    {
        public static MethodInfo? FindMethod(this Type type,
                                             String name,
                                             Object[] parameters,
                                             BindingFlags flags =
                                                 BindingFlags.Public | BindingFlags.Instance)
        {
            MethodInfo? meth = null;

            if (parameters.All(p => p != null))
            {
                var types = parameters.Select(p => p.GetType()).ToArray();
                meth = type.GetMethod(name, flags, null, types, null);
            }

            if (meth == null)
            {
                //can't look for the function from parameter types if there's a null
                meth = (from m in type.GetMethods(flags)
                    where m.Name == name && m.GetParameters().Length == parameters.Length
                    orderby CountMatchingTypes(m.GetParameters(), parameters) descending
                    select m).FirstOrDefault();
            }


            return meth;
        }

        private static Int32 CountMatchingTypes(ParameterInfo[] methParams, Object[] myValues)
        {
            var total = 0;
            for (var i = 0; i < methParams.Length; i++)
            {
                if (myValues[i] == null)
                    continue;

                if (methParams[i].ParameterType == myValues[i].GetType())
                    total++;
            }

            return total;
        }
    }
}