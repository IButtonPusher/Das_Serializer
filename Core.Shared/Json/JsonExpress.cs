using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Das.Serializer.Json
{
    public class JsonExpress
    {
        private readonly IInstantiator _instantiator;
        private readonly ITypeManipulator _types;
        private readonly ITypeInferrer _typeInference;

        private static readonly Char[] _objectOrString = new[] {'"', '{'};

        public JsonExpress(
            IInstantiator instantiator, 
            ITypeManipulator types, 
            ITypeInferrer typeInference)
        {
            _instantiator = instantiator;
            _types = types;
            _typeInference = typeInference;
        }

        public T Deserialize<T>(String json)
        {
            var current = _instantiator.BuildDefault<T>(true);
            var currentIndex = 0;
            DeserializeImpl(ref current, ref currentIndex, json, typeof(T));

            return current;
        }
        
        private void DeserializeImpl<T>(ref T instance, ref Int32 currentIndex , 
            String json, Type type)
        {
            var stringBuilder = new StringBuilder();

            AdvanceUntil('{', ref currentIndex, json);

            while (TryGetNextString(ref currentIndex, json, stringBuilder))
            {
                var prop = type.GetProperty(stringBuilder.ToString());

                if (prop == null)
                {
                    switch (stringBuilder.ToString())
                    {
                        case Const.TypeWrap:
                            stringBuilder.Clear();
                            var typeName = GetNextString(ref currentIndex, json, stringBuilder);
                            type = _typeInference.GetTypeFromClearName(typeName) ?? 
                                throw new TypeLoadException(typeName);
                            stringBuilder.Clear();
                            continue;

                        case Const.Val:
                            AdvanceUntilFieldStart(ref currentIndex, json);
                            stringBuilder.Clear();
                            var val = GetValue(ref currentIndex, json, 
                                type, stringBuilder, instance, null!);
                            instance = (T) val!;
                            stringBuilder.Clear();
                            
                            return;

                        default:
                            //todo: ignore the value?
                            throw new InvalidOperationException();
                    }
                }

                stringBuilder.Clear();

                AdvanceUntilFieldStart(ref currentIndex, json);

                var fieldValue = GetValue(ref currentIndex, json, 
                    prop.PropertyType, stringBuilder, instance, prop);
                prop.SetValue(instance, fieldValue, null);

                stringBuilder.Clear();
            }
        }

        private static void AdvanceUntilFieldStart(ref Int32 currentIndex, String json)
        {
            while (true)
            {
                switch (json[currentIndex++])
                {
                    case ':':
                        return;

                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        public Object Deserialize(String json, Type type)
        {
            var current = _instantiator.BuildDefault(type, true)
                ?? throw new InvalidOperationException();

            var currentIndex = 0;

            DeserializeImpl(ref current, ref currentIndex, json, type);

            return current!;
        }

        private Object? GetValue(
            ref Int32 currentIndex, 
            String json, 
            Type type, 
            StringBuilder stringBuilder,
            Object? parent,
            PropertyInfo prop)
        {
            if (type.IsEnum)
            {
                return Enum.Parse(type,
                    GetNextString(ref currentIndex, json, stringBuilder));
            }

            var code = Type.GetTypeCode(type);

            switch (code)
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    GetNextPrimitive(ref currentIndex, json, stringBuilder);

                    return Convert.ChangeType(stringBuilder.ToString(), code, 
                        CultureInfo.InvariantCulture);

                case TypeCode.Empty:
                    break;

                case TypeCode.Object:

                    if (_types.IsCollection(type))
                    {
                        var collection = GetCollection(prop, parent);

                        var germane = _types.GetGermaneType(type);

                        AdvanceUntil('[', ref currentIndex, json);

                        while (true)
                        {
                            var current = GetValue(ref currentIndex, json, germane,
                                stringBuilder, parent, prop);
                            if (current == null)
                                break;

                            stringBuilder.Clear();

                            collection.Add(current);

                            if (!AdvanceUntil(',', ref currentIndex, json))
                                break;

                            currentIndex++;
                        }

                        if (type.IsArray)
                        {
                            var arr = Array.CreateInstance(germane, collection.Count);
                            collection.CopyTo(arr, 0);
                            return arr;
                        }

                        return collection;
                    }

                    var next = AdvanceUntilAny(_objectOrString, ref currentIndex, json);

                    if (next == '"')
                    {
                        var conv = TypeDescriptor.GetConverter(type);
                        return conv.ConvertFromInvariantString(
                            GetNextString(ref currentIndex, json, stringBuilder));
                    }

                    else
                    {
                        var child = _instantiator.BuildDefault(type, true)
                                      ?? throw new InvalidOperationException();
                        DeserializeImpl(ref child, ref currentIndex, json, type);
                        AdvanceUntil('}', ref currentIndex, json);
                        currentIndex++;
                        return child;
                    }

                case TypeCode.DBNull:
                    break;
                
                case TypeCode.Boolean:
                    break;
                
                case TypeCode.Char:
                    if (!TryGetNextString(ref currentIndex, json, stringBuilder))
                        throw new InvalidOperationException();

                    return stringBuilder[0];

                case TypeCode.DateTime:
                    return DateTime.Parse(GetNextString(ref currentIndex, json, stringBuilder));

                case TypeCode.String:
                    return !TryGetNextString(ref currentIndex, json, stringBuilder) 
                        ? default
                        : stringBuilder.ToString();

                //return GetNextString(ref currentIndex, json, stringBuilder);

                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw new NotImplementedException();
        }

        [MethodImpl(256)]
        private IList GetCollection(PropertyInfo prop, Object? parent)
        {
            var type = prop.PropertyType;

            if (!type.IsArray)
            {
                if (prop.GetValue(parent, null) is IList list)
                    return list;
            }

            var germane = _types.GetGermaneType(type);

            return (type.IsArray
                                 ? _instantiator.BuildGenericList(germane)
                                 : _instantiator.BuildDefault(type, true) as IList)
                             ?? throw new InvalidOperationException();
        }

        [MethodImpl(256)]
        private static String GetNextString(ref Int32 currentIndex,
            String json, StringBuilder stringBuilder)
        {
            if (!TryGetNextString(ref currentIndex, json, stringBuilder))
                throw new InvalidOperationException();

            return stringBuilder.ToString();
        }

        private static Boolean TryGetNextString(ref Int32 currentIndex, 
            String json, StringBuilder sbString)
        {
            if (!AdvanceUntil('"', ref currentIndex, json))
                return false;

            for (currentIndex++; currentIndex < json.Length; currentIndex++)
            {
               var c = json[currentIndex];

                if (c == 92)
                {
                    currentIndex++;
                    c = json[currentIndex];

                    switch (c)
                    {
                        case 'b':
                            sbString.Append('\b');
                            break;
                        case 't':
                            sbString.Append('\t');
                            break;
                        case 'n':
                            sbString.Append('\n');
                            break;
                        case 'f':
                            sbString.Append('\f');
                            break;
                        case 'r':
                            sbString.Append(Const.CarriageReturn);
                            break;
                        case '"':
                            sbString.Append(Const.Quote);
                            break;
                        case 'u':
                            var unicode = json.Substring(currentIndex + 1, 4);
                            var unichar = Convert.ToInt32(unicode, 16);
                            sbString.Append((Char) unichar);

                            currentIndex += 4;
                            break;
                    }
                }
                else if (c == '\"')
                {
                    currentIndex++;
                    return true;
                }

                else
                    sbString.Append(c);
            }

            return false;
        }

        [MethodImpl(256)]
        private static Boolean AdvanceUntil(Char target, ref Int32 currentIndex, String json)
        {
            for (; currentIndex < json.Length; currentIndex++)
            {
                var current = json[currentIndex];

                if (current == target)
                    return true;

                if (current == ']' || current == '}')
                    return false;
            }

            return false;
        }

        [MethodImpl(256)]
        private static Char AdvanceUntilAny(Char[] targets, ref Int32 currentIndex, String json)
        {
            for (; currentIndex < json.Length; currentIndex++)
            {
                var current = json[currentIndex];

                for (var k = 0; k < targets.Length; k++)
                {
                    if (current == targets[k])
                        return current;
                }
            }

            throw new InvalidOperationException();
        }


        private static void GetNextPrimitive(
            ref Int32 currentIndex,
            String json,
            StringBuilder stringBuilder)
        {
            for (; currentIndex < json.Length; currentIndex++)
            {
                switch (json[currentIndex])
                {
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                        break;
                    default:
                        goto next;
                }
            }

            next:

            for (; currentIndex < json.Length; currentIndex++)
            {
                var currentChar = json[currentIndex];

                if (Char.IsDigit(currentChar))
                {
                    stringBuilder.Append(currentChar);
                    continue;
                }

                switch (currentChar)
                {
                    case 'E':
                    case 'e':
                    case '-':
                    case '.':
                        stringBuilder.Append(currentChar);
                        break;
                    default:
                        return;
                }

            }
        }
    }
}
