using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer.Scanners
{
    public abstract class BaseExpress2 : BaseExpress
    {
        protected BaseExpress2(IInstantiator instantiator,
                               IObjectManipulator objectManipulator,
                               ITypeInferrer typeInference,
                               ITypeManipulator types,
                               IStringPrimitiveScanner primitiveScanner,
                               IDynamicTypes dynamicTypes,
                               Char endBlockChar,
                               Char endArrayChar,
                               String typeWrapAttribute,
                               String circularReferenceAttribute,
                               Char[] fieldStartChars)
            : base(endArrayChar, endBlockChar, types)
        {
            _instantiator = instantiator;
            _objectManipulator = objectManipulator;
            _typeInference = typeInference;
            _types = types;
            _primitiveScanner = primitiveScanner;
            _dynamicTypes = dynamicTypes;
            _typeWrapAttribute = typeWrapAttribute;
            _circularReferenceAttribute = circularReferenceAttribute;

            _fieldStartChars = fieldStartChars;
        }

        public sealed override T Deserialize<T>(String txt,
                                                ISerializerSettings settings,
                                                Object[] ctorValues)
        {
            var currentIndex = 0;
            var sb = new StringBuilder();

            var noneState = NodeScanState.None;
            var res = DeserializeNode(txt, ref currentIndex, sb,
                          typeof(T), settings, ctorValues, ref noneState, null, null, null)
                      ?? throw new NullReferenceException();

            return _objectManipulator.CastDynamic<T>(res);
        }

        protected abstract void AdvanceScanState(String txt,
                                                 ref Int32 currentIndex,
                                                 StringBuilder stringBuilder,
                                                 ref NodeScanState scanState);

        protected abstract void AdvanceScanStateToNodeClose(String txt,
                                                            ref Int32 currentIndex,
                                                            StringBuilder stringBuilder,
                                                            ref NodeScanState scanState);

        protected abstract void AdvanceScanStateToNodeOpened(String txt,
                                                             ref Int32 currentIndex,
                                                             StringBuilder stringBuilder,
                                                             ref NodeScanState scanState);

        protected abstract void AdvanceScanStateUntil(String txt,
                                                      ref Int32 currentIndex,
                                                      StringBuilder stringBuilder,
                                                      NodeScanState targetState,
                                                      ref NodeScanState scanState);

        protected abstract void AdvanceUntilEndOfNode(ref Int32 currentIndex,
                                                      String txt);


        protected abstract void EnsurePropertyValType(ref Int32 currentIndex,
                                                      String txt,
                                                      StringBuilder stringBuilder,
                                                      ref Type? propvalType);

        protected abstract NodeTypes GetNodeInstanceType(ref Int32 currentIndex,
                                                         String txt,
                                                         StringBuilder stringBuilder,
                                                         ref Type? specifiedType,
                                                         ref NodeScanState nodeScanState);

        protected Type GetTypeFromText(ref Int32 currentIndex,
                                       String txt,
                                       StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
            var typeName = GetNextString(ref currentIndex, txt, stringBuilder);
            var type = _typeInference.GetTypeFromClearName(typeName, true) ??
                       throw new TypeLoadException(typeName);
            stringBuilder.Clear();

            return type;
        }


        protected abstract void HandleEncodingNode(String txt,
                                                   ref Int32 currentIndex,
                                                   StringBuilder stringBuilder,
                                                   ref NodeScanState nodeScanState);

        protected abstract Boolean IsCollectionHasMoreItems(ref Int32 currentIndex,
                                                            String txt);

        protected abstract void LoadNextPrimitive(ref Int32 currentIndex,
                                                  String txt,
                                                  StringBuilder stringBuilder);

        [MethodImpl(256)]
        protected static void SkipWhiteSpace(ref Int32 currentIndex,
                                             String txt)
        {
            for (; currentIndex < txt.Length; currentIndex++)
                switch (txt[currentIndex])
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

        protected abstract Boolean TryGetNextString(ref Int32 currentIndex,
                                                    String txt,
                                                    StringBuilder sbString);

        private void BuildConstructorValues(ref Int32 currentIndex,
                                            ref Object[] instance,
                                            ParameterInfo[] ctorParams,
                                            String txt,
                                            StringBuilder stringBuilder,
                                            ISerializerSettings settings,
                                            Object? root)
        {
            var nodeScanState = NodeScanState.None;
            var found = 0;

            while (true)
            {
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                String? name;
                Object? value;

                switch (nodeScanState)
                {
                    case NodeScanState.JustOpened:
                        continue;

                    case NodeScanState.EndOfNodeOpen:
                        if (stringBuilder.Length > 0)
                            goto loadChildNode;
                        break;

                    case NodeScanState.AttributeNameRead:

                        name = stringBuilder.GetConsumingString();

                        for (var c = 0; c < ctorParams.Length; c++)
                        {
                            if (!String.Equals(ctorParams[c].Name, name,
                                StringComparison.OrdinalIgnoreCase))
                                continue;

                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                            if (nodeScanState != NodeScanState.AttributeValueRead)
                                throw new InvalidOperationException();

                            value = _primitiveScanner.GetValue(stringBuilder.GetConsumingString(),
                                ctorParams[c].ParameterType, false);

                            instance[c] = value ?? throw new ArgumentNullException(ctorParams[c].Name);

                            goto next;
                        }

                        break;

                    case NodeScanState.ReadNodeName:

                        loadChildNode:
                        name = stringBuilder.GetConsumingString();

                        for (var c = 0; c < ctorParams.Length; c++)
                        {
                            if (!String.Equals(ctorParams[c].Name, name,
                                StringComparison.OrdinalIgnoreCase))
                                continue;

                            value = DeserializeNode(txt,
                                ref currentIndex, stringBuilder, ctorParams[c].ParameterType,
                                settings, _emptyCtorValues,
                                ref nodeScanState, null, null, root);

                            instance[c] = value ?? throw new ArgumentNullException(ctorParams[c].Name);

                            goto next;
                        }

                        break;

                    case NodeScanState.NodeSelfClosed:
                    case NodeScanState.EndOfNodeClose:
                    case NodeScanState.EndOfMarkup:
                        return;
                }

                continue;

                next:
                if (++found == ctorParams.Length)
                {
                    AdvanceScanStateToNodeClose(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                    return;
                }
            }
        }

        protected Object? DeserializeNode(String txt,
                                        ref Int32 currentIndex,
                                        StringBuilder stringBuilder,
                                        Type? specifiedType,
                                        ISerializerSettings settings,
                                        Object[] ctorValues,
                                        ref NodeScanState nodeScanState,
                                        Object? parent,
                                        PropertyInfo? nodeIsProperty,
                                        Object? root)
        {
            if (nodeScanState == NodeScanState.None)
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

            if (nodeScanState == NodeScanState.EncodingNodeOpened)
                HandleEncodingNode(txt, ref currentIndex, stringBuilder, ref nodeScanState);

            specifiedType = specifiedType == Const.ObjectType
                ? default
                : specifiedType;
            var nodeType = GetNodeInstanceType(ref currentIndex, txt, stringBuilder,
                ref specifiedType, ref nodeScanState);

            if (specifiedType == null)
                throw new TypeLoadException();

            stringBuilder.Clear();

            Object? child;

            switch (nodeType)
            {
                case NodeTypes.Collection:
                    AdvanceScanStateToNodeOpened(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                    var collectionValue = GetCollectionValue(ref currentIndex, txt, specifiedType,
                        stringBuilder, parent, nodeIsProperty, settings, root);
                    return collectionValue ?? throw new InvalidOperationException();

                case NodeTypes.PropertiesToConstructor when
                    _typeInference.TryGetPropertiesConstructor(specifiedType, out var ctor):
                    var ctorParams = ctor.GetParameters();
                    var arr = new Object[ctorParams.Length];
                    BuildConstructorValues(ref currentIndex, ref arr, ctorParams,
                        txt, stringBuilder, settings, root);

                    child = ctor.Invoke(arr);

                    return child; // todo: maybe more attributes/child nodes populate more properties?

                case NodeTypes.Object:
                    child = _instantiator.BuildDefault(specifiedType, true)
                            ?? throw new InvalidOperationException();

                    break;

                case NodeTypes.Dynamic:
                    child = new RuntimeObject();

                    break;

                case NodeTypes.Primitive:

                    var code = Type.GetTypeCode(specifiedType);

                    if (nodeScanState == NodeScanState.ReadNodeName)
                        AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                    switch (code)
                    {
                        case TypeCode.String:
                            AdvanceScanStateToNodeOpened(txt, ref currentIndex, stringBuilder, 
                                ref nodeScanState);

                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                            child = _primitiveScanner.Descape(stringBuilder.GetConsumingString());

                            //var isAString = TryGetNextString(ref currentIndex, txt, stringBuilder);
                            //child = !isAString
                            //    ? default
                            //    : _primitiveScanner.Descape(stringBuilder.GetConsumingString());

                            //AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                            return child;

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

                            LoadNextPrimitive(ref currentIndex, txt, stringBuilder);

                            if (stringBuilder.Length == 0 && txt[currentIndex] == '"')
                            {
                                // numeric value in quotes...
                                currentIndex++;
                                LoadNextPrimitive(ref currentIndex, txt, stringBuilder);
                                currentIndex++;
                            }

                            AdvanceUntilEndOfNode(ref currentIndex, txt);

                            return Convert.ChangeType(stringBuilder.GetConsumingString(), code,
                                CultureInfo.InvariantCulture);

                        case TypeCode.Boolean:
                            GetNext(ref currentIndex, txt, stringBuilder, _boolChars);
                            child = Boolean.Parse(stringBuilder.ToString());
                            AdvanceUntilEndOfNode(ref currentIndex, txt);
                            return child;


                        case TypeCode.Char:
                            if (!TryGetNextString(ref currentIndex, txt, stringBuilder))
                                throw new InvalidOperationException();

                            child = stringBuilder[0];
                            AdvanceUntilEndOfNode(ref currentIndex, txt);
                            return child;

                        case TypeCode.DateTime:
                            LoadNextPrimitive(ref currentIndex, txt, stringBuilder);
                            child = DateTime.Parse(stringBuilder.GetConsumingString());
                            AdvanceUntilEndOfNode(ref currentIndex, txt);
                            return child;

                        default:
                            throw new NotImplementedException();
                    }

                case NodeTypes.StringConvertible:
                    var conv = _types.GetTypeConverter(specifiedType);

                    AdvanceScanStateUntil(txt, ref currentIndex, stringBuilder,
                        NodeScanState.EndOfNodeClose, ref nodeScanState);

                    var rdrr = stringBuilder.GetConsumingString();

                    return conv.ConvertFrom(rdrr);

                default:
                    throw new NotImplementedException();
            }

            root ??= child;
            var propsSet = 0;

            while (true)
            {
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                String propName;
                PropertyInfo? prop = null;

                switch (nodeScanState)
                {
                    case NodeScanState.JustOpened:
                        continue;

                    case NodeScanState.EndOfNodeClose:

                        if (propsSet == 0 && stringBuilder.Length > 0)
                        {
                            var strCtor = GetConstructorWithStringParam(specifiedType);
                            if (strCtor != null)
                            {
                                _singleObjectArray ??= new Object[1];
                                _singleObjectArray[0] = stringBuilder.GetConsumingString();
                                return strCtor.Invoke(_singleObjectArray);
                            }
                        }

                        goto endOfObject;

                    case NodeScanState.EndOfNodeOpen:
                        if (stringBuilder.Length > 0)
                            goto loadChildNode;
                        break;

                    case NodeScanState.AttributeNameRead:

                        propName = stringBuilder.GetConsumingString();

                        AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                        if (nodeScanState != NodeScanState.AttributeValueRead)
                            throw new InvalidOperationException();

                        if (nodeType != NodeTypes.Dynamic)
                        {
                            prop = GetProperty(specifiedType, propName);

                            if (prop != null)
                            {
                                var propVal = _primitiveScanner.GetValue(stringBuilder.GetConsumingString(),
                                    prop.PropertyType, false);
                                
                                prop.SetValue(child, propVal, null);
                                propsSet++;
                            }
                            else
                            {
                                if (propName == _circularReferenceAttribute)
                                {
                                    if (root == null)
                                        throw new InvalidOperationException();

                                    //AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                                    child = GetFromXPath(root, stringBuilder.GetConsumingString(),
                                        stringBuilder);
                                    return child;
                                }

                                stringBuilder.Clear();
                            }
                        }

                        break;

                    case NodeScanState.ReadNodeName:

                        loadChildNode:
                        propName = stringBuilder.GetConsumingString();

                        Type? propType = null;
                        if (nodeType != NodeTypes.Dynamic)
                        {
                            prop = GetProperty(specifiedType, propName);
                            propType = prop?.PropertyType;
                            EnsurePropertyValType(ref currentIndex, txt, stringBuilder, ref propType);
                        }

                        var nodePropVal = DeserializeNode(txt,
                            ref currentIndex, stringBuilder, propType, settings, _emptyCtorValues,
                            ref nodeScanState, child, prop, root);

                        if (nodeType != NodeTypes.Dynamic)
                            prop?.SetValue(child, nodePropVal, null);
                        else
                            ((RuntimeObject) child!).Properties.Add(propName,
                                new RuntimeObject(nodePropVal!));

                        propsSet++;

                        break;

                    case NodeScanState.EndOfMarkup:
                        return child;
                }
            }

            endOfObject:
            if (nodeType != NodeTypes.Dynamic)
                return child;

            return _dynamicTypes.BuildDynamicObject((RuntimeObject) child!);
        }

        private Object GetCollectionValue(ref Int32 currentIndex,
                                           String txt,
                                           Type type,
                                           StringBuilder stringBuilder,
                                           Object? parent,
                                           PropertyInfo? prop,
                                           ISerializerSettings settings,
                                           Object? root)
        {
            var germane = _types.GetGermaneType(type);

            var collection = GetEmptyCollection(type, prop, parent);

            while (true)
            {
                SkipWhiteSpace(ref currentIndex, txt);

                if (!IsCollectionHasMoreItems(ref currentIndex, txt))
                    break;

                var noneState = NodeScanState.None;

                var current = DeserializeNode(txt, ref currentIndex, stringBuilder, germane, settings,
                    _emptyCtorValues, ref noneState, parent, prop, root);

                if (current == null)
                    break;

                stringBuilder.Clear();

                collection.Add(current);
            }

            AdvanceUntil(']', ref currentIndex, txt);
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

        [MethodImpl(256)]
        private IList GetEmptyCollection(Type collectionType,
                                         PropertyInfo? collectionIsProperty,
                                         Object? parent)
        {
            if (!collectionType.IsArray && collectionIsProperty != null)
            {
                var pVal = collectionIsProperty.GetValue(parent, null);

                if (pVal is IList list)
                    return list;
            }

            var res = collectionType.IsArray
                ? _instantiator.BuildGenericList(_types.GetGermaneType(collectionType))
                : _instantiator.BuildDefault(collectionType, true);
            if (res is IList good)
                return good;

            if (res is ICollection collection &&
                _types.GetAdder(collection, collectionType) is { } adder)
                return new ValueCollectionWrapper(collection, adder);

            throw new InvalidOperationException();
        }


        private static void GetNext(ref Int32 currentIndex,
                                    String txt,
                                    StringBuilder stringBuilder,
                                    HashSet<Char> allowed)
        {
            SkipWhiteSpace(ref currentIndex, txt);

            for (; currentIndex < txt.Length; currentIndex++)
            {
                var currentChar = txt[currentIndex];
                if (allowed.Contains(currentChar))
                    stringBuilder.Append(currentChar);
                else
                    return;
            }
        }

        [MethodImpl(256)]
        private String GetNextString(ref Int32 currentIndex,
                                     String txt,
                                     StringBuilder stringBuilder)
        {
            if (!TryGetNextString(ref currentIndex, txt, stringBuilder))
                throw new InvalidOperationException();

            return stringBuilder.ToString();
        }

        [MethodImpl(256)]
        private PropertyInfo? GetProperty(Type type,
                                          String name)
        {
            return type.GetProperty(name) ?? type.GetProperty(
                _typeInference.ToPropertyStyle(name));
        }

        protected static readonly Object[] _emptyCtorValues = new Object[0];

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

        private readonly String _circularReferenceAttribute;
        private readonly IDynamicTypes _dynamicTypes;
        protected readonly Char[] _fieldStartChars;

        protected readonly IInstantiator _instantiator;
        protected readonly IObjectManipulator _objectManipulator;


        protected readonly IStringPrimitiveScanner _primitiveScanner;
        protected readonly ITypeInferrer _typeInference;
        protected readonly ITypeManipulator _types;
        protected readonly String _typeWrapAttribute;
    }
}
