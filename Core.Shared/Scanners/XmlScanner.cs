using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Das.Serializer
{
    internal class XmlScanner : TextScanner
    {
        public XmlScanner(IConverterProvider converterProvider, ITextContext state)
            : base(converterProvider, state)
        {
            _nodes = state.ScanNodeProvider;
            EscapeChars = new List<Char>();
            WhiteSpaceChars = new List<Char> {Const.CarriageReturn, '\n', '\t'};
        }

        public sealed override Boolean IsRespectXmlIgnore => true;

        private void BuildNode()
        {
            if (NullNode == CurrentNode &&
                CurrentTagName == Const.WutXml)
                ClearCurrents();


            OpenNode();
        }

        private void ClearCurrents()
        {
            CurrentAttributes.Clear();
            CurrentTagName = Const.Empty;
            CurrentValue.Clear();
            _currentAttributeName = null;
        }

        private void CloseTag()
        {
            if (_isInsideTag && HasCurrentTag)
                BuildNode();


            _nodes.Sealer.CloseNode(CurrentNode);

            ClearCurrents();

            if (NullNode != CurrentNode.Parent)
                CurrentNode = CurrentNode.Parent;
        }

        public void CollateCurrentValue()
        {
            var val = CurrentValue.ToString();

            if (!HasCurrentTag)
                CurrentTagName = val;


            else if (String.IsNullOrWhiteSpace(_currentAttributeName))
                _currentAttributeName = val;
            else if (!CurrentAttributes.ContainsKey(_currentAttributeName!))
                CurrentAttributes.Add(_currentAttributeName!, val);
        }

        [MethodImpl(256)]
        protected sealed override Boolean IsQuote(Char c)
        {
            return _isOpeningOrClosingTag && (c == Const.Quote || c == Const.SingleQuote);
        }


        private void OpenTag()
        {
            //get any open attribute
            CollateCurrentValue();

            //<SomeTag> could be a pointless tag like
            //<Properties></Properties>
            //or could be good like <ID>5</ID>
            if (HasCurrentTag)
                BuildNode();

            _isOpeningTag = false;
            CurrentTagName = Const.Empty;
            _currentAttributeName = null;
        }

        protected sealed override void ProcessCharacter(Char c)
        {
            //we should never get here if we are in quotes/escaping
            switch (c)
            {
                case '<':
                    _isInsideTag = true;
                    CurrentAttributes.Clear();
                    _isNextTagPivot = true;

                    _isOpeningTag = true;
                    _isClosingTag = false;
                    _isOpeningOrClosingTag = true;
                    _isJustClosedTag = false;
                    break;
                case '>':
                    if (_isClosingTag)
                        CloseTag();
                    else if (_isOpeningTag)
                        OpenTag();

                    _isInsideTag = false;

                    CurrentValue.Clear();
                    _isClosingTag = false;
                    _isOpeningTag = false;
                    _isOpeningOrClosingTag = false;
                    break;
                case '/':
                    if (!_isInsideTag)
                    {
                        CurrentValue.Append(c);
                        CurrentNode.Append(c);
                        break;
                    }

                    _isOpeningTag = false;
                    _isClosingTag = true;
                    _isOpeningOrClosingTag = true;
                    break;
                case '=':
                    if (_isOpeningTag)
                    {
                        //this could be within a tag's text and is not a delimiter in that case
                        _currentAttributeName = CurrentValue.ToString();
                        CurrentValue.Clear();
                    }
                    else
                    {
                        CurrentNode.Append(c);
                        CurrentValue.Append(c);
                    }

                    break;
                case Const.Space:
                    //<SomeTag someAttribute...
                    if (_isOpeningTag)
                    {
                        CollateCurrentValue();
                        CurrentValue.Clear();
                    }
                    else if (!_isJustClosedTag)
                    {
                        CurrentValue.Append(c);
                        CurrentNode.Append(c);
                    }

                    break;
                default:
                    if (_isNextTagPivot)
                    {
                        CurrentValue.Clear();
                        _isNextTagPivot = false;
                    }

                    if (!_isInsideTag)
                        CurrentNode.Append(c);

                    CurrentValue.Append(c);

                    break;
            }
        }

        private static readonly NullNode NullNode = NullNode.Instance;
        private readonly ITextNodeProvider _nodes;
        private String? _currentAttributeName;
        private Boolean _isClosingTag;
        private Boolean _isInsideTag;
        private Boolean _isJustClosedTag;

        // when we see a < it could mean open a new tag or close a tag.  We have to decide to 
        //purge whitespace or not till we see what's next
        private Boolean _isNextTagPivot;
        private Boolean _isOpeningOrClosingTag;
        private Boolean _isOpeningTag;
    }
}