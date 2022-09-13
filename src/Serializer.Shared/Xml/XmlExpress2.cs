using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.State;

namespace Das.Serializer.Xml
{
    public partial class XmlExpress2 : BaseExpress
    {
        public XmlExpress2(IInstantiator instantiator,
                           ITypeManipulator types,
                           IObjectManipulator objectManipulator,
                           IStringPrimitiveScanner primitiveScanner,
                           ITypeInferrer typeInference,
                           ISerializerSettings settings,
                           IDynamicTypes dynamicTypes)
            : base(ImpossibleChar, '>', types, instantiator)
            //: base(instantiator, objectManipulator, typeInference, types, primitiveScanner,
            //    dynamicTypes, '>', ImpossibleChar, Const.XmlType, Const.RefTag,
            //    new[] {'"', '>'}, Const.XmlNull)
        {
            _settings = settings;
            _objectManipulator = objectManipulator;
            _typeInference = typeInference;
            _primitiveScanner = primitiveScanner;

            _dynamicTypes = dynamicTypes;
            _typeWrapAttribute = Const.XmlType;
            _circularReferenceAttribute = Const.RefTag;

            _fieldStartChars = new[] {'"', '>'};
            _nullPrimitiveAttribute = Const.XmlNull;
        }

        public sealed override T Deserialize<T>(String txt,
                                                ISerializerSettings settings,
                                                Object[] ctorValues)
        {
            var currentIndex = 0;
            var sb = new StringBuilder();

            var noneState = NodeScanState.None;
            var res = DeserializeNode(txt, ref currentIndex, sb,
                          typeof(T), settings, ctorValues, ref noneState, null, null, null, true)
                      ?? throw new NullReferenceException();

            var cooked = _objectManipulator.CastDynamic<T>(res);
            _instantiator.OnDeserialized(cooked);
            return cooked;
        }

        public sealed override IEnumerable<T> DeserializeMany<T>(String xml)
        {
            var currentIndex = 0;
            var nodeScanState = NodeScanState.None;

            var stringBuilder = new StringBuilder();
            AdvanceScanStateToNodeOpened(xml, ref currentIndex, stringBuilder, ref nodeScanState);

            while (true)
            {
                SkipWhiteSpace(ref currentIndex, xml);

                if (!IsCollectionHasMoreItems(ref currentIndex, xml))
                    break;

                var noneState = NodeScanState.None;

                var current = DeserializeNode(xml, ref currentIndex, stringBuilder, typeof(T), _settings,
                    _emptyCtorValues, ref noneState, null, null, null, false);

                if (current is T good)
                    yield return good;
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
                                          IPropertyAccessor? nodeIsProperty,
                                          Object? root,
                                          Boolean canBeEncodingNode)
        {
            var nodeType = OpenNode(txt, ref currentIndex, ref specifiedType, ref nodeScanState,
                stringBuilder, canBeEncodingNode);

            var isSetProps = nodeType != NodeTypes.Dynamic &&
                             nodeType != NodeTypes.PropertiesToConstructor;

            if (specifiedType == null)
                throw new TypeLoadException();

            try
            {
                Object? child;

                switch (nodeType)
                {
                    case NodeTypes.Collection:
                        AdvanceScanStateToNodeOpened(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                        var collectionValue = GetCollectionValue(ref currentIndex, txt, specifiedType,
                            stringBuilder, parent, nodeIsProperty, settings, root, ref nodeScanState);
                        return collectionValue ?? throw new InvalidOperationException();

                    case NodeTypes.Object:
                        child = _instantiator.BuildDefault(specifiedType, true)
                                ?? throw new InvalidOperationException();

                        break;

                    case NodeTypes.Dynamic:
                    case NodeTypes.PropertiesToConstructor:
                        child = new RuntimeObject(_types);
                        break;

                    case NodeTypes.Primitive:

                        if (nodeScanState == NodeScanState.AttributeNameRead)
                            switch (stringBuilder.GetConsumingString())
                            {
                                default: //todo: do we ever care about an attribute value here?
                                    AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                                    stringBuilder.Clear();
                                    break;
                            }

                        var code = Type.GetTypeCode(specifiedType);

                        // load the node inner text into the sb
                        AdvanceScanStateToNodeOpened(txt, ref currentIndex, stringBuilder,
                            ref nodeScanState);
                        
                        stringBuilder.Clear();

                        if (nodeScanState != NodeScanState.NodeSelfClosed)
                        {
                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                            if (nodeScanState == NodeScanState.JustOpened)
                            {
                                // a primitive value needlessly wrapped in another tag 
                                // e.g. <item><key><string>why!?</string></key></item>
                                child = DeserializeNode(txt, ref currentIndex, stringBuilder, specifiedType,
                                    settings, ctorValues, ref nodeScanState, parent, nodeIsProperty,
                                    root, false);
                                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                                return child;
                            }
                        }

                        switch (code)
                        {
                            case TypeCode.String:
                                child = _primitiveScanner.Descape(stringBuilder.GetConsumingString());

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
                                return _primitiveScanner.GetValue(stringBuilder.GetConsumingString(),
                                    specifiedType, false);

                            case TypeCode.Boolean:
                                child = Boolean.Parse(stringBuilder.ToString());
                                return child;

                            case TypeCode.Char:
                                if (!TryGetNextString(ref currentIndex, txt, stringBuilder))
                                    throw new InvalidOperationException();

                                child = stringBuilder[0];

                                return child;

                            case TypeCode.DateTime:
                                LoadNextPrimitive(ref currentIndex, txt, stringBuilder);
                                if (DateTime.TryParse(stringBuilder.GetConsumingString(),
                                    out var dtGood))
                                    child = dtGood;
                                else child = DateTime.MinValue;

                                return child;

                            default:
                                throw new NotSupportedException($"Type {code} cannot be deserialized as a {nodeType}");
                        }

                    case NodeTypes.StringConvertible:

                        if (nodeScanState == NodeScanState.AttributeNameRead &&
                            stringBuilder.GetConsumingString() == _nullPrimitiveAttribute)
                        {
                            // nullable value type (probably null...)
                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                            if (nodeScanState == NodeScanState.AttributeValueRead &&
                                String.Equals(stringBuilder.GetConsumingString(), "true",
                                    StringComparison.OrdinalIgnoreCase))
                                return null;
                        }

                        AdvanceScanStateUntil(txt, ref currentIndex, stringBuilder,
                            NodeScanState.StartOfNodeClose, ref nodeScanState);

                        return _types.ConvertTo(stringBuilder.GetConsumingString(), specifiedType);

                    default:
                        throw new NotSupportedException($"{nodeType} cannot be deserialized in this context");
                }

                root ??= child;
                var propsSet = 0;

                var typeStruct = _types.GetTypeStructure(specifiedType);

                while (true)
                {
                    String propName;
                    IPropertyAccessor? prop = null;

                    switch (nodeScanState)
                    {
                        case NodeScanState.JustOpened:
                            break;

                        case NodeScanState.StartOfNodeClose:
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

                            if (nodeType == NodeTypes.Dynamic)
                                throw new NotSupportedException();


                            prop = GetProperty(typeStruct, propName);

                            if (prop != null)
                            {
                                var propVal = _primitiveScanner.GetValue(stringBuilder.GetConsumingString(),
                                    prop.PropertyType, false);

                                if (isSetProps)
                                {
                                    if (prop.CanWrite)
                                        prop.SetPropertyValue(ref child, propVal);
                                }
                                else
                                    ((RuntimeObject) child).Properties.Add(propName,
                                        new RuntimeObject(_types, propVal));

                                propsSet++;
                            }
                            else
                            {
                                if (propName == _circularReferenceAttribute)
                                {
                                    if (root == null)
                                        throw new InvalidOperationException();

                                    child = GetFromXPath(root, stringBuilder.GetConsumingString(),
                                        stringBuilder);
                                    return child;
                                }
                                else if (propName == _nullPrimitiveAttribute)
                                {
                                }

                                stringBuilder.Clear();
                            }


                            break;

                        case NodeScanState.ReadNodeName:

                            loadChildNode:
                            propName = stringBuilder.GetConsumingString();

                            Type? propType = null;
                            if (nodeType != NodeTypes.Dynamic)
                            {
                                prop = GetProperty(typeStruct, propName);
                                propType = prop?.PropertyType;
                            }

                            var nodePropVal = DeserializeNode(txt,
                                ref currentIndex, stringBuilder, propType, settings, _emptyCtorValues,
                                ref nodeScanState, child, prop, root, false);

                            if (isSetProps)
                            {
                                if (prop is { } p && p.CanWrite)
                                    p.SetPropertyValue(ref child, nodePropVal);
                            }
                            else
                                ((RuntimeObject) child).Properties.Add(propName,
                                    new RuntimeObject(_types, nodePropVal!));

                            propsSet++;

                            break;

                        case NodeScanState.EndOfMarkup:
                        case NodeScanState.NodeSelfClosed:
                            goto endOfObject;
                    }

                    stringBuilder.Clear();
                    AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                }

                endOfObject:
                switch (nodeType)
                {
                    case NodeTypes.Dynamic:
                        return _dynamicTypes.BuildDynamicObject((RuntimeObject) child);

                    case NodeTypes.PropertiesToConstructor:
                        _typeInference.TryGetPropertiesConstructor(specifiedType, out var ctor);
                        var ctorParams = ctor.GetParameters();
                        var arr = new Object?[ctorParams.Length];

                        var robj = (RuntimeObject) child;
                        for (var c = 0; c < ctorParams.Length; c++)
                        {
                            var ctorParam = ctorParams[c];
                            var pName = ctorParam.Name ?? throw new ArgumentException($"{ctor} parameter {c} has no name");

                            if (robj.Properties.TryGetValue(pName, out var found) ||
                                robj.Properties.TryGetValue(_typeInference.ToPascalCase(pName), out found))
                            {
                                if (found.PrimitiveValue is { } letsUse &&
                                    !ctorParam.ParameterType.IsAssignableFrom(found.PrimitiveValue.GetType()))
                                {
                                    if (_objectManipulator.TryCastDynamic(found.PrimitiveValue, ctorParam.ParameterType,
                                            out var casted))
                                    {
                                        letsUse = casted;
                                    }
                                }
                                else letsUse = found.PrimitiveValue;

                                arr[c] = letsUse;
                            }
                            else
                            {
                                arr[c] = _instantiator.BuildDefault(ctorParams[c].ParameterType, true);
                            }
                        }

                        return ctor.Invoke(arr);


                    default:
                        return child;
                }
            }
            finally
            {
                AdvanceScanStateToNodeClose(txt, ref currentIndex, stringBuilder, ref nodeScanState);
            }
        }


        private Object GetCollectionValue(ref Int32 currentIndex,
                                          String txt,
                                          Type type,
                                          StringBuilder stringBuilder,
                                          Object? parent,
                                          IPropertyAccessor? prop,
                                          ISerializerSettings settings,
                                          Object? root,
                                          ref NodeScanState nodeScanState)
        {
            var germane = _types.GetGermaneType(type);

            var collection = GetEmptyCollection(type, prop, parent);

            if (nodeScanState == NodeScanState.NodeSelfClosed)
                goto wrapItUp;

            while (true)
            {
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                if (nodeScanState == NodeScanState.StartOfNodeClose)
                    break;

                var current = DeserializeNode(txt, ref currentIndex, stringBuilder, germane, settings,
                    _emptyCtorValues, ref nodeScanState, parent, prop, root, false);

                if (current == null)
                    break;

                stringBuilder.Clear();

                collection.Add(current);
            }

            wrapItUp:

            if (type.IsArray)
            {
                var arr = Array.CreateInstance(germane, collection.Count);
                collection.CopyTo(arr, 0);
                return arr;
            }

            if (collection is ICollectionWrapper wrapper)
                return wrapper.GetBaseCollection();

            return collection;
        }

        [MethodImpl(256)]
        protected static IPropertyAccessor? GetProperty(ITypeStructure type,
                                                        String name)
        {
            if (type.TryGetPropertyAccessor(name, PropertyNameFormat.Default, out var prop))
                return prop;

            if (type.TryGetPropertyAccessor(name, PropertyNameFormat.PascalCase, out prop))
                return prop;

            return default;
        }

        protected void AdvanceScanStateToNodeClose(String txt,
                                                            ref Int32 currentIndex,
                                                            StringBuilder stringBuilder,
                                                            ref NodeScanState scanState)
        {
            while (scanState != NodeScanState.EndOfMarkup &&
                   scanState != NodeScanState.NodeSelfClosed &&
                   scanState != NodeScanState.EndOfNodeClose)
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);

            if (scanState == NodeScanState.NodeSelfClosed)
                currentIndex++;
        }

        protected void AdvanceScanStateToNodeNameRead(String txt,
                                                               ref Int32 currentIndex,
                                                               StringBuilder stringBuilder,
                                                               ref NodeScanState scanState)
        {
            while (scanState != NodeScanState.NodeSelfClosed &&
                   scanState != NodeScanState.EndOfNodeOpen &&
                   scanState != NodeScanState.ReadNodeName)
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
        }

        protected void AdvanceScanStateToNodeOpened(String txt,
                                                             ref Int32 currentIndex,
                                                             StringBuilder stringBuilder,
                                                             ref NodeScanState scanState)
        {
            while (scanState != NodeScanState.NodeSelfClosed &&
                   scanState != NodeScanState.EndOfNodeOpen)
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
        }

        protected void AdvanceScanStateUntil(String txt,
                                                      ref Int32 currentIndex,
                                                      StringBuilder stringBuilder,
                                                      NodeScanState targetState,
                                                      ref NodeScanState scanState)
        {
            while (scanState != targetState)
            {
                stringBuilder.Clear();
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
            }
        }


        protected void HandleEncodingNode(String txt,
                                                   ref Int32 currentIndex,
                                                   StringBuilder stringBuilder,
                                                   ref NodeScanState nodeScanState)
        {
            AdvanceScanStateUntil(txt, ref currentIndex, stringBuilder,
                NodeScanState.EncodingNodeClose, ref nodeScanState);
            stringBuilder.Clear();
            nodeScanState = NodeScanState.None;
            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
        }


        protected bool IsCollectionHasMoreItems(ref Int32 currentIndex,
                                                         String txt)
        {
            SkipWhiteSpace(ref currentIndex, txt);

            if (currentIndex + 2 >= txt.Length)
                return false;

            if (txt[currentIndex] != '<')
                return false;

            return txt[currentIndex + 1] != '/';
        }

        protected void LoadNextPrimitive(ref Int32 currentIndex,
                                                  String txt,
                                                  StringBuilder stringBuilder)
        {
            switch (txt[currentIndex])
            {
                case '>':
                    currentIndex++;
                    break;

                case '"':
                    currentIndex++;
                    break;
            }

            SkipWhiteSpace(ref currentIndex, txt);

            for (; currentIndex < txt.Length; currentIndex++)
            {
                var currentChar = txt[currentIndex];

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

                    case '"':
                    case '<':
                        currentIndex++;
                        return;


                    default:
                        return;
                }
            }
        }

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


        protected NodeTypes OpenNode(String txt,
                                     ref Int32 currentIndex,
                                     ref Type? specifiedType,
                                     ref NodeScanState nodeScanState,
                                     StringBuilder stringBuilder,
                                     Boolean canBeEncodingNode)
        {
            if (nodeScanState == NodeScanState.None &&
                !AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState))
                return NodeTypes.None;

            if (canBeEncodingNode)
            {
                if (nodeScanState == NodeScanState.JustOpened)
                    if (txt[currentIndex] == '?')
                        AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                if (nodeScanState == NodeScanState.EncodingNodeOpened)
                    HandleEncodingNode(txt, ref currentIndex, stringBuilder, ref nodeScanState);
            }

            AdvanceScanStateToNodeNameRead(txt, ref currentIndex, stringBuilder, ref nodeScanState);

            stringBuilder.Clear();

            if (nodeScanState != NodeScanState.EndOfNodeOpen)
            {
                if (!AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState))
                    return NodeTypes.None;

                var isAttributeRelevant = true;

                while (nodeScanState == NodeScanState.AttributeNameRead &&
                       isAttributeRelevant)
                    switch (stringBuilder.ToString())
                    {
                        case Const.XmlXsiAttribute:
                            // ignore the url
                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                            stringBuilder.Clear();
                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                            break;


                        case Const.XmlType:
                            stringBuilder.Clear();
                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                            var typeName = stringBuilder.GetConsumingString();
                            specifiedType = _typeInference.GetTypeFromClearName(typeName, true) ??
                                            throw new TypeLoadException(typeName);
                            break;

                        default:
                            isAttributeRelevant = false;
                            break;
                    }
            }

            specifiedType ??= Const.ObjectType;

            if (specifiedType == Const.ObjectType)
            {
                specifiedType = typeof(RuntimeObject);
                return NodeTypes.Dynamic;
            }

            if (_types.IsCollection(specifiedType))
                return NodeTypes.Collection;

            if (_typeInference.HasEmptyConstructor(specifiedType))
                return NodeTypes.Object;

            if (_types.IsLeaf(specifiedType, true))
                return NodeTypes.Primitive;

            if (_types.CanChangeType(typeof(String), specifiedType))
                return NodeTypes.StringConvertible;

            if (_typeInference.TryGetPropertiesConstructor(specifiedType, out _))
                return NodeTypes.PropertiesToConstructor;

            throw new NotSupportedException($"Could not detect a safe way to instantiate type {specifiedType}");
        }

        protected bool TryGetNextString(ref Int32 currentIndex,
                                        String xml,
                                        StringBuilder sbString)
        {
            var foundChar = xml[currentIndex];

            switch (foundChar)
            {
                case '"':
                case '>':
                    break;

                default:

                    GetUntilAny(ref currentIndex, xml, sbString, _stringEndChars, out _);
                    return true;
            }

            if (!TryAdvanceUntilAny(_beforeStringChars, ref currentIndex, xml,
                out foundChar))
                return false;

            currentIndex++;

            switch (foundChar)
            {
                case ' ':
                    return TryGetUntil(ref currentIndex, xml, sbString, '=');

                case '"':
                    GetUntil(ref currentIndex, xml, sbString, '"');
                    return true;

                case '>':
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

        private static Boolean TryGetUntil(ref Int32 currentIndex,
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
                    return true;
                }

                sbString.Append(c);
            }

            return false;
        }


        /// <summary>
        ///     ' ', ", (space, double-quote, comma>
        /// </summary>
        private static readonly Char[] _beforeStringChars = {' ', '"', '>'};

        private static readonly Char[] _stringEndChars = {'<', '"'};
        private readonly ISerializerSettings _settings;

        protected static readonly Object[] _emptyCtorValues = new Object[0];

        protected readonly String _circularReferenceAttribute;
        protected readonly IDynamicTypes _dynamicTypes;
        protected readonly Char[] _fieldStartChars;

        
        protected readonly String _nullPrimitiveAttribute;
        protected readonly IObjectManipulator _objectManipulator;


        protected readonly IStringPrimitiveScanner _primitiveScanner;
        protected readonly ITypeInferrer _typeInference;
        
        protected readonly String _typeWrapAttribute;
    }
}
