using System;
using System.Collections;
using System.Linq;
using Das.Serializer;
using Das.Serializer.Remunerators;

namespace Das.Printers
{
    internal class XmlPrinter : TextPrinter
    {
        public XmlPrinter(ITextRemunerable writer, ISerializationState stateProvider,
            ISerializerSettings settings)
            : base(writer, stateProvider, settings)
        {
            _stateProvider = stateProvider;
            PathSeparator = '/';
            PathAttribute = DasCoreSerializer.RefTag;
            _formatStack.Push(new StackFormat(-1, false));
        }

        private readonly ISerializationState _stateProvider;

        private const String SelfClose = " />\r\n";
        private const String CloseTag = ">\r\n";
        private const Char CloseAttributes = '>';
        private const Char OpenAttributes = '<';

        protected Boolean IsPrintingLeaf;


        public override Boolean PrintNode(INamedValue node)
        {
            if (node.Value == null)
                return false;

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
                        {
                            //var valu = new PrintNode(node, nodeType);
                            PrintLeafAttribute(valu);
                        }

                        return true;
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
                var tabBlob = Enumerable.Repeat(_indenter, current.Tabs);
                if (Settings.CircularReferenceBehavior != CircularReference.IgnoreObject
                    || !IsObjectReferenced(node.Value))
                //don't open a tag unless we need it
                {
                    Writer.Append(tabBlob);
                    Writer.Append(OpenAttributes, node.Name);
                }
                else return false; //we're ignoring circular refs and this was a circular ref...

                if (isWrapping)
                {
                    //embed type info
                    var typeName = _stateProvider.TypeInferrer.ToClearName(valType, false);
                    typeName = System.Security.SecurityElement.Escape(typeName);
                    
                    Writer.Append(" ", Const.XmlType);

                    Writer.Append(Const.Equal, Const.StrQuote);
                    Writer.Append(typeName, Const.StrQuote);

                    if (nodeType == NodeTypes.Primitive || nodeType == NodeTypes.Fallback)
                    {
                        current.IsTagOpen = false;
                        Writer.Append(CloseAttributes);
                    }

//                    node.Type = valType;
                }
                else if (_stateProvider.TypeInferrer.IsLeaf(node.Type, true)
                         || nodeType == NodeTypes.Fallback && current.IsTagOpen)
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
                                return true;
                            break;
                        default:
                            tabBlob = Enumerable.Repeat(_indenter, current.Tabs);
                            Writer.Append(tabBlob);
                            break;
                    }
                   
                    Writer.Append($"</{node.Name}>\r\n");
                }

                return true;
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

        public override Boolean IsRespectXmlIgnore => true;

        protected Boolean PrintCollectionObject(ObjectNode val)
        {
            if (!_isIgnoreCircularDependencies)
                PushStack($"[{val.Index}]");

            PrintNode(val);
            if (!_isIgnoreCircularDependencies)
                PopStack();

            return true;
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

        protected override void PrintString(String input, Boolean isInQuotes)
        {
            var parent = _formatStack.Pop();

            if (!IsPrintingLeaf && parent.IsTagOpen)
            {
                Writer.Append(CloseAttributes);
                parent.IsTagOpen = false;
            }

            Writer.Append(System.Security.SecurityElement.Escape(input));

            _formatStack.Push(parent);
        }
    }
}