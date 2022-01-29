using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;

#pragma warning disable 8600
#pragma warning disable 8605
#pragma warning disable 8604
#pragma warning disable 8602

// ReSharper disable All

namespace Serializer.Tests
{
    public static class SlowEquality
    {
        public static Boolean AreEqual<T>(T left,
                                          T right)
        {
            var fk = "";
            return AreEqual(left, right, ref fk);
        }


        public static Boolean AreEqual<T>(T left,
                                          T right,
                                          ref String badProp)
        {
            if (ReferenceEquals(left, null) != ReferenceEquals(right, null))
                return false;

            if (ReferenceEquals(left, null))
                return true;

            if (left.Equals(right))
                return true;

            if (badProp == null)
                badProp = "";
            if (left == null && right != null)
                return false;

            var refl = typeof(T);
            if (refl == typeof(Object))
            {
                if (left == null && right == null)
                    return true;
                if (left != null && right == null)
                    return false;
                if (left == null && right != null)
                    return false;

                refl = left.GetType();
            }

            if (TypeCore.IsLeaf(refl, true))
            {
                if (!EqualityComparer<T>.Default.Equals(left, right))
                {
                    return false;
                }

                return true;
            }

            if (refl.IsCollection())
            {
                if (left is IList listLeft && right is IList listRight)
                {
                    if (listLeft.Count != listRight.Count)
                        return false;

                    for (var i = 0; i < listLeft.Count; i++)
                    {
                        if (!AreEqual(listLeft[i], listRight[i], ref badProp))
                            return false;
                    }
                }
                else if (left is IDictionary leftDict && right is IDictionary rightDict)
                {
                    if (leftDict.Count != rightDict.Count)
                        return false;

                    foreach (var k in leftDict.Keys)
                    {
                        if (!rightDict.Contains(k))
                            return false;

                        if (!AreEqual(leftDict[k], rightDict[k]))
                            return false;
                    }
                }
                else throw new NotSupportedException();

                return true;
            }

            var propsFound = 0;
            foreach (var prop in refl.GetProperties(BindingFlags.Public |
                                                    BindingFlags.Instance))
            {
                var l = prop.GetValue(left);
                var rProp = right.GetType().GetProperty(prop.Name);

                var r = rProp.GetValue(right);
                if (!AreEqual(l, r, ref badProp))
                {
                    badProp = prop.Name;
                    return false;
                }

                propsFound++;
            }

            if (propsFound == 0)
            {
                if (refl.IsCollection())
                {
                    var i = 0;
                    var iRight = right as IList;
                    foreach (var something in ((IList) left))
                    {
                        if (AreEqual(something, iRight[i++]))
                            return false;
                    }
                }

                if (!EqualityComparer<T>.Default.Equals(left, right))
                {
                    return false;
                }
            }

            return true;
        }

        public static Boolean IsCollection(this Type type) =>
            typeof(IEnumerable).IsAssignableFrom(type) && !type.IsString();
    }
}
