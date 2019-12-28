using System;

namespace Das.Serializer.Scanners
{
    internal class JsonScanner : TextScanner
    {
        internal JsonScanner(IConverterProvider converterProvider, ITextContext state) :
            base(converterProvider, state)
        {
        }

        private static readonly NullNode NullNode = NullNode.Instance;

        protected sealed override Boolean IsQuote(Char c) => c == Const.Quote;

        protected override void ProcessCharacter(Char c)
        {
            switch (c)
            {
                case ':':
                    CurrentTagName = CurrentValue.ToString();
                    CurrentValue.Clear();
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
                default:
                    if (!WhiteSpaceChars.Contains(c))
                    {
                        //whitespace outside of quotes is immaterial
                        CurrentNode.Append(c);
                        CurrentValue.Append(c);
                    }

                    break;
            }
        }

        private void CreateNode()
        {
            if (CurrentTagName == DasCoreSerializer.Val)
            {
                //don't make another node just for the val block
                CurrentTagName = Const.Empty;
                return;
            }

            if (NullNode == RootNode && CurrentTagName == Const.Empty)
                CurrentTagName = Const.Root;

            OpenNode();
        }

        private void CloseNode()
        {
            if (CurrentNode.Attributes.TryGetValue(DasCoreSerializer.Val, out var val))
                CurrentNode.SetText(val);

            Sealer.CloseNode(CurrentNode);

            if (NullNode != CurrentNode.Parent)
                CurrentNode = CurrentNode.Parent;
        }

        private void AddAttribute()
        {
            if (CurrentTagName != Const.Empty && CurrentValue.Length > 0)
            {
                CurrentNode.Attributes.Add(CurrentTagName,
                    PrimitiveScanner.Descape(CurrentValue.ToString()));

                CurrentTagName = Const.Empty;
                CurrentValue.Clear();
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
                    var val = PrimitiveScanner.GetValue(str, TypeInferrer.
                        GetGermaneType(CurrentNode.Type));
                    Sealer.Imbue(CurrentNode, String.Empty, val);
                    CurrentValue.Clear();
                }
                else
                {
                    // zb { "Purple" }
                    CurrentNode.SetText(CurrentValue);
                    CurrentValue.Clear();
                }
            }
        }
    }
}