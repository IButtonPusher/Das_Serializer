using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
                               Char startBlockChar,
                               Char endBlockChar,
                               Char endArrayChar,
                               String typeWrapAttribute,
                               String circularReferenceAttribute,
                               Char[] objectOrStringOrNull,
                               Char[] arrayOrObjectOrNull,
                               Char[] fieldStartChars)
        : base(endArrayChar, endBlockChar)
        {
            _instantiator = instantiator;
            _objectManipulator = objectManipulator;
            _typeInference = typeInference;
            _types = types;
            _primitiveScanner = primitiveScanner;
            _dynamicTypes = dynamicTypes;
            _startBlockChar = startBlockChar;
            _typeWrapAttribute = typeWrapAttribute;
            _circularReferenceAttribute = circularReferenceAttribute;

            _objectOrStringOrNull = objectOrStringOrNull;
            _arrayOrObjectOrNull = arrayOrObjectOrNull;
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
                typeof(T), settings, ctorValues, ref noneState, null, null);

            //var currentIndex = 0;
            //var sb = new StringBuilder();

            //var type = GetInstanceType(ref currentIndex, txt, sb, typeof(T) != Const.ObjectType
            //    ? typeof(T)
            //    : default);
            
            /////////////////////////////////////////////
            
            //Object? root = null;

            //if (_typeInference.IsCollection(type))
            //{
            //    AdvanceUntilFieldStart(ref currentIndex, txt);
            //    var collectionValue = GetCollectionValue(ref currentIndex, txt, type,
            //        sb, null, null, ref root, ctorValues);
            //    root = collectionValue ?? throw new InvalidOperationException();
            //}

            //else if (_typeInference.IsLeaf(type, true))
            //{
            //    if (settings.TypeSpecificity == TypeSpecificity.All)
            //    {
            //        root = _instantiator.BuildDefault(type, true) ?? throw new InvalidOperationException();
            //        DeserializeImpl<Object>(ref root, ref currentIndex, txt, type,
            //            ref root, ctorValues, true);
            //        //return root!;
            //    }

            //    else
            //    {
            //        AdvanceUntil(_startBlockChar, ref currentIndex, txt);
            //        currentIndex++;

            //        root = GetValue(ref currentIndex, txt,
            //            type, sb, null, null, ref root, ctorValues) ?? throw new InvalidOperationException();
            //    }
            //}

            //var res = DeserializeImpl(ref root, ref currentIndex, txt, type, sb, ctorValues, true);
            //return res;
            
            ///////////////////////////////////////////

            //var res = Deserialize(txt, instanceType, settings, ctorValues);
            return _objectManipulator.CastDynamic<T>(res);
        }

        private Object? DeserializeNode(String txt,
                                        ref Int32 currentIndex,
                                        StringBuilder stringBuilder,
                                        Type? specifiedType,
                                        ISerializerSettings settings,
                                        Object[] ctorValues,
                                        ref NodeScanState nodeScanState,
                                        Object? parent,
                                        PropertyInfo? nodeIsProperty)
        {
            if (nodeScanState == NodeScanState.None) // && txt[currentIndex++] != '<')
            {
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
            }

            if (nodeScanState == NodeScanState.EncodingNodeOpened)
                HandleEncodingNode(txt, ref currentIndex, stringBuilder, ref nodeScanState);
            

            //if (nodeScanState == NodeScanState.None)
            //    nodeScanState = AdvanceScanState(txt, ref currentIndex, stringBuilder);

            //if (txt[currentIndex] != '<')
            //    throw new InvalidOperationException();

            Object? root = null;

            specifiedType = specifiedType == Const.ObjectType
                ? default
                : specifiedType;
            var nodeType = GetNodeInstanceType(ref currentIndex, txt, stringBuilder, 
                ref specifiedType, ref nodeScanState);

            if (specifiedType == null)
                throw new TypeLoadException();

            stringBuilder.Clear();

            Object? child = null;

            switch (nodeType)
            {
                case NodeTypes.Collection:
                    //AdvanceUntilFieldStart(ref currentIndex, txt);

                    AdvanceScanStateToNodeOpened(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                    var collectionValue = GetCollectionValue(ref currentIndex, txt, specifiedType,
                        stringBuilder, parent, nodeIsProperty, ref root, ctorValues, settings);
                    return collectionValue ?? throw new InvalidOperationException();

                case NodeTypes.PropertiesToConstructor when 
                    _typeInference.TryGetPropertiesConstructor(specifiedType, out var ctor):
                    var ctorParams = ctor.GetParameters();
                    var arr = new Object[ctorParams.Length];
                    BuildConstructorValues(ref currentIndex, ref arr, ctorParams,
                        txt, stringBuilder, ref root, ctorValues, settings);

                    child = ctor.Invoke(arr);
                    root ??= child;
                    return child; // todo: maybe more attributes/child nodes populate more properties?

                case NodeTypes.Object:
                    child = _instantiator.BuildDefault(specifiedType, true)
                            ?? throw new InvalidOperationException();
                    root ??= child;
                    break;

                case NodeTypes.Dynamic:
                    child = new RuntimeObject();
                    root ??= child;
                    break;

                case NodeTypes.Primitive:

                    //AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                    var code = Type.GetTypeCode(specifiedType);

                    if (nodeScanState == NodeScanState.ReadNodeName)
                        AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);

                    //AdvanceUntilFieldStart(ref currentIndex, txt);
                    

                    switch (code)
                    {
                        case TypeCode.String:
                            var isAString = TryGetNextString(ref currentIndex, txt, stringBuilder);
                            child =  !isAString
                                ? default
                                : _primitiveScanner.Descape(stringBuilder.GetConsumingString());

                            AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                            //if (nodeScanState != NodeScanState.EndOfNode)
                            //    throw new NotImplementedException();

                            //AdvanceUntilEndOfNode(ref currentIndex, txt);

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
                            //return DateTime.Parse(getVal(ref currentIndex, txt, stringBuilder));

                        default:
                            throw new NotImplementedException();
                    }

                    break;

               case NodeTypes.StringConvertible:
                   var conv = _types.GetTypeConverter(specifiedType);
                   
                   AdvanceScanStateUntil(txt, ref currentIndex, stringBuilder, 
                       NodeScanState.EndOfNodeClose, ref nodeScanState);
                   //AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                   
                   var rdrr = stringBuilder.GetConsumingString();
                   
                   return conv.ConvertFrom(rdrr);
                   

                default:
                    throw new NotImplementedException();
            }

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
                                //var propVal = _primitiveScanner.Descape(stringBuilder.GetConsumingString());
                                prop.SetValue(child, propVal, null);
                                propsSet++;
                            }
                            else
                                stringBuilder.Clear();
                        }
                        else
                        {
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
                            ref nodeScanState, child, prop);

                        if (nodeType != NodeTypes.Dynamic)
                        {
                            //var prop = GetProperty(specifiedType, propName);
                            
                            prop?.SetValue(child, nodePropVal, null);
                        }
                        else
                        {
                            ((RuntimeObject) child!).Properties.Add(propName,
                                new RuntimeObject(nodePropVal!));
                        }

                        propsSet++;

                        break;

                    case NodeScanState.EndOfMarkup:
                        return child;
                }
                //    var propRes = TryLoadNextAttributePropertyName(txt, ref currentIndex, stringBuilder);
            //    if (propRes != NodeScanState.Found)
            //        break;

            //    throw new NotImplementedException();
            }

            

            //while (true)
            //{
            //    // values from child nodes

            //    nodeScanState = NodeScanState.None;

            //    //var nextNode = AdvanceScanState(txt, ref currentIndex, stringBuilder);
            //    //if (nextNode != NodeScanState.OpenNode)
            //    //    break;

            //    while (true)
            //    {
            //        AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
            //        if (nodeScanState == NodeScanState.JustOpened)
            //            continue;

            //        if (nodeScanState == NodeScanState.EndOfNodeClose)
            //            goto endOfObject;

            //        //if (nodeScanState == )

            //        break;
            //    }

            //    if (nodeType != NodeTypes.Dynamic)
            //    {
            //        var prop = GetProperty(specifiedType, stringBuilder.ToString());
            //    }
            //    else
            //    {
            //        var dynamicPropertyName = stringBuilder.GetConsumingString();
            //        var dynamicPropertyValue = DeserializeNode(txt,
            //            ref currentIndex, stringBuilder, null, settings, _emptyCtorValues, 
            //            ref nodeScanState);

            //        if (dynamicPropertyValue != null)
            //        {
            //            ((RuntimeObject) child!).Properties.Add(dynamicPropertyName,
            //                new RuntimeObject(dynamicPropertyValue));
            //        }
            //    }
            //}

            endOfObject:
            if (nodeType != NodeTypes.Dynamic)
                return child;

            return _dynamicTypes.BuildDynamicObject((RuntimeObject) child!);


            //if (_typeInference.IsCollection(specifiedType))
            //{
            //    AdvanceUntilFieldStart(ref currentIndex, txt);

            //    var collectionValue = GetCollectionValue(ref currentIndex, txt, specifiedType,
            //        stringBuilder, null, null, ref root, ctorValues);
            //    return collectionValue ?? throw new InvalidOperationException();
            //}

            //if (_typeInference.IsLeaf(specifiedType, true))
            //{
            //    // this shouldn't ever happen?
            //    if (settings.TypeSpecificity == TypeSpecificity.All)
            //    {
            //        root = _instantiator.BuildDefault(specifiedType, true) ?? throw new InvalidOperationException();
            //        DeserializeImpl<Object>(ref root, ref currentIndex, txt, specifiedType,
            //            ref root, ctorValues, true, settings);
            //        return root!;
            //    }

            //    AdvanceUntil(_startBlockChar, ref currentIndex, txt);
            //    currentIndex++;

            //    return GetValue(ref currentIndex, txt,
            //        specifiedType, stringBuilder, null, null, ref root, ctorValues, settings) 
            //           ?? throw new InvalidOperationException();
            //}

            //var res = DeserializeImpl(ref root, ref currentIndex, txt, specifiedType, 
            //    stringBuilder, ctorValues, true, settings);
            //return res;
        }

        protected abstract NodeTypes GetNodeInstanceType(ref Int32 currentIndex,
                                                         String txt,
                                                         StringBuilder stringBuilder,
                                                         ref Type? specifiedType,
                                                         ref NodeScanState nodeScanState);

       

        //private Object DeserializeImpl(ref Object? root,
        //                               ref Int32 currentIndex,
        //                               String txt,
        //                               Type type,
        //                               StringBuilder stringBuilder,
        //                               Object[] ctorValues,
        //                               Boolean isRootLevel,
        //                               ISerializerSettings settings)
        //{
        //    Object child;

        //    if (_typeInference.HasEmptyConstructor(type))
        //    {
        //        child = _instantiator.BuildDefault(type, true)
        //                ?? throw new InvalidOperationException();
        //        root ??= child;

        //        DeserializeImpl(ref child, ref currentIndex, txt, type,
        //            ref root, ctorValues, isRootLevel, settings);
        //    }
        //    else if (_typeInference.TryGetPropertiesConstructor(type, out var ctor))
        //    {
        //        var ctorParams = ctor.GetParameters();
        //        var arr = new Object[ctorParams.Length];
        //        BuildConstructorValues(ref currentIndex, ref arr, ctorParams,
        //            txt, stringBuilder, ref root, ctorValues, settings);

        //        child = ctor.Invoke(arr);
        //        root ??= child;
        //    }
        //    else return GetValue(ref currentIndex, txt, type, stringBuilder, 
        //        null, null, ref root, ctorValues, settings) ?? throw new InvalidOperationException();
        //    //else throw new InvalidOperationException();

        //    AdvanceUntil(_endBlockChar, ref currentIndex, txt);
        //    currentIndex++;

        //    return child;
        //}

        //protected abstract Object GetTypeWrappedValue(ref Int32 currentIndex,
        //                                              String txt,
        //                                              Type type,
        //                                              StringBuilder stringBuilder,
        //                                              Object? parent,
        //                                              PropertyInfo? prop,
        //                                              ref Object? root,
        //                                              Object[] ctorValues,
        //                                              ISerializerSettings settings);

        protected abstract void HandleEncodingNode(String txt,
                                                   ref Int32 currentIndex,
                                                   StringBuilder stringBuilder,
                                                   ref NodeScanState nodeScanState);

        protected Type GetTypeFromText(ref Int32 currentIndex,
                                       String txt,
                                       StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
            var typeName = GetNextString(ref currentIndex, txt, stringBuilder);
            var type = _typeInference.GetTypeFromClearName(typeName) ??
                   throw new TypeLoadException(typeName);
            stringBuilder.Clear();

            return type;
        }

        protected Boolean TryGetTypeFromText(ref Int32 currentIndex,
                                       String txt,
                                       StringBuilder stringBuilder,
                                       out Type foundType)
        {
            stringBuilder.Clear();
            var typeName = GetNextString(ref currentIndex, txt, stringBuilder);
            foundType = _typeInference.GetTypeFromClearName(typeName)!;
            stringBuilder.Clear();

            return foundType != null;
        }
        
        //protected abstract NodeScanState TryLoadNextAttributePropertyName(String txt,
        //                                                                    ref Int32 currentValue,
        //                                                                    StringBuilder stringBuilder);

        protected abstract void AdvanceScanState(String txt,
                                                 ref Int32 currentIndex,
                                                 StringBuilder stringBuilder,
                                                 ref NodeScanState scanState);

        protected abstract void AdvanceScanStateUntil(String txt,
                                                 ref Int32 currentIndex,
                                                 StringBuilder stringBuilder,
                                                 NodeScanState targetState,
                                                 ref NodeScanState scanState);

        protected abstract void AdvanceScanStateToNodeOpened(String txt,
                                                            ref Int32 currentIndex,
                                                            StringBuilder stringBuilder,
                                                            ref NodeScanState scanState);

        protected abstract void AdvanceScanStateToNodeClose(String txt,
                                                      ref Int32 currentIndex,
                                                      StringBuilder stringBuilder,
                                                      ref NodeScanState scanState);

        //private void DeserializeImpl<T>(ref T instance,
        //                                ref Int32 currentIndex,
        //                                String txt,
        //                                Type type,
        //                                ref Object? root,
        //                                Object[] ctorValues,
        //                                Boolean isRootLevel,
        //                                ISerializerSettings settings)
        //{
        //    var stringBuilder = new StringBuilder();

        //    AdvanceUntil(_startBlockChar, ref currentIndex, txt);

        //    while (TryLoadNextPropertyName(ref currentIndex, txt, stringBuilder))//, out var propertyValType))
        //    {
        //        var prop = GetProperty(type, stringBuilder.ToString());

        //        if (prop == null)
        //        {
        //            var sbStr = stringBuilder.ToString();

        //            if (sbStr == _typeWrapAttribute)
        //            {
        //                type = GetTypeFromText(ref currentIndex, txt, stringBuilder);

        //                var val = GetTypeWrappedValue(ref currentIndex, txt,
        //                    type, stringBuilder, instance, null!, ref root, ctorValues, settings);

        //                instance = (T) val!;
        //                stringBuilder.Clear();

        //                return;
        //            }

        //            if (sbStr == Const.Val)
        //            {
        //                AdvanceUntilFieldStart(ref currentIndex, txt);
        //                stringBuilder.Clear();
        //                var val = GetValue(ref currentIndex, txt,
        //                    type, stringBuilder, instance, null!, ref root, ctorValues, settings);
        //                instance = (T) val!;
        //                stringBuilder.Clear();

        //                return;
        //            }

        //            if (sbStr == _circularReferenceAttribute)
        //            {
        //                //circular dependency
        //                stringBuilder.Clear();

        //                var current = root ?? throw new InvalidOperationException();

        //                var path = GetNextString(ref currentIndex, txt, stringBuilder);
        //                stringBuilder.Clear();
        //                var tokens = path.Split('/');

        //                for (var c = 1; c < tokens.Length; c++)
        //                    current = _objectManipulator.GetPropertyValue(current, tokens[c])
        //                              ?? throw new InvalidOperationException();


        //                instance = (T) current;
        //                return;
        //            }

        //            // unknown property - ignore it unless this is a dynamic type
        //            if (instance is RuntimeObject robj)
        //            {
        //                var anonType = typeof(Object);

        //                var propName = stringBuilder.GetConsumingString();
        //                EnsurePropertyValType(ref currentIndex, txt, stringBuilder, ref anonType);
        //                AdvanceUntilFieldStart(ref currentIndex, txt);

        //                var anonymousVal = GetValue(ref currentIndex, txt,
        //                    anonType, stringBuilder, instance, null, ref root,
        //                    ctorValues, settings);

        //                stringBuilder.Clear();

        //                if (anonymousVal != null)
        //                {
        //                    robj.Properties.Add(propName, new RuntimeObject(anonymousVal));
        //                    continue;
        //                }
        //            }

        //            stringBuilder.Clear();

        //            LoadNextStringValue(ref currentIndex, txt, stringBuilder);
        //            stringBuilder.Clear();

        //            continue;
        //            //throw new InvalidOperationException();
        //        }

        //        if (prop.Name == "ShiftPreference")
        //        {}

        //        stringBuilder.Clear();

        //        //AdvanceUntilFieldStart(ref currentIndex, txt);

        //        var valuesType = //propertyValType ?? 
        //                         prop.PropertyType;

        //        EnsurePropertyValType(ref currentIndex, txt, stringBuilder, ref valuesType);

        //        var fieldValue = GetValue(ref currentIndex, txt,
        //            valuesType,
        //            stringBuilder, instance, prop, ref root,
        //            ctorValues, settings);
        //        prop.SetValue(instance, fieldValue, null);

        //        stringBuilder.Clear();
        //    }

        //    while (isRootLevel && currentIndex < txt.Length - 2)
        //    {
        //        // dynamic object
        //        AdvanceUntil(_startBlockChar, ref currentIndex, txt);
        //        Object? childRoot = null;
        //        var child = DeserializeImpl(ref childRoot, ref currentIndex, txt, type, stringBuilder,
        //            _emptyCtorValues, false, settings);
        //    }
        //}

        protected abstract void EnsurePropertyValType(ref Int32 currentIndex,
                                                      String txt,
                                                      StringBuilder stringBuilder,
                                                      ref Type? propvalType);

        private void BuildConstructorValues(ref Int32 currentIndex,
                                            ref Object[] instance,
                                            ParameterInfo[] ctorParams,
                                            String txt,
                                            StringBuilder stringBuilder,
                                            ref Object? root,
                                            Object[] ctorValues,
                                            ISerializerSettings settings)
        {
            var nodeScanState = NodeScanState.None;
            var found = 0;

            while (true)
            {
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                String? name = null;
                Object? value = null;

                switch (nodeScanState)
                {
                    case NodeScanState.JustOpened:
                        continue;

                    case NodeScanState.EndOfNodeClose:
                        return;

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
                                ref nodeScanState, null, null);

                            instance[c] = value ?? throw new ArgumentNullException(ctorParams[c].Name);
                           
                            goto next;
                        }

                        break;

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

            //var found = 0;

            //AdvanceUntil(_startBlockChar, ref currentIndex, txt);

            //while (TryGetNextString(ref currentIndex, txt, stringBuilder))
            //{
            //    var name = stringBuilder.ToString();
            //    stringBuilder.Clear();

            //    for (var c = 0; c < ctorParams.Length; c++)
            //    {
            //        if (!String.Equals(ctorParams[c].Name, name,
            //            StringComparison.OrdinalIgnoreCase))
            //            continue;

            //        AdvanceUntilFieldStart(ref currentIndex, txt);

            //        var fieldValue = GetValue(ref currentIndex, txt,
            //            ctorParams[c].ParameterType, stringBuilder, null, null,
            //            ref root, ctorValues, settings);
            //        instance[c] = fieldValue ?? throw new ArgumentNullException(ctorParams[c].Name);
            //        stringBuilder.Clear();

            //        if (++found == ctorParams.Length)
            //            return;

            //        break;
            //    }
            //}
        }

        //private Object? GetObjectValue(ref Int32 currentIndex,
        //                               String txt,
        //                               Type type,
        //                               StringBuilder stringBuilder,
        //                               Object? parent,
        //                               PropertyInfo? prop,
        //                               ref Object? root,
        //                               Object[] ctorValues,
        //                               GetStringValue getStringVal,
        //                               LoadStringValue loadStringValue,
        //                               ISerializerSettings settings)
        //{
        //    if (_types.IsCollection(type))

        //        return GetCollectionValue(ref currentIndex, txt, type,
        //            stringBuilder, parent, prop, ref root, ctorValues, settings);

        //    //var next = AdvanceUntilAny(_objectOrStringOrNull, ref currentIndex, txt);
        //    //AdvanceUntilFieldStart(ref currentIndex, txt);
        //    var next = txt[currentIndex];

        //    if (next == _startBlockChar)
        //    {
        //        // full node, not an attribute, not a node's value
        //        return DeserializeImpl(ref root, ref currentIndex,
        //            txt, type, stringBuilder, ctorValues, false, settings);
        //    }

        //    loadStringValue(ref currentIndex, txt, stringBuilder);


        //    //if (next == '"')
        //    //if (IsCharInArray(next, _fieldStartChars))
        //    {
        //        var conv = TypeDescriptor.GetConverter(type);
        //        if (conv.CanConvertFrom(typeof(String)))
        //            return conv.ConvertFromInvariantString(stringBuilder.GetConsumingString());

        //        //getStringVal(ref currentIndex, txt, stringBuilder));
        //    }

        //    //else if (next == 'n')
        //    //{
        //    //    if (txt[++currentIndex] == 'u' &&
        //    //        txt[++currentIndex] == 'l' &&
        //    //        txt[++currentIndex] == 'l')
        //    //        return null;

        //    //    throw new InvalidOperationException();
        //    //}

        //    //else
        //        // nested object
        //        return DeserializeImpl(ref root, ref currentIndex,
        //            txt, type, stringBuilder, ctorValues, false, settings);
        //}

        [MethodImpl(256)]
        protected static Boolean IsCharInArray(Char c,
                                               Char[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i] == c)
                    return true;
            }

            return false;
        }

        protected abstract Boolean IsCollectionHasMoreItems(ref Int32 currentIndex,
                                                            String txt);

        ///// <summary>
        /////     Leaves the StringBuilder dirty!
        ///// </summary>
        //protected Object? GetValue(ref Int32 currentIndex,
        //                         String txt,
        //                         Type type,
        //                         StringBuilder stringBuilder,
        //                         Object? parent,
        //                         PropertyInfo? prop,
        //                         ref Object? root,
        //                         Object[] ctorValues,
        //                         ISerializerSettings settings)
        //{
        //    return GetValue(ref currentIndex, txt, type, stringBuilder, parent, prop,
        //        ref root, ctorValues, GetNextString, LoadNextPrimitive, TryGetNextString,
        //        GetObjectValue, LoadNextStringValue, settings);
        //}


        /// <summary>
        ///     Leaves the StringBuilder dirty!
        /// </summary>
        protected Object? GetValue(ref Int32 currentIndex,
                                 String txt,
                                 Type type,
                                 StringBuilder stringBuilder,
                                 Object? parent,
                                 PropertyInfo? prop,
                                 ref Object? root,
                                 Object[] ctorValues,
                                 GetStringValue getVal,
                                 LoadPrimitiveValue loadPrimitive,
                                 TryGetStringValue tryGetString,
                                 GetObjectValue getObjectValue,
                                 LoadStringValue loadStringValue,
                                 ISerializerSettings settings)
        {
            if (type.IsEnum)
                return Enum.Parse(type,
                    getVal(ref currentIndex, txt, stringBuilder));

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

                    loadPrimitive(ref currentIndex, txt, stringBuilder);

                    if (stringBuilder.Length == 0 && txt[currentIndex] == '"')
                    {
                        // numeric value in quotes...
                        currentIndex++;
                        loadPrimitive(ref currentIndex, txt, stringBuilder);
                        currentIndex++;
                    }

                    return Convert.ChangeType(stringBuilder.ToString(), code,
                        CultureInfo.InvariantCulture);

                case TypeCode.Empty:
                    break;

                case TypeCode.Object:

                    return getObjectValue(ref currentIndex, txt, type, stringBuilder, parent, prop,
                        ref root, ctorValues, getVal, loadStringValue, settings);

                case TypeCode.DBNull:
                    break;

                case TypeCode.Boolean:
                    GetNext(ref currentIndex, txt, stringBuilder, _boolChars);
                    return Boolean.Parse(stringBuilder.ToString());


                case TypeCode.Char:
                    if (!tryGetString(ref currentIndex, txt, stringBuilder))
                        throw new InvalidOperationException();

                    return stringBuilder[0];

                case TypeCode.DateTime:
                    return DateTime.Parse(getVal(ref currentIndex, txt, stringBuilder));

                case TypeCode.String:
                    var isAString = tryGetString(ref currentIndex, txt, stringBuilder);
                    return !isAString
                        ? default
                        : _primitiveScanner.Descape(stringBuilder.ToString());

                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw new NotImplementedException();
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

        protected abstract void AdvanceUntilFieldStart(ref Int32 currentIndex,
                                                       String txt);

        protected abstract void AdvanceUntilEndOfNode(ref Int32 currentIndex,
                                                      String txt);

        protected abstract void LoadNextPrimitive(ref Int32 currentIndex,
                                                 String txt,
                                                 StringBuilder stringBuilder);

        protected abstract void LoadNextStringValue(ref Int32 currentIndex,
                                 String txt,
                                 StringBuilder stringBuilder);

        [MethodImpl(256)]
        private PropertyInfo? GetProperty(Type type,
                                          String name)
        {
            return type.GetProperty(name) ?? type.GetProperty(
                _typeInference.ToPropertyStyle(name));
        }

        /// <summary>
        /// should move the currentIndex past the opening tag (xml) or the opening [ (json)
        /// </summary>
        /// <param name="currentIndex"></param>
        /// <param name="txt"></param>
        /// <param name="stringBuilder">the value of the string builder should be the tag name for xml</param>
        /// <returns>false if the value is null, true if deserialization can proceed</returns>
        protected abstract Boolean InitializeCollection(ref Int32 currentIndex,
                                                   String txt,
                                                   StringBuilder stringBuilder);

        private Object? GetCollectionValue(ref Int32 currentIndex,
                                           String txt,
                                           Type type,
                                           StringBuilder stringBuilder,
                                           Object? parent,
                                           PropertyInfo? prop,
                                           ref Object? root,
                                           Object[] ctorValues,
                                           ISerializerSettings settings)
        {
            var germane = _types.GetGermaneType(type);

            var collection = GetEmptyCollection(type, germane, prop, parent);

            while (true)
            {
                SkipWhiteSpace(ref currentIndex, txt);

                if (!IsCollectionHasMoreItems(ref currentIndex, txt))
                    break;

                var noneState = NodeScanState.None;

                var current = DeserializeNode(txt, ref currentIndex, stringBuilder, germane, settings,
                    _emptyCtorValues, ref noneState, parent, prop);

                //var current = GetValue(ref currentIndex, txt, germane,
                //    stringBuilder, parent, prop, ref root, ctorValues);
                if (current == null)
                    break;

                stringBuilder.Clear();

                collection.Add(current);

                //if (!IsCollectionHasMoreItems(ref currentIndex, txt, collectionName))
                //    break;
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
        private String GetNextString(ref Int32 currentIndex,
                                     String txt,
                                     StringBuilder stringBuilder)
        {
            if (!TryGetNextString(ref currentIndex, txt, stringBuilder))
                throw new InvalidOperationException();

            return stringBuilder.ToString();
        }

        protected abstract Boolean TryGetNextProperty(ref Int32 currentIndex,
                                                      String txt,
                                                      StringBuilder sbString,
                                                      out PropertyInfo prop,
                                                      out Type propValType);

        protected abstract Boolean TryLoadNextPropertyName(ref Int32 currentIndex,
                                                           String txt,
                                                           StringBuilder sbString);
                                                          //out Type? propertyValueType);

        protected abstract Boolean TryGetNextString(ref Int32 currentIndex,
                                                    String txt,
                                                    StringBuilder sbString);

        [MethodImpl(256)]
        private IList GetEmptyCollection(Type collectionType,
                                         Type germaneType,
                                         PropertyInfo? collectionIsProperty,
                                         Object? parent)
        {
            if (!collectionType.IsArray && collectionIsProperty != null)
            {
                var pVal = collectionIsProperty.GetValue(parent, null);

                if (pVal is IList list)
                    return list;

                //var gCollection = typeof(ICollection<>).MakeGenericType(germaneType);

                //if (collectionIsProperty.GetValue(parent, null) is IList list)
                //    return list;
            }

            var res = collectionType.IsArray
                ? _instantiator.BuildGenericList(_types.GetGermaneType(collectionType))
                : _instantiator.BuildDefault(collectionType, true);
            if (res is IList good)
                return good;

            if (res is ICollection collection &&
                _types.GetAdder(collection, collectionType) is { } adder)
            {
                return new ValueCollectionWrapper(collection, adder);
            }

            throw new InvalidOperationException();
        }

        //[MethodImpl(256)]
        //private IList GetEmptyCollection(Type collectionType,
        //                                 Type germaneType)
        //{
        //    if (!collectionType.IsArray && prop != null)
        //        if (prop.GetValue(parent, null) is IList list)
        //            return list;

        //    var res = type.IsArray
        //        ? _instantiator.BuildGenericList(_types.GetGermaneType(type))
        //        : _instantiator.BuildDefault(type, true);
        //    if (res is IList good)
        //        return good;

        //    if (res is ICollection collection &&
        //        _types.GetAdder(collection, type) is { } adder)
        //    {
        //        return new ValueCollectionWrapper(collection, adder);
        //    }

        //    throw new InvalidOperationException();
        //}

        protected readonly IInstantiator _instantiator;
        protected readonly IObjectManipulator _objectManipulator;
        protected readonly ITypeInferrer _typeInference;
        protected readonly ITypeManipulator _types;
        protected readonly IStringPrimitiveScanner _primitiveScanner;
        private readonly IDynamicTypes _dynamicTypes;
        private readonly Char _startBlockChar;
        protected readonly String _typeWrapAttribute;
        private readonly String _circularReferenceAttribute;

        private static readonly Object[] _emptyCtorValues = new Object[0];


        private readonly Char[] _objectOrStringOrNull; // = {'"', '{', 'n'};
        private readonly Char[] _arrayOrObjectOrNull; // = {'[', '{', 'n'};
        protected readonly Char[] _fieldStartChars;

        private static readonly HashSet<Char> _boolChars = new HashSet<char>(
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
    }

    public delegate String GetStringValue(ref Int32 currentIndex,
                                          String txt,
                                          StringBuilder stringBuilder);

    public delegate void LoadPrimitiveValue(ref Int32 currentIndex,
                                          String txt,
                                          StringBuilder stringBuilder);

    public delegate void LoadStringValue(ref Int32 currentIndex,
                                           String txt,
                                           StringBuilder stringBuilder);

    public delegate Boolean TryGetStringValue(ref Int32 currentIndex,
                                             String txt,
                                             StringBuilder sbString);

    public delegate Object? GetObjectValue(ref Int32 currentIndex,
                                           String txt,
                                           Type type,
                                           StringBuilder stringBuilder,
                                           Object? parent,
                                           PropertyInfo? prop,
                                           ref Object? root,
                                           Object[] ctorValues,
                                           GetStringValue getVal,
                                           LoadStringValue loadPrimitiveValue,
                                           ISerializerSettings settings);
}
