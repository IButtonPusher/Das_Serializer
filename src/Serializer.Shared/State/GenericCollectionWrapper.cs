using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Reflection.Common;

namespace Das.Serializer.State
{
    public static class GenericCollectionWrapper
    {
        public static IList Get(Type collectionType)
        {
            if (collectionType.IsGenericType)
            {
                var gargs = collectionType.GetGenericArguments();

                var callMe = typeof(GenericCollectionWrapper).GetPublicStaticMethodOrDie(
                                                                 nameof(GetGeneric))
                                                             .MakeGenericMethod(gargs[0]);

                var ctor = collectionType.GetConstructor(Type.EmptyTypes);
                var collection = ctor?.Invoke(new object[0])!;


                var res = callMe.Invoke(null, new[] { collection });


                return (IList)res;
            }

            throw new NotSupportedException();
        }

        public static IList GetGeneric<T>(Object collection)
        {
            switch (collection)
            {
                case ICollection<T> c:
                    return new ValueCollectionWrapper<T>(c);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
