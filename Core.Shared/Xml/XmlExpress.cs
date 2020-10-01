using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Das.Extensions;
using Das.Serializer.Scanners;

namespace Das.Serializer.Xml
{
    public class XmlExpress : BaseExpress
    {
        public XmlExpress(IInstantiator instantiator,
                          ITypeManipulator types,
                          ISerializerSettings settings,
                          IStringPrimitiveScanner primitiveScanner)
        {
            _instantiator = instantiator;
            _types = types;
            _settings = settings;
            _primitiveScanner = primitiveScanner;
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
                    instance.Add(
                        _primitiveScanner.GetValue(stringBuilder.GetConsumingString(), germaneType));
                }
                else
                {
                    var current = _instantiator.BuildDefault(germaneType, true) ?? throw new XmlException();
                    DeserializeTag(ref current, ref currentIndex, xml, germaneType, stringBuilder);
                    instance.Add(current);
                }
            }
        }

        public T Deserialize<T>(String xml)
        {
            var current = _instantiator.BuildDefault<T>(true);
            Object oCurrent = current ?? throw new XmlException();
            var currentIndex = 0;

            AdvanceUntil('<', ref currentIndex, xml);

            if (xml[++currentIndex] == '?')
            {
                //skip encoding header
                AdvanceUntil('>', ref currentIndex, xml);
                AdvanceUntil('<', ref currentIndex, xml);
            }

            var stringBuilder = new StringBuilder();

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

            var currentChar = xml[currentIndex];
            PropertyInfo? prop;
            Type propType;

            while (true)
                switch (currentChar)
                {
                    case '/':
                        //end of the object
                        AdvanceUntil('>', ref currentIndex, xml);
                        return;

                    case '>':
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

                            GetNextString(ref currentIndex, xml, stringBuilder);

                            if (prop != null)
                            {
                                ts.SetValue(attrib, ref instance,
                                    _primitiveScanner.GetValue(stringBuilder.ToString(), prop.PropertyType),
                                    _settings.SerializationDepth);
                            }

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
                                        {
                                            ts.SetValue(prop.Name, ref instance,
                                                _instantiator.BuildDefault(type, true),
                                                _settings.SerializationDepth);
                                        }

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
                                {
                                    // property already set and we can add to it
                                    BuildCollection(ref l, ref currentIndex, xml, 
                                        germane, stringBuilder);
                                }
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
                                    {
                                        switch (stringBuilder.GetConsumingString())
                                        {
                                            case Const.XmlNull:

                                                GetNextString(ref currentIndex, xml, stringBuilder);
                                                //todo: what if it's false?
                                                stringBuilder.Clear();

                                                    
                                                ts.SetValue(prop.Name, ref instance, null!,
                                                    _settings.SerializationDepth);

                                                AdvanceUntil('>', ref currentIndex, xml);
                                                currentChar = '>';

                                                continue;
                                        }
                                    }
                                    else stringBuilder.Clear();
                                }
                                
                                currentIndex++;
                                GetUntil(ref currentIndex, xml, stringBuilder, '<');
                                ts.SetValue(prop.Name, ref instance,
                                    _primitiveScanner.GetValue(stringBuilder.GetConsumingString(), propType),
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

        private static void GetNextString(ref Int32 currentIndex,
                                          String xml,
                                          StringBuilder sbString)
        {
            if (!AdvanceUntil('"', ref currentIndex, xml))
                throw new XmlException();

            currentIndex++;
            GetUntil(ref currentIndex, xml, sbString, '"');

            //for (currentIndex++; currentIndex < xml.Length; currentIndex++)
            //{
            //    var c = xml[currentIndex];
            //    if (c == '"')
            //    {
            //        currentIndex++;
            //        return;
            //    }

            //    sbString.Append(c);
            //}

            //throw new XmlException();
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
    }
}