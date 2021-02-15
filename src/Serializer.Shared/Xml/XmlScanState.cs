using System;
using System.Text;
using System.Threading.Tasks;

namespace Das.Serializer.Xml
{
    public partial class XmlExpress2
    {
        protected override Boolean AdvanceScanState(String txt,
                                                    ref Int32 currentIndex,
                                                    StringBuilder stringBuilder,
                                                    ref NodeScanState scanState)
        {
            if (currentIndex >= txt.Length)
            {
                scanState = NodeScanState.EndOfMarkup;
                return false;
            }

            Char currentChar;

            switch (scanState)
            {
                // try to open a new node
                case NodeScanState.NodeSelfClosed:
                case NodeScanState.None:
                case NodeScanState.EndOfNodeClose:
                case NodeScanState.EncodingNodeClose:
                    if (TrySkipUntil(ref currentIndex, txt, '<'))
                    {
                        if (txt[currentIndex] == '/')
                            scanState = NodeScanState.StartOfNodeClose;
                        else
                            scanState = NodeScanState.JustOpened;
                    }
                    else
                        scanState = NodeScanState.EndOfMarkup;

                    return true;

                case NodeScanState.JustOpened:
                    currentChar = txt[currentIndex];
                    if (currentChar == '?')
                    {
                        scanState = NodeScanState.EncodingNodeOpened;

                        // skip <?xml(space)
                        SkipUntil(ref currentIndex, txt, ' ');
                        return true;
                    }

                    else if (currentChar == '!')
                    {
                        SkipUntil(ref currentIndex, txt, '>');
                        scanState = NodeScanState.None;
                        
                        return AdvanceScanState(txt, ref currentIndex, stringBuilder, ref scanState);
                    }

                    goto readNodeName;


                case NodeScanState.EncodingNodeOpened:
                case NodeScanState.ReadNodeName:
                case NodeScanState.AttributeValueRead:
                    // try to read the next attribute=value

                    SkipWhiteSpace(ref currentIndex, txt);

                    GetUntilAny(ref currentIndex, txt, stringBuilder, _attributeNameSplitters, out currentChar);

                    switch (currentChar)
                    {
                        case '=':
                            scanState = NodeScanState.AttributeNameRead;
                            return true;

                        case '/':
                            scanState = NodeScanState.NodeSelfClosed;
                            return true;

                        case '>':
                            scanState = NodeScanState.EndOfNodeOpen;
                            return true;

                        case '?':
                            scanState = NodeScanState.EncodingNodeClose;
                            return true;
                    }

                    throw new NotImplementedException();

                case NodeScanState.AttributeNameRead:
                    currentChar = txt[currentIndex];
                    if (currentChar != '\'' && currentChar != '"')
                        throw new NotImplementedException();

                    currentIndex++;

                    GetUntilAny(ref currentIndex, txt, stringBuilder, _attributeValueEnders);
                    scanState = NodeScanState.AttributeValueRead;
                    return true;

                case NodeScanState.EndOfNodeOpen:
                    GetUntil(ref currentIndex, txt, stringBuilder, '<');

                    if (txt[++currentIndex] == '/')
                        scanState = NodeScanState.StartOfNodeClose;
                    else
                    {
                        // opening a child node, all the whitespace in the sb 
                        // is useless at best
                        stringBuilder.Clear();
                        scanState = NodeScanState.JustOpened;
                    }

                    return true;

                case NodeScanState.StartOfNodeClose:
                    SkipUntil(ref currentIndex, txt, '>');
                    scanState = NodeScanState.EndOfNodeClose;
                    return true;

                case NodeScanState.EndOfMarkup:
                    throw new InvalidOperationException();

                default:
                    throw new ArgumentOutOfRangeException(nameof(scanState), scanState, null);
            }

            readNodeName:
            GetUntilAny(ref currentIndex, txt, stringBuilder, _nodeNameEnders, out currentChar);

            switch (currentChar)
            {
                case ' ':
                    scanState = NodeScanState.ReadNodeName;
                    return true;

                case '/':
                    scanState = NodeScanState.NodeSelfClosed;
                    return true;

                case '>':
                    scanState = NodeScanState.EndOfNodeOpen;
                    return true;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        ///     ' ', '>', '/'
        /// </summary>
        private static readonly Char[] _nodeNameEnders = {' ', '>', '/'};

        /// <summary>
        ///     '=', '>', '/', '?'
        /// </summary>
        private static readonly Char[] _attributeNameSplitters = {'=', '>', '/', '?'};

        /// <summary>
        ///     single/double quote
        /// </summary>
        private static readonly Char[] _attributeValueEnders = {'\'', '"'};
    }
}
