using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer.Json
{
    public class JsonExpress : BaseExpress
    {
        public JsonExpress(IInstantiator instantiator,
                           ITypeManipulator types,
                           ITypeInferrer typeInference,
                           IObjectManipulator objectManipulator,
                           IStringPrimitiveScanner stringPrimitiveScanner,
                           IDynamicTypes dynamicTypes)
            : base(']', '}', types)
        {
            _instantiator = instantiator;
            _types = types;
            _typeInference = typeInference;
            _objectManipulator = objectManipulator;
            _stringPrimitiveScanner = stringPrimitiveScanner;
            _dynamicTypes = dynamicTypes;
        }

        public sealed override T Deserialize<T>(String json,
                                                ISerializerSettings settings,
                                                Object[] ctorValues)
        {
            var res = Deserialize(json, typeof(T), settings, ctorValues);
            return _objectManipulator.CastDynamic<T>(res);
        }

        public Object Deserialize(String json,
                                  Type type,
                                  ISerializerSettings settings,
                                  Object[] ctorValues)
        {
            var currentIndex = 0;
            Object? root = null;
            var sb = new StringBuilder();

            if (_typeInference.IsCollection(type))
            {
                var collectionValue = GetCollectionValue(ref currentIndex, json, type,
                    sb, null, null, ref root, ctorValues, settings);
                return collectionValue ?? throw new InvalidOperationException();
            }

            if (_typeInference.IsLeaf(type, true))
            {
                if (settings.TypeSpecificity == TypeSpecificity.All)
                {
                    root = _instantiator.BuildDefault(type, true) ?? throw new InvalidOperationException();
                    DeserializeImpl<Object>(ref root, ref currentIndex, json, type,
                        ref root, ctorValues, true, settings, sb);
                    return root!;
                }

                AdvanceUntil(ref currentIndex, json, '{');
                currentIndex++;

                return GetValue(ref currentIndex, json,
                    type, sb, null, null, ref root, ctorValues, settings)
                       ?? throw new InvalidOperationException();
            }

            var res = DeserializeImpl(ref root, ref currentIndex, json, type, 
                sb, ctorValues, true, settings);

            if (settings.TypeNotFoundBehavior == TypeNotFoundBehavior.GenerateRuntime &&
                res is RuntimeObject robj &&
                //don't dynamically build a type if we specified we want a RuntimeObject
                type != typeof(RuntimeObject)) 
                return _dynamicTypes.BuildDynamicObject(robj);

            return res;
        }

        public override IEnumerable<T> DeserializeMany<T>(String txt)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(256)]
        private static void AdvanceUntilFieldStart(ref Int32 currentIndex,
                                                   String json)
        {
            while (true)
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

        private void BuildConstructorValues(ref Int32 currentIndex,
                                            ref Object[] instance,
                                            ParameterInfo[] ctorParams,
                                            String json,
                                            StringBuilder stringBuilder,
                                            ref Object? root,
                                            Object[] ctorValues,
                                            ISerializerSettings settings)
        {
            AdvanceUntil(ref currentIndex, json, '{');

            while (TryGetNextString(ref currentIndex, json, stringBuilder))
            {
                var name = stringBuilder.ToString();
                stringBuilder.Clear();

                for (var c = 0; c < ctorParams.Length; c++)
                {
                    if (!String.Equals(ctorParams[c].Name, name,
                        StringComparison.OrdinalIgnoreCase))
                        continue;

                    AdvanceUntilFieldStart(ref currentIndex, json);

                    var fieldValue = GetValue(ref currentIndex, json,
                        ctorParams[c].ParameterType, stringBuilder, null, null,
                        ref root, ctorValues, settings);
                    instance[c] = fieldValue ?? throw new ArgumentNullException(ctorParams[c].Name);
                    stringBuilder.Clear();
                    break;
                }
            }
        }

        private Object DeserializeImpl(ref Object? root,
                                       ref Int32 currentIndex,
                                       String json,
                                       Type type,
                                       StringBuilder stringBuilder,
                                       Object[] ctorValues,
                                       Boolean isRootLevel,
                                       ISerializerSettings settings)
        {
            Object child;

            if (_typeInference.HasEmptyConstructor(type))
            {
                child = type != Const.ObjectType
                    ? _instantiator.BuildDefault(type, true)
                      ?? throw new InvalidOperationException()
                    : new RuntimeObject(_types);

                root ??= child;

                DeserializeImpl(ref child, ref currentIndex, json, type,
                    ref root, ctorValues, isRootLevel, settings, stringBuilder);
            }
            else if (_typeInference.TryGetPropertiesConstructor(type, out var ctor))
            {
                var ctorParams = ctor.GetParameters();
                var arr = new Object[ctorParams.Length];
                BuildConstructorValues(ref currentIndex, ref arr, ctorParams,
                    json, stringBuilder, ref root, ctorValues, settings);

                child = ctor.Invoke(arr);
                root ??= child;
            }
            
            else if (type == typeof(RuntimeObject))
            {
                child = new RuntimeObject(_types);
                root ??= child;

                DeserializeImpl(ref child, ref currentIndex, json, type,
                    ref root, ctorValues, isRootLevel, settings,  stringBuilder);
            }
            else throw new InvalidOperationException();

            AdvanceUntil(ref currentIndex, json, '}');
            currentIndex++;

            return child;
        }

        private void DeserializeImpl<T>(ref T instance,
                                        ref Int32 currentIndex,
                                        String json,
                                        Type type,
                                        ref Object? root,
                                        Object[] ctorValues,
                                        Boolean isRootLevel,
                                        ISerializerSettings settings,
                                        StringBuilder stringBuilder)
        {
            //var stringBuilder = new StringBuilder();

            AdvanceUntil(ref currentIndex, json, '{');

            while (TryGetNextString(ref currentIndex, json, stringBuilder))
            {

                var attributeKey = stringBuilder.ToString();
                var prop = GetProperty(type, attributeKey);

                if (prop == null)
                    switch (attributeKey)
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
                                type, stringBuilder, instance, null!, ref root, ctorValues, settings);
                            instance = (T) val!;
                            stringBuilder.Clear();

                            return;

                        case Const.RefAttr:
                            //circular dependency
                            stringBuilder.Clear();

                            var current = root ?? throw new InvalidOperationException();

                            var path = GetNextString(ref currentIndex, json, stringBuilder);
                            stringBuilder.Clear();
                            var tokens = path.Split('/');

                            for (var c = 1; c < tokens.Length; c++)
                                current = _objectManipulator.GetPropertyValue(current, tokens[c])
                                          ?? throw new InvalidOperationException();

                            instance = (T) current;
                            return;


                        default:
                            //todo: ignore the value?

                            if (instance is RuntimeObject robj)
                            {
                                var propName = stringBuilder.GetConsumingString();
                                var anonymousVal = GetValue(ref currentIndex, json,
                                    Const.ObjectType, stringBuilder, instance, null, ref root,
                                    ctorValues, settings);

                                stringBuilder.Clear();

                                if (anonymousVal != null)
                                {
                                    robj.Properties.Add(propName, new RuntimeObject(_types, anonymousVal));
                                    continue;
                                }
                            }
                            else
                            {
                                switch (settings.PropertyNotFoundBehavior)
                                {
                                    case PropertyNotFoundBehavior.Ignore:
                                        GetValue(ref currentIndex, json,
                                            Const.ObjectType, stringBuilder, instance, null, ref root,
                                            ctorValues, settings);
                                        continue;

                                    case PropertyNotFoundBehavior.ThrowException:
                                        throw new MissingMemberException(attributeKey);
                                    
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }

                            return;
                    }

                stringBuilder.Clear();

                AdvanceUntilFieldStart(ref currentIndex, json);

                var fieldValue = GetValue(ref currentIndex, json,
                    prop.PropertyType, stringBuilder, instance, prop, ref root,
                    ctorValues, settings);
                prop.SetValue(instance, fieldValue, null);

                stringBuilder.Clear();
            }

            if (isRootLevel && currentIndex < json.Length - 2)
            {
            }
        }

        [MethodImpl(256)]
        private IList GetCollection(Type type,
                                    PropertyInfo? prop,
                                    Object? parent)
        {
            if (!type.IsArray && prop != null)
                if (prop.GetValue(parent, null) is IList list)
                    return list;

            var res = type.IsArray
                ? _instantiator.BuildGenericList(_types.GetGermaneType(type))
                : _instantiator.BuildDefault(type, true);
            if (res is IList good)
                return good;

            if (res is ICollection collection &&
                _types.GetAdder(collection, type) is { } adder)
                return new ValueCollectionWrapper(collection, adder);

            throw new InvalidOperationException();
        }

        private Object? GetCollectionValue(ref Int32 currentIndex,
                                           String json,
                                           Type type,
                                           StringBuilder stringBuilder,
                                           Object? parent,
                                           PropertyInfo? prop,
                                           ref Object? root,
                                           Object[] ctorValues,
                                           ISerializerSettings settings)
        {
            var germane = _types.GetGermaneType(type);

            var next = AdvanceUntilAny(_arrayOrObjectOrNull, ref currentIndex, json);
            if (next == '[')
                currentIndex++;

            else if (next == 'n')
            {
                if (json[++currentIndex] == 'u' &&
                    json[++currentIndex] == 'l' &&
                    json[++currentIndex] == 'l')
                    return null;

                throw new InvalidOperationException();
            }
            else
            {
                currentIndex++;
                while (TryGetNextString(ref currentIndex, json, stringBuilder))
                    switch (stringBuilder.ToString())
                    {
                        case Const.TypeWrap:
                            stringBuilder.Clear();
                            var typeName = GetNextString(ref currentIndex, json, stringBuilder);
                            type = _typeInference.GetTypeFromClearName(typeName, true) ??
                                   throw new TypeLoadException(typeName);
                            stringBuilder.Clear();
                            continue;

                        case Const.Val:
                            AdvanceUntilFieldStart(ref currentIndex, json);
                            stringBuilder.Clear();
                            var val = GetValue(ref currentIndex, json,
                                type, stringBuilder, parent, prop, ref root,
                                ctorValues, settings);

                            stringBuilder.Clear();

                            return val ?? throw new InvalidOperationException();

                        default:
                            //todo: ignore the value?
                            throw new InvalidOperationException();
                    }
            }


            var collection = GetCollection(type, prop, parent);

            if (json[currentIndex] != ']')
            {
                while (true)
                {
                    var current = GetValue(ref currentIndex, json, germane,
                        stringBuilder, parent, prop, ref root, ctorValues, settings);
                    if (current == null)
                        break;

                    stringBuilder.Clear();

                    collection.Add(current);

                    if (!AdvanceUntil(ref currentIndex, json, ','))
                        break;

                    currentIndex++;
                }
            }

            AdvanceUntil(ref currentIndex, json, ']');
            currentIndex++;

            if (type.IsArray)
            {
                var arr = Array.CreateInstance(germane, collection.Count);
                collection.CopyTo(arr, 0);
                return arr;
            }

            if (collection is ValueCollectionWrapper wrapper)
                return wrapper.GetBaseCollection();

            return collection;
        }


        private static void GetNext(ref Int32 currentIndex,
                                    String json,
                                    StringBuilder stringBuilder,
                                    HashSet<Char> allowed)
        {
            SkipWhiteSpace(ref currentIndex, json);

            for (; currentIndex < json.Length; currentIndex++)
            {
                var currentChar = json[currentIndex];
                if (allowed.Contains(currentChar))
                    stringBuilder.Append(currentChar);
                else
                    return;
            }
        }

        private static void GetNextPrimitive(ref Int32 currentIndex,
                                             String json,
                                             StringBuilder stringBuilder)
        {
            SkipWhiteSpace(ref currentIndex, json);

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

        [MethodImpl(256)]
        private String GetNextString(ref Int32 currentIndex,
                                     String json,
                                     StringBuilder stringBuilder)
        {
            if (!TryGetNextString(ref currentIndex, json, stringBuilder))
                throw new InvalidOperationException();

            return stringBuilder.ToString();
        }

        [MethodImpl(256)]
        private PropertyInfo? GetProperty(Type type,
                                          String name)
        {
            return type.GetProperty(name) ?? type.GetProperty(
                _typeInference.ToPascalCase(name));
        }

        /// <summary>
        ///     Leaves the StringBuilder dirty!
        /// </summary>
        private Object? GetValue(ref Int32 currentIndex,
                                 String json,
                                 Type type,
                                 StringBuilder stringBuilder,
                                 Object? parent,
                                 PropertyInfo? prop,
                                 ref Object? root,
                                 Object[] ctorValues,
                                 ISerializerSettings settings)
        {
            if (type.IsEnum)
                return Enum.Parse(type,
                    GetNextString(ref currentIndex, json, stringBuilder));

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

                    if (stringBuilder.Length == 0 && json[currentIndex] == '"')
                    {
                        // numeric value in quotes...
                        currentIndex++;
                        GetNextPrimitive(ref currentIndex, json, stringBuilder);
                        currentIndex++;
                    }

                    return Convert.ChangeType(stringBuilder.ToString(), code,
                        CultureInfo.InvariantCulture);

                case TypeCode.Empty:
                    break;

                case TypeCode.Object:

                    if (_types.IsCollection(type))
                        return GetCollectionValue(ref currentIndex, json, type,
                            stringBuilder, parent, prop, ref root, ctorValues, settings);

                    var next = AdvanceUntilAny(_objectOrStringOrNull, ref currentIndex, json);

                    if (next == '"')
                    {
                        if (type == Const.ObjectType)
                            return GetNextString(ref currentIndex, json, stringBuilder);

                        var conv = TypeDescriptor.GetConverter(type);
                        if (conv.CanConvertFrom(Const.StrType))
                            return conv.ConvertFromInvariantString(
                                GetNextString(ref currentIndex, json, stringBuilder));

                        var strCtor = GetConstructorWithStringParam(type);
                        if (strCtor != null)
                        {
                            _singleObjectArray ??= new Object[1];
                            _singleObjectArray[0] = GetNextString(ref currentIndex, json, stringBuilder);
                            return strCtor.Invoke(_singleObjectArray);
                        }

                        throw new InvalidOperationException();
                    }

                    else if (next == 'n')
                    {
                        if (json[++currentIndex] == 'u' &&
                            json[++currentIndex] == 'l' &&
                            json[++currentIndex] == 'l')
                            return null;

                        throw new InvalidOperationException();
                    }

                    else if (next == ',' || next == '}')
                    {
                        //has to be a number...
                        stringBuilder.Clear();
                        for (var j = currentIndex - 1; j >= 0; j--)
                        {
                            next = json[j];

                            switch (next)
                            {
                                case ' ':
                                case ':':
                                    return _stringPrimitiveScanner.GetValue(
                                        stringBuilder.GetConsumingString(), type, false);

                                default:
                                    stringBuilder.Insert(0, next);
                                    break;
                            }
                        }

                        throw new InvalidOperationException();
                    }

                    else
                        // nested object
                        return DeserializeImpl(ref root, ref currentIndex,
                            json, type, stringBuilder, ctorValues, false, settings);

                case TypeCode.DBNull:
                    break;

                case TypeCode.Boolean:
                    GetNext(ref currentIndex, json, stringBuilder, _boolChars);
                    return Boolean.Parse(stringBuilder.ToString());


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

                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw new NotImplementedException();
        }

        [MethodImpl(256)]
        private static void SkipWhiteSpace(ref Int32 currentIndex,
                                           String json)
        {
            for (; currentIndex < json.Length; currentIndex++)
                switch (json[currentIndex])
                {
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                        break;
                    default:
                        return;
                }
        }

        private Boolean TryGetNextString(ref Int32 currentIndex,
                                         String json,
                                         StringBuilder sbString)
        {
            if (!AdvanceUntil(ref currentIndex, json, '"'))
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

                        case '\\':
                            sbString.Append('\\');
                            break;

                        default:
                            throw new NotImplementedException();
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

        private static readonly Char[] _objectOrStringOrNull = {'"', '{', 'n', ',', '}'};
        private static readonly Char[] _arrayOrObjectOrNull = {'[', '{', 'n'};

        private static readonly HashSet<Char> _boolChars = new(
            new[]
            {
                't',
                'r',
                'u',
                'e',
                'f',
                'a',
                'l',
                's'
            });

        private readonly IDynamicTypes _dynamicTypes;

        private readonly IInstantiator _instantiator;
        private readonly IObjectManipulator _objectManipulator;
        private readonly IStringPrimitiveScanner _stringPrimitiveScanner;
        private readonly ITypeInferrer _typeInference;
        private readonly ITypeManipulator _types;
    }
}
