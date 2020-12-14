using Das.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace Das.Serializer.Xml
{
    public partial class XmlExpress
    {
        [ThreadStatic]
        private static Int32 _iteratingIndex;

        private IEnumerable<T> IterateCollection<T>(String xml,
                                                    StringBuilder stringBuilder)
        {
            var currentIndex = _iteratingIndex;

            while (true)
            {
                GetUntil(ref currentIndex, xml, stringBuilder, '<');
                stringBuilder.Clear();

                var currentChar = xml[currentIndex];
                
                if (currentChar == '/')
                {
                    GetUntil(ref currentIndex, xml, stringBuilder, '>');
                    stringBuilder.Clear();
                    yield break;
                }

                GetCurrentTagName(ref currentIndex, xml, stringBuilder);
                
                stringBuilder.Clear();

                if (typeof(T).IsPrimitive || typeof(T) == Const.StrType)
                {
                    currentIndex++;
                    GetUntil(ref currentIndex, xml, stringBuilder, '<');
                    var current = _primitiveScanner.GetValue(
                        stringBuilder.GetConsumingString(), typeof(T));
                    if (current is T good)
                        yield return good;
                    else throw new InvalidOleVariantTypeException();


                }
                else if (_typeInference.HasEmptyConstructor(typeof(T)))
                {
                    
                    var current = _instantiator.BuildDefault<T>(true) ?? throw new XmlException();
                    Object oCurrent = current;
                    DeserializeTag(ref oCurrent, ref currentIndex, xml, typeof(T), stringBuilder);
                    yield return current;
                }
                else if (_typeInference.TryGetPropertiesConstructor(typeof(T), out var ctor))
                {
                    var ctorParams = ctor.GetParameters();
                    var bldr = new ValueArgsBuilder(ctorParams);
                    Object nada = null!;
                    DeserializeTagImpl(ref nada, ref currentIndex, xml, typeof(T),
                        stringBuilder, bldr);


                    var res = ctor.Invoke(bldr.Values);
                    if (res is T good)
                        yield return good;
                    else throw new InvalidOperationException();
                }
            }
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
            //var ts = _types.GetTypeStructure(type, _settings);

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

    }
}
