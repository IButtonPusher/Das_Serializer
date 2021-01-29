using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer
{
    public abstract class BaseExpress
    {
        protected BaseExpress(Char endArrayChar,
                              Char endBlockChar,
                              ITypeManipulator typeManipulator)
        {
            _endArrayChar = endArrayChar;
            _endBlockChar = endBlockChar;
            _typeManipulator = typeManipulator;
        }

        public abstract T Deserialize<T>(String txt,
                                         ISerializerSettings settings,
                                         Object[] ctorValues);

        public abstract IEnumerable<T> DeserializeMany<T>(String txt);

        [MethodImpl(256)]
        protected Boolean AdvanceUntil(Char target,
                                       ref Int32 currentIndex,
                                       String txt)
        {
            for (; currentIndex < txt.Length; currentIndex++)
            {
                var current = txt[currentIndex];

                if (current == target)
                    return true;

                if (current == _endArrayChar || current == _endBlockChar)
                    return false;
            }

            return false;
        }

        [MethodImpl(256)]
        protected static Char AdvanceUntilAny(Char[] targets,
                                              ref Int32 currentIndex,
                                              String txt)
        {
            for (; currentIndex < txt.Length; currentIndex++)
            {
                var current = txt[currentIndex];

                for (var k = 0; k < targets.Length; k++)
                    if (current == targets[k])
                        return current;
            }

            throw new InvalidOperationException();
        }

        protected static ConstructorInfo? GetConstructorWithStringParam(Type type)
        {
            var allMyCtors = type.GetConstructors();
            for (var c = 0; c < allMyCtors.Length; c++)
            {
                var allMyParams = allMyCtors[c].GetParameters();
                if (allMyParams.Length == 1 && allMyParams[0].ParameterType == Const.StrType)
                    return allMyCtors[c];
            }

            return default;
        }

        protected Object? GetFromXPath(Object root,
                                       String xPath,
                                       StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
            Object? current = null;

            // xPath[0] should always be '/'
            for (var c = 1; c < xPath.Length; c++)
            {
                var currentChar = xPath[c];
                switch (currentChar)
                {
                    case '/':
                        if (!UpdateCurrentFromPathToken(ref current, root,
                            stringBuilder.GetConsumingString()))
                            return default;

                        break;

                    case '[':
                        break;

                    default:
                        stringBuilder.Append(currentChar);
                        break;
                }
            }

            if (stringBuilder.Length > 0)
                UpdateCurrentFromPathToken(ref current, root, stringBuilder.ToString());

            return current;
        }

        [MethodImpl(256)]
        protected static void GetUntil(ref Int32 currentIndex,
                                       String txt,
                                       StringBuilder sbString,
                                       Char target)
        {
            for (; currentIndex < txt.Length; currentIndex++)
            {
                var current = txt[currentIndex];
                if (current == target)
                    return;

                sbString.Append(current);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Advances currentIndex to the found index + 1
        /// </summary>
        protected static void GetUntilAny(ref Int32 currentIndex,
                                          String txt,
                                          StringBuilder sbString,
                                          Char[] targets,
                                          out Char foundChar)
        {
            for (; currentIndex < txt.Length; currentIndex++)
            {
                var current = txt[currentIndex];

                for (var k = 0; k < targets.Length; k++)
                    if (current == targets[k])
                    {
                        foundChar = current;
                        currentIndex++;
                        return;
                    }

                sbString.Append(current);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Advances currentIndex to the found index + 1
        /// </summary>
        protected static void GetUntilAny(ref Int32 currentIndex,
                                          String txt,
                                          StringBuilder sbString,
                                          Char[] targets)
        {
            for (; currentIndex < txt.Length; currentIndex++)
            {
                var current = txt[currentIndex];

                for (var k = 0; k < targets.Length; k++)
                    if (current == targets[k])
                    {
                        currentIndex++;
                        return;
                    }

                sbString.Append(current);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        ///     advances currentIndex until the stopAt is found + 1
        /// </summary>
        [MethodImpl(256)]
        protected static void SkipUntil(ref Int32 currentIndex,
                                        String txt,
                                        Char stopAt)
        {
            if (TrySkipUntil(ref currentIndex, txt, stopAt))
                return;

            throw new InvalidOperationException();
        }

        [MethodImpl(256)]
        protected static Boolean TryAdvanceUntilAny(Char[] targets,
                                                    ref Int32 currentIndex,
                                                    String txt,
                                                    out Char foundChar)
        {
            for (; currentIndex < txt.Length; currentIndex++)
            {
                var current = txt[currentIndex];

                for (var k = 0; k < targets.Length; k++)
                    if (current == targets[k])
                    {
                        foundChar = current;
                        return true;
                    }
            }

            foundChar = '0';
            return false;
        }

        /// <summary>
        ///     advances currentIndex until the stopAt is found + 1
        /// </summary>
        [MethodImpl(256)]
        protected static Boolean TrySkipUntil(ref Int32 currentIndex,
                                              String txt,
                                              Char stopAt)
        {
            for (; currentIndex < txt.Length; currentIndex++)
                if (txt[currentIndex] == stopAt)
                {
                    currentIndex++;
                    return true;
                }

            return false;
        }

        private Boolean UpdateCurrentFromPathToken(ref Object? current,
                                                   Object root,
                                                   String pathToken)
        {
            if (current == null)
            {
                if (String.Equals(pathToken,
                    root.GetType().FullName))
                {
                    current = root;
                    return true;
                }

                return false;
            }

            if (_typeManipulator.IsCollection(current.GetType()))
                throw new NotImplementedException();

            var prop = _typeManipulator.FindPublicProperty(current.GetType(),
                pathToken);
            if (prop == null)
                return false;

            current = prop.GetValue(current, null);
            return current != null;
        }

        protected const Char ImpossibleChar = '\0';

        [ThreadStatic]
        protected static Object[]? _singleObjectArray;

        private readonly Char _endArrayChar;
        protected readonly Char _endBlockChar;
        private readonly ITypeManipulator _typeManipulator;
    }
}
