using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Das.Extensions;
using Das.Serializer.Scanners;

namespace Das.Serializer.Xml
{
    public partial class XmlExpress : BaseExpress
    {
        public XmlExpress(IInstantiator instantiator,
                          ITypeManipulator types,
                          ISerializerSettings settings,
                          IStringPrimitiveScanner primitiveScanner, 
                          ITypeInferrer typeInference)
        {
            _instantiator = instantiator;
            _types = types;
            _settings = settings;
            _primitiveScanner = primitiveScanner;
            _typeInference = typeInference;
        }

        private void BuildCollection(ref IList instance,
                                            ref Int32 currentIndex,
                                            String xml,
                                            Type germaneType,
                                            StringBuilder stringBuilder)
        {
            while (true)
            {
                GetUntil(ref currentIndex, xml, stringBuilder, '<');
                stringBuilder.Clear();

                var currentChar = xml[currentIndex];
                
                if (currentChar == '/')
                {
                    GetUntil(ref currentIndex, xml, stringBuilder, '>');
                    stringBuilder.Clear();
                    return;
                }

                GetCurrentTagName(ref currentIndex, xml, stringBuilder);
                
                stringBuilder.Clear();

                if (germaneType.IsValueType)
                {
                    currentIndex++;
                    GetUntil(ref currentIndex, xml, stringBuilder, '<');
                    instance.Add(_primitiveScanner.GetValue(
                        stringBuilder.GetConsumingString(), germaneType, true));
                }
                else
                {
                    var current = _instantiator.BuildDefault(germaneType, true) ?? throw new XmlException();
                    
                    DeserializeTag(ref current, ref currentIndex, xml, germaneType, stringBuilder);
                    instance.Add(current);
                }
            }
        }

        public IEnumerable<T> DeserializeMany<T>(String xml)
        {
            var currentIndex = 0;
            //skip the top level tag
            AdvanceUntil('>', ref currentIndex, xml);
            var stringBuilder = new StringBuilder();

            _iteratingIndex = currentIndex;
            foreach (var item in IterateCollection<T>(xml, stringBuilder))
            {
                yield return item;
            }

            //currentIndex = _iteratingIndex;
        }

        public T Deserialize<T>(String xml)
        {
            var currentIndex = 0;
            var stringBuilder = new StringBuilder();
            return DeserializeImpl<T>(xml, ref currentIndex, stringBuilder);
        }

        private T DeserializeImpl<T>(String xml,
                                     ref Int32 currentIndex,
                                     StringBuilder stringBuilder)
        {
            var current = _instantiator.BuildDefault<T>(true);
            Object oCurrent = current ?? throw new XmlException();
            

            AdvanceUntil('<', ref currentIndex, xml);

            if (xml[++currentIndex] == '?')
            {
                //skip encoding header
                AdvanceUntil('>', ref currentIndex, xml);
                AdvanceUntil('<', ref currentIndex, xml);
            }

            if (current is IList list && typeof(T).IsGenericType 
                                      && typeof(T).GetGenericArguments() is { } argList 
                                      && argList.Length == 1)
            {
                BuildCollection(ref list, ref currentIndex, xml, argList[0], stringBuilder);
                return current;
            }

            //currentIndex++;
            GetCurrentTagName(ref currentIndex, xml, stringBuilder);
            //todo: validate that the tag name matches the type?
            stringBuilder.Clear();

            DeserializeTag(ref oCurrent, ref currentIndex, xml, typeof(T), stringBuilder);

            return current;
        }

        /// <summary>
        ///     Assumes that the currentIndex points to the characater after the
        ///     root attribute
        /// </summary>
        private void DeserializeTag(ref Object instance,
                                    ref Int32 currentIndex,
                                    String xml,
                                    Type type,
                                    StringBuilder stringBuilder)
        {

            var ts = _types.GetTypeStructure(type, _settings);
            DeserializeTagImpl(ref instance, ref currentIndex, xml, type, stringBuilder, ts);
        }

        private static Char GetCurrentTagName(ref Int32 currentIndex,
                                              String xml,
                                              StringBuilder sbString)
        {
            for (; currentIndex < xml.Length; currentIndex++)
            {
                var c = xml[currentIndex];

                if (Char.IsWhiteSpace(c) || c == '>' || c == '/')
                    return c;

                sbString.Append(c);
            }

            throw new XmlException();
        }

        private static void GetNextString(ref Int32 currentIndex,
                                          String xml,
                                          StringBuilder sbString)
        {
            if (!TryGetNextString(ref currentIndex, xml, sbString))
                throw new XmlException();
        }

        private static Boolean TryGetNextString(ref Int32 currentIndex,
                                          String xml,
                                          StringBuilder sbString)
        {
            if (!AdvanceUntil('"', ref currentIndex, xml))
                return false;

            currentIndex++;
            GetUntil(ref currentIndex, xml, sbString, '"');

            return true;
        }

        private static void GetUntil(ref Int32 currentIndex,
                                     String xml,
                                     StringBuilder sbString,
                                     Char stopAt)
        {
            for (; currentIndex < xml.Length; currentIndex++)
            {
                var c = xml[currentIndex];
                if (c == stopAt)
                {
                    currentIndex++;
                    return;
                }

                sbString.Append(c);
            }

            throw new XmlException();
        }

        private static Boolean TryGetNextAttribute(ref Int32 currentIndex,
                                                   String xml,
                                                   StringBuilder sbString)
        {
            for (currentIndex++; currentIndex < xml.Length; currentIndex++)
            {
                var c = xml[currentIndex];

                switch (c)
                {
                    case ' ':
                        continue;

                    case '=':
                        return true;

                    case '/':
                    case '>':
                        return false;

                    default:
                        sbString.Append(c);
                        break;
                }
            }

            throw new XmlException();
        }

        private readonly IStringPrimitiveScanner _primitiveScanner;
        private readonly ISerializerSettings _settings;
        private readonly IInstantiator _instantiator;
        private readonly ITypeManipulator _types;
        private readonly ITypeInferrer _typeInference;
    }
}