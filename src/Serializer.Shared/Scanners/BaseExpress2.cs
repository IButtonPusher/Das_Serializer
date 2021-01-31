using System;
using System.Collections;
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
                               Char[] fieldStartChars,
                               String nullPrimitiveAttribute)
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
            _nullPrimitiveAttribute = nullPrimitiveAttribute;
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

            return _objectManipulator.CastDynamic<T>(res);
        }

        protected abstract Boolean AdvanceScanState(String txt,
                                                    ref Int32 currentIndex,
                                                    StringBuilder stringBuilder,
                                                    ref NodeScanState scanState);

        protected abstract void AdvanceScanStateToNodeClose(String txt,
                                                            ref Int32 currentIndex,
                                                            StringBuilder stringBuilder,
                                                            ref NodeScanState scanState);

        protected abstract void AdvanceScanStateToNodeNameRead(String txt,
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


        protected Object? DeserializeNode(String txt,
                                          ref Int32 currentIndex,
                                          StringBuilder stringBuilder,
                                          Type? specifiedType,
                                          ISerializerSettings settings,
                                          Object[] ctorValues,
                                          ref NodeScanState nodeScanState,
                                          Object? parent,
                                          PropertyInfo? nodeIsProperty,
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
                        child = new RuntimeObject();

                        break;

                    case NodeTypes.Primitive:

                        if (nodeScanState == NodeScanState.AttributeNameRead)
                        {
                            switch (stringBuilder.GetConsumingString())
                            {
                                //case Const.XmlXsiAttribute:
                                default: //todo: do we ever care about an attribute value here?
                                    AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                                    stringBuilder.Clear();
                                    break;

                                //default:
                                //    throw new NotImplementedException();
                            }
                        }

                        var code = Type.GetTypeCode(specifiedType);

                        // load the node inner text into the sb
                        AdvanceScanStateToNodeOpened(txt, ref currentIndex, stringBuilder,
                            ref nodeScanState);
                        stringBuilder.Clear();

                        if (nodeScanState != NodeScanState.NodeSelfClosed)
                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

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
                                {
                                    child = dtGood;
                                }
                                else child = DateTime.MinValue;
                                //child = DateTime.Parse(stringBuilder.GetConsumingString());

                                return child;

                            default:
                                throw new NotImplementedException();
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

                        var conv = _types.GetTypeConverter(specifiedType);

                        AdvanceScanStateUntil(txt, ref currentIndex, stringBuilder,
                            NodeScanState.StartOfNodeClose, ref nodeScanState);

                        return conv.ConvertFrom(stringBuilder.GetConsumingString());

                    default:
                        throw new NotImplementedException();
                }

                root ??= child;
                var propsSet = 0;

                while (true)
                {
                    String propName;
                    PropertyInfo? prop = null;

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


                            prop = GetProperty(specifiedType, propName);

                            if (prop != null)
                            {
                                var propVal = _primitiveScanner.GetValue(stringBuilder.GetConsumingString(),
                                    prop.PropertyType, false);

                                if (isSetProps)
                                    prop.SetValue(child, propVal, null);
                                else
                                    ((RuntimeObject) child!).Properties.Add(propName,
                                        new RuntimeObject(propVal));

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
                            if (propName == "PlayerLabel")
                            {
                            }

                            Type? propType = null;
                            if (nodeType != NodeTypes.Dynamic)
                            {
                                prop = GetProperty(specifiedType, propName);
                                propType = prop?.PropertyType;
                            }

                            var nodePropVal = DeserializeNode(txt,
                                ref currentIndex, stringBuilder, propType, settings, _emptyCtorValues,
                                ref nodeScanState, child, prop, root, false);

                            if (isSetProps)
                            {
                                if (prop is { } p && p.CanWrite)
                                    p.SetValue(child, nodePropVal, null);
                            }
                            else
                                ((RuntimeObject) child!).Properties.Add(propName,
                                    new RuntimeObject(nodePropVal!));

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
                        return _dynamicTypes.BuildDynamicObject((RuntimeObject) child!);

                    case NodeTypes.PropertiesToConstructor:
                        _typeInference.TryGetPropertiesConstructor(specifiedType, out var ctor);
                        var ctorParams = ctor.GetParameters();
                        var arr = new Object?[ctorParams.Length];

                        var robj = (RuntimeObject) child;
                        for (var c = 0; c < ctorParams.Length; c++)
                            if (robj.Properties.TryGetValue(ctorParams[c].Name, out var found) ||
                                robj.Properties.TryGetValue(
                                    _typeInference.ToPascalCase(
                                        ctorParams[c].Name), out found))
                                arr[c] = found.PrimitiveValue;

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


        protected abstract void HandleEncodingNode(String txt,
                                                   ref Int32 currentIndex,
                                                   StringBuilder stringBuilder,
                                                   ref NodeScanState nodeScanState);

        protected abstract Boolean IsCollectionHasMoreItems(ref Int32 currentIndex,
                                                            String txt);

        protected abstract void LoadNextPrimitive(ref Int32 currentIndex,
                                                  String txt,
                                                  StringBuilder stringBuilder);

        protected abstract NodeTypes OpenNode(String txt,
                                              ref Int32 currentIndex,
                                              ref Type? specifiedType,
                                              ref NodeScanState nodeScanState,
                                              StringBuilder stringBuilder,
                                              Boolean canBeEncodingNode);

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

        private Object GetCollectionValue(ref Int32 currentIndex,
                                          String txt,
                                          Type type,
                                          StringBuilder stringBuilder,
                                          Object? parent,
                                          PropertyInfo? prop,
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

        [MethodImpl(256)]
        private PropertyInfo? GetProperty(Type type,
                                          String name)
        {
            return type.GetProperty(name) ?? type.GetProperty(
                _typeInference.ToPascalCase(name));
        }

        protected static readonly Object[] _emptyCtorValues = new Object[0];

        private readonly String _circularReferenceAttribute;
        private readonly IDynamicTypes _dynamicTypes;
        protected readonly Char[] _fieldStartChars;

        protected readonly IInstantiator _instantiator;
        private readonly String _nullPrimitiveAttribute;
        protected readonly IObjectManipulator _objectManipulator;


        protected readonly IStringPrimitiveScanner _primitiveScanner;
        protected readonly ITypeInferrer _typeInference;
        protected readonly ITypeManipulator _types;
        protected readonly String _typeWrapAttribute;
    }
}
