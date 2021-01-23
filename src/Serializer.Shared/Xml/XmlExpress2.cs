using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Das.Serializer.Scanners;

namespace Das.Serializer.Xml
{
    public class XmlExpress2 : BaseExpress2
    {
        private readonly ISerializerSettings _settings;

        public XmlExpress2(IInstantiator instantiator,
                           ITypeManipulator types,
                           IObjectManipulator objectManipulator,
                           IStringPrimitiveScanner primitiveScanner,
                           ITypeInferrer typeInference,
                           ISerializerSettings settings,
                           IDynamicTypes dynamicTypes)
            : base(instantiator, objectManipulator, typeInference, types, primitiveScanner, 
                dynamicTypes, '>', ImpossibleChar, Const.XmlType, Const.RefTag,
                new[] {'"', '>'})
        {
            _settings = settings;
          //  _lolCopter = new XmlExpress(instantiator, types, settings, primitiveScanner, typeInference);
        }

        //private readonly XmlExpress _lolCopter;

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
                    _emptyCtorValues, ref noneState, null, null, null);

                if (current is T good)
                    yield return good;
                else
                {}
            }

            //todo: yeah...
            //return _lolCopter.DeserializeMany<T>(xml);
        }

        protected sealed override void AdvanceUntilEndOfNode(ref Int32 currentIndex,
                                                             String txt)
        {
            AdvanceUntil('>', ref currentIndex, txt);
            currentIndex++;
        }

        protected virtual void LoadNextStringValue(ref Int32 currentIndex,
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
                    case '"':
                    case '<':
                        currentIndex++;
                        return;

                    default:
                        stringBuilder.Append(currentChar);
                        break;
                }
            }
        }

        protected override void LoadNextPrimitive(ref Int32 currentIndex,
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

            //_primitiveScanner.GetValue(stringBuilder.ToString(),
            //    prop.PropertyType, true)
        }

        protected sealed override NodeTypes GetNodeInstanceType(ref Int32 currentIndex,
                                                                String txt,
                                                                StringBuilder stringBuilder,
                                                                ref Type? specifiedType,
                                                                ref NodeScanState nodeScanState)
        {
            if (nodeScanState == NodeScanState.JustOpened)
            {
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                stringBuilder.Clear();
            }
           

            for (; currentIndex < txt.Length; currentIndex++)
            {
                if (currentIndex + 8 /*length of xsi:type*/ >= txt.Length)
                    goto noXsiType;

                if (txt[currentIndex] == 'x' &&
                    txt[currentIndex + 1] == 's' &&
                    txt[currentIndex + 2] == 'i' &&
                    txt[currentIndex + 3] == ':' &&
                    txt[currentIndex + 4] == 't' &&
                    txt[currentIndex + 5] == 'y' &&
                    txt[currentIndex + 6] == 'p' &&
                    txt[currentIndex + 7] == 'e')
                {
                    currentIndex += 9;
                    stringBuilder.Clear();
                    
                    specifiedType = GetTypeFromText(ref currentIndex, txt, stringBuilder);
                    
                }

                if (txt[currentIndex] == ' ')
                    continue;

                //if (txt[currentIndex] == '>')
                    goto noXsiType;
            }

            noXsiType:
            if (specifiedType == null)
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

            var conv = _types.GetTypeConverter(specifiedType);
            if (conv.CanConvertFrom(typeof(String)))
                return NodeTypes.StringConvertible;

            if (_typeInference.TryGetPropertiesConstructor(specifiedType, out _))
                return NodeTypes.PropertiesToConstructor;

            


            //return specifiedType;
            return NodeTypes.None;
        }

        protected override void HandleEncodingNode(String txt,
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


        protected override void AdvanceScanState(String txt,
                                                 ref Int32 currentIndex,
                                                 StringBuilder stringBuilder,
                                                 ref NodeScanState scanState)
        {
            SkipWhiteSpace(ref currentIndex, txt);

            if (currentIndex >= txt.Length)
                goto eof;

            switch (txt[currentIndex])
            {
                case '<':
                    switch (txt[currentIndex + 1])
                    {
                        case '/':
                            currentIndex++;
                            while (true)
                            {
                                var currentChar = txt[++currentIndex];
                                switch (currentChar)
                                {
                                    case '>':
                                        scanState = NodeScanState.EndOfNodeClose;
                                        currentIndex++;
                                        return;
                                }
                            }
                        
                        case '?':
                            scanState = NodeScanState.EncodingNodeOpened;
                            currentIndex += 6; // skip <?xml(space)
                            return;

                        case '!':
                            // skip the whole thing here
                            while (true)
                            {
                                var currentChar = txt[++currentIndex];
                                switch (currentChar)
                                {
                                    case '>':
                                        scanState = NodeScanState.None;
                                        currentIndex++;
                                        AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
                                        
                                        return;
                                }
                            }

                        default:
                            currentIndex++;
                            scanState = NodeScanState.JustOpened;
                            break;
                    }

                    break;

                case '>':
                    currentIndex++;
                    scanState = NodeScanState.EndOfNodeOpen;
                    return;

                case '/':
                    if (scanState == NodeScanState.AttributeValueRead ||
                        scanState == NodeScanState.ReadNodeName)
                    {
                        if (txt[++currentIndex] != '>')
                            throw new InvalidOperationException();

                        scanState = NodeScanState.NodeSelfClosed;
                        currentIndex++;
                        return;
                    }

                    throw new NotImplementedException();
                    

                case '?':
                    scanState = NodeScanState.EncodingNodeClose;
                    currentIndex += 2;
                    return;
            }

            for (; currentIndex < txt.Length; currentIndex++)
            {
                var currentChar = txt[currentIndex];

                switch (currentChar)
                {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        break;

                    case '<':
                        currentChar = txt[++currentIndex];
                        if (currentChar == '/')
                        {
                            // closing node
                            while (true)
                            {
                                currentChar = txt[++currentIndex];
                                switch (currentChar)
                                {
                                    //case ' ':
                                    //case '/':
                                    case '>':
                                        scanState = NodeScanState.EndOfNodeClose;
                                        currentIndex++;
                                        return;

                                    //default:
                                    //    stringBuilder.Append(currentChar);
                                    //    break;
                                }
                            }

                        }

                        break;

                    case '"':
                        if (scanState == NodeScanState.AttributeNameRead)
                        {
                            LoadNextStringValue(ref currentIndex, txt, stringBuilder);
                            scanState = NodeScanState.AttributeValueRead;
                            return;
                        }

                        break;

                    default:

                        while (true)
                        {
                            stringBuilder.Append(currentChar);

                            currentChar = txt[++currentIndex];
                            switch (currentChar)
                            {
                                case ' ':
                                    if (scanState == NodeScanState.JustOpened)
                                    {
                                        scanState = NodeScanState.ReadNodeName;
                                        return;
                                    }

                                    break;

                                case '=':
                                    //if (scanState == NodeScanState.AttributeNameRead)
                                    {
                                        currentIndex++;
                                        scanState = NodeScanState.AttributeNameRead;
                                        return;
                                    }

                                case '<':
                                  if (txt[currentIndex + 1] == '/')
                                  {
                                      while (txt[++currentIndex] != '>')
                                      {
                                      }

                                      currentIndex++;

                                      scanState = NodeScanState.EndOfNodeClose;
                                  }

                                  return;

                                case '/':
                                    if (scanState == NodeScanState.JustOpened)
                                        scanState = NodeScanState.NodeSelfClosed;

                                    if (scanState == NodeScanState.EndOfNodeOpen)
                                        continue;

                                    return;

                                case '>':
                                    if (scanState == NodeScanState.JustOpened)
                                    {
                                        currentIndex++;
                                        scanState = NodeScanState.EndOfNodeOpen;
                                    }

                                    return;
                                //goto endOfOpen;

                                //default:
                                //    stringBuilder.Append(currentChar);
                                //    break;
                            }
                        }

                    //endOfOpen:
                    //return NodeScanState.OpenNode;

                    //default:
                    //    stringBuilder.Append(currentChar);
                    //    break;
                }
            }

            eof:
            scanState = NodeScanState.EndOfMarkup;
        }

        protected override void AdvanceScanStateUntil(String txt,
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

        protected override void AdvanceScanStateToNodeOpened(String txt,
                                                             ref Int32 currentIndex,
                                                             StringBuilder stringBuilder,
                                                             ref NodeScanState scanState)
        {
            while (scanState != NodeScanState.NodeSelfClosed && 
                   scanState != NodeScanState.EndOfNodeOpen)
            {
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
            }
        }

        protected override void AdvanceScanStateToNodeClose(String txt,
                                                            ref Int32 currentIndex,
                                                            StringBuilder stringBuilder,
                                                            ref NodeScanState scanState)
        {
            while (scanState != NodeScanState.EndOfMarkup && 
                   scanState != NodeScanState.NodeSelfClosed && 
                   scanState != NodeScanState.EndOfNodeClose)
            {
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
            }
        }

        protected override void EnsurePropertyValType(ref Int32 currentIndex,
                                                      String txt,
                                                      StringBuilder stringBuilder,
                                                      ref Type? propvalType)
        {
            SkipWhiteSpace(ref currentIndex, txt);

            if (currentIndex + 8 /*length of xsi:type*/ >= txt.Length)
                return;

            if (txt[currentIndex] == 'x' &&
                txt[currentIndex + 1] == 's' &&
                txt[currentIndex + 2] == 'i' &&
                txt[currentIndex + 3] == ':' &&
                txt[currentIndex + 4] == 't' &&
                txt[currentIndex + 5] == 'y' &&
                txt[currentIndex + 6] == 'p' &&
                txt[currentIndex + 7] == 'e')
            {
                currentIndex += 9;
                
                //TryLoadNextPropertyName(ref currentIndex, txt, stringBuilder);
                stringBuilder.Clear();
                //LoadNextStringValue(ref currentIndex, txt, stringBuilder);
                propvalType = GetTypeFromText(ref currentIndex, txt, stringBuilder);
            }
        }

        protected override bool IsCollectionHasMoreItems(ref Int32 currentIndex,
                                                         String txt)
        {
            SkipWhiteSpace(ref currentIndex, txt);

            if (currentIndex + 2 >= txt.Length)
                return false;

            if (txt[currentIndex] != '<')
                return false;

            return txt[currentIndex + 1] != '/';
        }

        protected override bool TryGetNextString(ref Int32 currentIndex,
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

    }
}
