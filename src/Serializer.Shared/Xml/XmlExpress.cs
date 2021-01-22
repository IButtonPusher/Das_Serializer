using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Das.Extensions;

namespace Das.Serializer.Xml
{
    public partial class XmlExpress : BaseExpress
    {
        public XmlExpress(IInstantiator instantiator,
                          ITypeManipulator types,
                          ISerializerSettings settings,
                          IStringPrimitiveScanner primitiveScanner,
                          ITypeInferrer typeInference)
            : base(ImpossibleChar, '>')
        {
            _instantiator = instantiator;
            _types = types;
            _settings = settings;
            _primitiveScanner = primitiveScanner;
            _typeInference = typeInference;
        }

        //public sealed override T Deserialize<T>(String xml)
        public sealed override T Deserialize<T>(String xml,
                                                ISerializerSettings settings,
                                                Object[] ctorValues)
        {
            var currentIndex = 0;
            var stringBuilder = new StringBuilder();
            return DeserializeImpl<T>(xml, ref currentIndex, stringBuilder);
        }

        public sealed override IEnumerable<T> DeserializeMany<T>(String xml)
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

        private T DeserializeImpl<T>(String xml,
                                     ref Int32 currentIndex,
                                     StringBuilder stringBuilder)
        {
            var current = typeof(T) != Const.ObjectType
                ? _instantiator.BuildDefault<T>(true)
                : new RuntimeObject<T>();

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

            DeserializeTag(ref oCurrent, ref currentIndex, xml,
                current.GetType(),
                stringBuilder);

            switch (oCurrent)
            {
                case T good:
                    return good;

                    default:
                return current;
            }
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

        /// <summary>
        ///     Assumes that the currentIndex points to the characater after the
        ///     root attribute
        /// </summary>
        private void DeserializeTagImpl<TValueSetter>(ref Object instance,
                                                      ref Int32 currentIndex,
                                                      String xml,
                                                      Type type,
                                                      StringBuilder stringBuilder,
                                                      TValueSetter ts)
            where TValueSetter : IValueSetter
        {
            var currentChar = xml[currentIndex];
            PropertyInfo? prop = null;
            Type propType;

            while (true)
                switch (currentChar)
                {
                    case '/':
                        //end of the object
                        AdvanceUntil('>', ref currentIndex, xml);
                        return;

                    case '>':
                        if (prop == null)
                        {
                            currentIndex++;
                            var nodeVal = GetNodeValue(ref currentIndex, xml, stringBuilder);
                            var converter = _types.GetTypeConverter(type);
                            var val = converter.ConvertFrom(nodeVal);

                            stringBuilder.Clear();

                            if (val != null)
                                instance = val;
                        }

                        if (!AdvanceUntil('<', ref currentIndex, xml))
                            break;

                        currentChar = xml[currentIndex];
                        break;

                    case ' ':
                        //try to load next value as attribute
                        if (TryGetNextAttribute(ref currentIndex, xml, stringBuilder))
                        {
                            var attrib = stringBuilder.ToString();
                            stringBuilder.Clear();
                            prop = type.GetProperty(attrib);

                            if (prop == null)
                                switch (attrib)
                                {
                                    case Const.XmlType:
                                        stringBuilder.Clear();
                                        var typeName = GetNextString(ref currentIndex, xml, stringBuilder);
                                        type = _typeInference.GetTypeFromClearName(typeName) ??
                                               throw new TypeLoadException(typeName);
                                        stringBuilder.Clear();
                                        break;

                                    case Const.RefTag:
                                        //circular dependency

                                        break;
                                }
                            else
                                LoadNextString(ref currentIndex, xml, stringBuilder);


                            if (prop != null)
                                ts.SetValue(attrib, ref instance,
                                    _primitiveScanner.GetValue(stringBuilder.ToString(),
                                        prop.PropertyType, true),
                                    _settings.SerializationDepth);

                            stringBuilder.Clear();
                        }

                        currentChar = xml[currentIndex];

                        break;

                    case '<':
                        currentIndex++;
                        currentChar = GetCurrentTagName(ref currentIndex, xml, stringBuilder);

                        if (currentChar == '/')
                        {
                            //end of the object
                            AdvanceUntil('>', ref currentIndex, xml);
                            return;
                        }

                        prop = type.GetProperty(stringBuilder.ToString());
                        stringBuilder.Clear();

                        if (prop != null)
                        {
                            //Debug.WriteLine("got to prop " + prop);

                            propType = prop.PropertyType;
                            Object? child = null;

                            if (_types.IsCollection(propType))
                            {
                                //////////////////////////////////////////////////
                                // COLLECTION
                                //////////////////////////////////////////////////

                                if (currentChar == ' ')
                                {
                                    currentChar = xml[++currentIndex];
                                    if (currentChar == '/')
                                    {
                                        // self closing do nothing value
                                        if (ts.GetValue(instance, prop.Name) == null)
                                            ts.SetValue(prop.Name, ref instance,
                                                _instantiator.BuildDefault(type, true),
                                                _settings.SerializationDepth);

                                        AdvanceUntil('>', ref currentIndex, xml);
                                        currentChar = xml[currentIndex];

                                        continue;
                                    }
                                }


                                IList collectChild;
                                var germane = _types.GetGermaneType(propType);

                                if (propType.IsArray)
                                {
                                    //////////////////////////////////////////////////
                                    // ARRAY
                                    //////////////////////////////////////////////////

                                    collectChild = _instantiator.BuildGenericList(germane);
                                    BuildCollection(ref collectChild, ref currentIndex, xml,
                                        germane, stringBuilder);
                                    var arr = Array.CreateInstance(
                                        germane, collectChild.Count);
                                    collectChild.CopyTo(arr, 0);

                                    ts.SetValue(prop.Name, ref instance,
                                        arr, _settings.SerializationDepth);
                                }
                                else if (ts.GetValue(instance, prop.Name) is IList l)
                                    // property already set and we can add to it
                                    BuildCollection(ref l, ref currentIndex, xml,
                                        germane, stringBuilder);
                                else
                                {
                                    // property is either null or the wrong type
                                    //  in the latter case this may go poorly
                                    collectChild = _instantiator.BuildDefault(type, true) as IList
                                                   ?? throw new XmlException();
                                    BuildCollection(ref collectChild, ref currentIndex, xml,
                                        germane, stringBuilder);
                                    ts.SetValue(prop.Name, ref instance,
                                        collectChild, _settings.SerializationDepth);
                                }
                            }

                            else if (propType.IsValueType || propType == typeof(String))
                            {
                                //value type with its own tag... better be <Prop>val<Prop>
                                if (currentChar == ' ')
                                {
                                    currentChar = xml[++currentIndex];
                                    if (currentChar == '/')
                                    {
                                        // self closing do nothing value
                                        child = _instantiator.BuildDefault(prop.PropertyType, true)
                                                ?? throw new XmlException();
                                        ts.SetValue(prop.Name, ref instance,
                                            child, _settings.SerializationDepth);
                                        AdvanceUntil('>', ref currentIndex, xml);
                                        currentChar = xml[currentIndex];
                                        break;
                                    }

                                    // some special attribute
                                    stringBuilder.Append(currentChar);
                                    if (TryGetNextAttribute(ref currentIndex, xml, stringBuilder))
                                        switch (stringBuilder.GetConsumingString())
                                        {
                                            case Const.XmlNull:

                                                LoadNextString(ref currentIndex, xml, stringBuilder);
                                                //todo: what if it's false?
                                                stringBuilder.Clear();


                                                ts.SetValue(prop.Name, ref instance, null!,
                                                    _settings.SerializationDepth);

                                                AdvanceUntil('>', ref currentIndex, xml);
                                                currentChar = '>';

                                                continue;
                                        }
                                    else stringBuilder.Clear();
                                }

                                currentIndex++;
                                GetUntil(ref currentIndex, xml, stringBuilder, '<');
                                ts.SetValue(prop.Name, ref instance,
                                    _primitiveScanner.GetValue(
                                        stringBuilder.GetConsumingString(), propType, true),
                                    _settings.SerializationDepth);

                                AdvanceUntil('>', ref currentIndex, xml);
                            }

                            else
                            {
                                child ??= _instantiator.BuildDefault(prop.PropertyType, true)
                                          ?? throw new XmlException();
                                DeserializeTag(ref child, ref currentIndex, xml, prop.PropertyType,
                                    stringBuilder);

                                ts.SetValue(prop.Name, ref instance,
                                    child, _settings.SerializationDepth);
                            }
                        }

                        break;

                    case '\r':
                    case '\n':
                        currentChar = xml[++currentIndex];
                        break;

                    default:
                        throw new XmlException();
                }
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

        private static String GetNodeValue(ref Int32 currentIndex,
                                           String xml,
                                           StringBuilder sbString)
        {
            for (; currentIndex < xml.Length; currentIndex++)
            {
                var current = xml[currentIndex];

                if (current == '<')
                    break;

                sbString.Append(current);

                //if (current == ']' || current == '}')
                //    return false;
            }

            return sbString.ToString();
        }

        private String GetNextString(ref Int32 currentIndex,
                                            String xml,
                                            StringBuilder sbString)
        {
            if (!TryGetNextString(ref currentIndex, xml, sbString))
                throw new XmlException();

            return sbString.ToString();
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

        [MethodImpl(256)]
        private void LoadNextString(ref Int32 currentIndex,
                                           String xml,
                                           StringBuilder sbString)
        {
            if (!TryGetNextString(ref currentIndex, xml, sbString))
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

        private Boolean TryGetNextString(ref Int32 currentIndex,
                                                String xml,
                                                StringBuilder sbString)
        {
            if (!AdvanceUntil('"', ref currentIndex, xml))
                return false;

            currentIndex++;
            GetUntil(ref currentIndex, xml, sbString, '"');

            return true;
        }

        private readonly IInstantiator _instantiator;

        private readonly IStringPrimitiveScanner _primitiveScanner;
        private readonly ISerializerSettings _settings;
        private readonly ITypeInferrer _typeInference;
        private readonly ITypeManipulator _types;
    }
}
