using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class JsonScanner : TextScanner
    {
        public JsonScanner(IConverterProvider converterProvider, ITextContext state) :
            base(converterProvider, state)
        {
        }

        private void AddAttribute()
        {
            if (CurrentTagName != Const.Empty && CurrentValue.Length > 0)
            {
                CurrentNode.AddAttribute(CurrentTagName,
                    CurrentValue.ToString(), _wasCurrentValueInQuotes);

                CurrentTagName = Const.Empty;
                CurrentValue.Clear();
                _wasCurrentValueInQuotes = false;
            }
            else if (CurrentValue.Length > 0)
            {
                var str = PrimitiveScanner.Descape(CurrentValue.ToString());
                if (CurrentNode.NodeType == NodeTypes.None)
                {
                    Types.InferType(CurrentNode);
                    Types.EnsureNodeType(CurrentNode);
                }

                if (CurrentNode.NodeType == NodeTypes.Collection)
                {
                    var val = PrimitiveScanner.GetValue(str, 
                        TypeInferrer.GetGermaneType(CurrentNode.Type!), _wasCurrentValueInQuotes);
                    Sealer.Imbue(CurrentNode, String.Empty, val!);
                    CurrentValue.Clear();
                    _wasCurrentValueInQuotes = false;
                }
                else
                {
                    // zb { "Purple" }
                    CurrentNode.SetText(CurrentValue);
                    CurrentValue.Clear();
                    _wasCurrentValueInQuotes = false;
                }
            }
        }

        private void CloseNode()
        {
            if (CurrentNode.TryGetAttribute(Const.Val, false, out var val))
                CurrentNode.SetText(val.Value);

            Sealer.CloseNode(CurrentNode);

            if (NullNode != CurrentNode.Parent)
                CurrentNode = CurrentNode.Parent;
        }

        private void CreateNode()
        {
            if (CurrentTagName == Const.Val)
            {
                //don't make another node just for the val block
                CurrentTagName = Const.Empty;
                return;
            }

            if (NullNode == RootNode && CurrentTagName == Const.Empty)
                CurrentTagName = Const.Root;

            OpenNode();
        }

        [MethodImpl(256)]
        protected sealed override Boolean IsQuote(ref Char c)
        {
            return c == Const.Quote;
        }

        protected sealed override void ProcessCharacter(Char c)
        {
            switch (c)
            {
                case ':':
                    CurrentTagName = CurrentValue.ToString();
                    CurrentValue.Clear();
                    _wasCurrentValueInQuotes = false;
                    return;

                case Const.OpenBracket:
                    CreateNode();
                    CurrentTagName = Const.Empty;
                    break;

                case Const.OpenBrace:
                    CreateNode();
                    CurrentTagName = Const.Empty;
                    return;

                case Const.CloseBracket:
                    AddAttribute();
                    CloseNode();
                    break;

                case Const.CloseBrace:
                    AddAttribute();
                    CloseNode();
                    break;

                case Const.Comma:
                    AddAttribute();
                    break;


                case Const.CarriageReturn:
                case Const.NewLine:
                case Const.Tab:
                case Const.Space:
                    break;

                default:

                    CurrentNode.Append(c);
                    CurrentValue.Append(c);

                    break;
            }
        }

        private static readonly NullNode NullNode = NullNode.Instance;
    }
}