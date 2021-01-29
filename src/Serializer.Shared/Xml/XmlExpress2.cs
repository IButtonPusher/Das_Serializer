using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.Scanners;

namespace Das.Serializer.Xml
{
    public partial class XmlExpress2 : BaseExpress2
    {
        public XmlExpress2(IInstantiator instantiator,
                           ITypeManipulator types,
                           IObjectManipulator objectManipulator,
                           IStringPrimitiveScanner primitiveScanner,
                           ITypeInferrer typeInference,
                           ISerializerSettings settings,
                           IDynamicTypes dynamicTypes)
            : base(instantiator, objectManipulator, typeInference, types, primitiveScanner,
                dynamicTypes, '>', ImpossibleChar, Const.XmlType, Const.RefTag,
                new[] {'"', '>'}, Const.XmlNull)
        {
            _settings = settings;
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

        protected override void AdvanceScanStateToNodeClose(String txt,
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

        protected override void AdvanceScanStateToNodeNameRead(String txt,
                                                               ref Int32 currentIndex,
                                                               StringBuilder stringBuilder,
                                                               ref NodeScanState scanState)
        {
            while (scanState != NodeScanState.NodeSelfClosed &&
                   scanState != NodeScanState.EndOfNodeOpen &&
                   scanState != NodeScanState.ReadNodeName)
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
        }

        protected override void AdvanceScanStateToNodeOpened(String txt,
                                                             ref Int32 currentIndex,
                                                             StringBuilder stringBuilder,
                                                             ref NodeScanState scanState)
        {
            while (scanState != NodeScanState.NodeSelfClosed &&
                   scanState != NodeScanState.EndOfNodeOpen)
                AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
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
        }

        protected sealed override NodeTypes OpenNode(String txt,
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

                if (nodeScanState == NodeScanState.AttributeNameRead &&
                    stringBuilder.ToString() == Const.XmlType)
                {
                    stringBuilder.Clear();
                    AdvanceScanState(txt, ref currentIndex, stringBuilder, ref nodeScanState);
                    var typeName = stringBuilder.GetConsumingString();
                    specifiedType = _typeInference.GetTypeFromClearName(typeName, true) ??
                                    throw new TypeLoadException(typeName);
                }
            }

            if (specifiedType == null)
                specifiedType = Const.ObjectType;

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

            var conv = _types.GetTypeConverter(specifiedType);
            if (conv.CanConvertFrom(typeof(String)))
                return NodeTypes.StringConvertible;

            if (_typeInference.TryGetPropertiesConstructor(specifiedType, out _))
                return NodeTypes.PropertiesToConstructor;

            throw new NotImplementedException();
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
    }
}
