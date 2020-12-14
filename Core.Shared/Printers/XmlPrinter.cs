using System;
using System.Collections;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Das.Serializer;
#pragma warning disable 8604
#pragma warning disable 8602

namespace Das.Printers
{
    public class XmlPrinter : TextPrinter
    {
        public XmlPrinter(ITextRemunerable writer, 
                          ISerializationState stateProvider,
                          ISerializerSettings settings)
            : base(writer, stateProvider, settings)
        {
            _stateProvider = stateProvider;
            PathSeparator = '/';
            PathAttribute = Const.RefTag;
            _formatStack.Push(new StackFormat(-1, false));
        }

        public override Boolean IsRespectXmlIgnore => true;

        protected override void PrintCollection(IPrintNode node)
        {
            node.Type = node.Value.GetType();

            var parent = _formatStack.Pop();

            var knownEmpty = node.IsEmptyInitialized;

            if (parent.IsTagOpen)
            {
                Writer.Append(knownEmpty ? SelfClose : CloseTag);
                parent.IsTagOpen = false;
            }

            if (!knownEmpty)
            {
                var germane = _stateProvider.TypeInferrer.GetGermaneType(node.Type);

                PrintSeries(ExplodeList(node.Value as IEnumerable, germane),
                    PrintCollectionObject);
            }

            _formatStack.Push(parent);
        }

        protected void PrintCollectionObject(ObjectNode val)
        {
            if (!_isIgnoreCircularDependencies)
                PushStack($"[{val.Index}]");

            PrintNode(val);
            if (!_isIgnoreCircularDependencies)
                PopStack();
        }

        private void PrintLeafAttribute(IPrintNode node)
        {
            IsPrintingLeaf = true;
            Writer.Append($" {node.Name}={Const.Quote}");
            var res = _nodeTypes.GetNodeType(node.Type, Settings.SerializationDepth);
            node.NodeType = res;
            PrintObject(node);
            Writer.Append(Const.Quote);
        }


        public override void PrintNode(INamedValue node)
        {
            if (node.Value == null)
                return; 

            if (!_isIgnoreCircularDependencies)
                PushStack(node.Name);

            try
            {
                var valType = node.Value.GetType();
                var isWrapping = IsWrapNeeded(node.Type, valType);

                var nodeType = _nodeTypes.GetNodeType(valType,
                    Settings.SerializationDepth);
                var parent = _formatStack.Pop();

                /////////////////////////
                //close parent tag or print as attribute
                /////////////////////////

                if (parent.IsTagOpen) //try to print like inline attributes zb <tag val="5" ...
                {
                    if (nodeType == NodeTypes.Primitive && !isWrapping)
                    {
                        //does a leaf need to go in the stack?
                        _formatStack.Push(parent);
                        node.Type = valType;
                        using (var valu = _printNodePool.GetPrintNode(node))
                            PrintLeafAttribute(valu);
                        

                        return;
                    }
                    else
                    {
                        Writer.Append(CloseTag, Tabs);
                        parent.IsTagOpen = false;
                    }
                }

                /////////////////////////

                _formatStack.Push(parent);
                var current = new StackFormat(parent.Tabs + 1, true);

                /////////////////////////
                // open node's tag
                /////////////////////////

                if (Settings.CircularReferenceBehavior != CircularReference.IgnoreObject
                    || !IsObjectReferenced(node.Value))
                    //don't open a tag unless we need it
                {
                    for (var c = 0; c < current.Tabs; c++)
                        Writer.Append(_indenter);

                    //Writer.Append(tabBlob);
                    Writer.Append(OpenAttributes, node.Name);
                }
                else
                {
                    return; //we're ignoring circular refs and this was a circular ref...
                }

                if (isWrapping)
                {
                    //embed type info
                    var typeName = _stateProvider.TypeInferrer.ToClearName(valType, false);
                    typeName = SecurityElement.Escape(typeName);

                    Writer.Append(" ", Const.XmlType);

                    Writer.Append(Const.Equal, Const.StrQuote);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    Writer.Append(typeName, Const.StrQuote);

                    if (nodeType == NodeTypes.Primitive || nodeType == NodeTypes.Fallback)
                    {
                        current.IsTagOpen = false;
                        Writer.Append(CloseAttributes);
                    }
                }
                else if (_stateProvider.TypeInferrer.IsLeaf(node.Type, true)
                         || nodeType == NodeTypes.Fallback && current.IsTagOpen
                         || _stateProvider.TypeInferrer.TryGetNullableType(node.Type, out _))
                {
                    //if we got here with a leaf then the regular logic of trying to make it an attribute 
                    //doesn't apply => self close
                    Writer.Append(CloseAttributes);
                    current.IsTagOpen = false;
                }
                /////////////////////////


                /////////////////////////
                // print node contents
                /////////////////////////
                _formatStack.Push(current);
                Boolean couldPrint;

                node.Type = valType;
                using (var print = _printNodePool.GetPrintNode(node))
                    couldPrint = PrintObject(print);
                

                _formatStack.Pop();
                /////////////////////////


                /////////////////////////
                // close tag
                /////////////////////////

                if (current.IsTagOpen)
                {
                    if (couldPrint)
                        Writer.Append(SelfClose, Tabs);
                }
                else
                {
                    switch (nodeType)
                    {
                        case NodeTypes.Primitive:
                        case NodeTypes.Fallback:
                            break;
                        case NodeTypes.Collection:
                            if (node.IsEmptyInitialized)
                                return;
                            break;
                        default:
                            for (var c = 0; c < current.Tabs; c++)
                                Writer.Append(_indenter);
                            break;
                    }

                    Writer.Append($"</{node.Name}>\r\n");
                }

                /////////////////////////
            }
            finally
            {
                if (!_isIgnoreCircularDependencies)
                    PopStack();
            }
        }

        protected override void PrintReferenceType(IPrintNode node)
        {
            var series = _stateProvider.ObjectManipulator.GetPropertyResults(node, this)
                                       .OrderByDescending(r => r.Type == Const.StrType);
            PrintSeries(series, PrintProperty);
        }

        protected override void PrintString(String input,
                                            Boolean isInQuotes)
        {
            var parent = _formatStack.Pop();

            if (!IsPrintingLeaf && parent.IsTagOpen)
            {
                Writer.Append(CloseAttributes);
                parent.IsTagOpen = false;
            }

            Writer.Append(SecurityElement.Escape(input)!);

            _formatStack.Push(parent);
        }

        private const String SelfClose = " />\r\n";
        private const String CloseTag = ">\r\n";
        private const Char CloseAttributes = '>';
        private const Char OpenAttributes = '<';

        private readonly ISerializationState _stateProvider;

        protected Boolean IsPrintingLeaf;
    }
}