using Das.Remunerators;
using System;
using System.Collections;
using System.Linq;
using Das.Serializer;
using Das.Serializer.Objects;
using Serializer;
using Serializer.Core.Printers;

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


        public override Boolean PrintNode(NamedValueNode node)
        {
            if (node.Value == null)
                return false;

            if (!_isIgnoreCircularDependencies)
                PushStack(node.Name);

            try
            {
                var valType = node.Value.GetType();
                var isWrapping = IsWrapNeeded(node.Type, valType);

                var nodeType = _stateProvider.GetNodeType(valType,
                    Settings.SerializationDepth);
                var parent = _formatStack.Pop();

                /////////////////////////
                //close parent tag or print as attribute
                /////////////////////////

                if (parent.IsTagOpen) //tag is open
                {
                    if (nodeType == NodeTypes.Primitive && !isWrapping)
                    {
                        //does a leaf need to go in the stack?
                        _formatStack.Push(parent);
                        node.Type = valType;
                        var valu = new PrintNode(node, nodeType);
                        PrintLeafAttribute(valu);
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
                var print = new PrintNode(node, nodeType);
                var couldPrint = PrintObject(print);

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
                    if (nodeType != NodeTypes.Primitive && nodeType != NodeTypes.Fallback)
                    {
                        tabBlob = Enumerable.Repeat(_indenter, current.Tabs);
                        Writer.Append(tabBlob);
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

        protected override void PrintReferenceType(PrintNode node)
        {
            var series = _stateProvider.ObjectManipulator.GetPropertyResults(node, this)
                .OrderByDescending(r => r.Type == Const.StrType);
            PrintSeries(series, PrintProperty);
        }

        protected override void PrintCollection(PrintNode node)
        {
            node.Type = node.Value.GetType();

            var parent = _formatStack.Pop();
            if (parent.IsTagOpen)
            {
                Writer.Append(CloseTag);
                parent.IsTagOpen = false;
            }

            var germane = _stateProvider.TypeInferrer.GetGermaneType(node.Type);
            PrintSeries(ExplodeList(node.Value as IEnumerable, germane),
                PrintCollectionObject);

            _formatStack.Push(parent);
        }

        protected Boolean PrintCollectionObject(ObjectNode val)
        {
            if (!_isIgnoreCircularDependencies)
                PushStack($"[{val.Index}]");

            PrintNode(val);
            if (!_isIgnoreCircularDependencies)
                PopStack();

            return true;
        }

        private void PrintLeafAttribute(PrintNode node)
        {
            IsPrintingLeaf = true;
            Writer.Append($" {node.Name}={Const.Quote}");
            var res = _stateProvider.GetNodeType(node.Type, Settings.SerializationDepth);
            node.NodeType = res;
            PrintObject(node);
            Writer.Append(Const.Quote);
        }

        protected override void PrintString(string input, Boolean isInQuotes)
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