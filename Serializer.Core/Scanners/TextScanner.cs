using System;
using System.Collections.Generic;
using System.Text;
using Das.Serializer;
using Serializer;
using Serializer.Core;

namespace Das.Scanners
{
	public abstract class TextScanner : SerializerCore, ITextScanner
    {
        protected List<Char> EscapeChars;
		protected List<Char> WhiteSpaceChars;

        protected abstract Boolean IsQuote(Char c);

        private readonly ITextNodeProvider _nodes;
		
		private Boolean _isQuoteOpen;
		private Boolean _isEscapeNext;
		protected readonly StringBuilder CurrentValue;
		public ITextNode RootNode { get; protected set; }
		protected ITextNode CurrentNode;
		protected readonly Dictionary<String, String> CurrentAttributes;
		protected readonly IStringPrimitiveScanner PrimitiveScanner;
		protected String CurrentTagName;

        protected bool HasCurrentTag => !String.IsNullOrWhiteSpace(CurrentTagName);

        protected readonly ITextContext TextState;

        protected readonly INodeSealer<ITextNode> Sealer;
        protected readonly INodeManipulator Types;
        private Type _resultType;
        private readonly IObjectConverter _converter;

        protected TextScanner(IConverterProvider converterProvider, ITextContext state)
            : base(state, state.Settings)
        {
            _converter = converterProvider.ObjectConverter;
            CurrentValue = new StringBuilder();
            CurrentAttributes = new Dictionary<string, string>();
            TextState = state;
            Sealer = state.NodeProvider.Sealer;
            Types = state.NodeProvider.TypeProvider;

            PrimitiveScanner = state.PrimitiveScanner;
			EscapeChars = new List<Char>{ Const.BackSlash};
			WhiteSpaceChars = new List<Char> { Const.CarriageReturn, '\n', '\t', Const.Space };


            _nodes = state.NodeProvider;
		}


        protected void OpenNode()
        {
            var hold = CurrentNode;

            CurrentNode = _nodes.Get(CurrentTagName, CurrentAttributes, hold);

            if (hold != null)
                hold.AddChild(CurrentNode);
            else if (!CurrentNode.IsEmpty)
            {
                RootNode = CurrentNode;
                var rootType = _resultType;
                if (!TextState.TypeInferrer.IsUseless(rootType) &&
                    TextState.TypeInferrer.IsUseless(RootNode.Type))

                    CurrentNode.Type = _resultType;
            }
            else
            {
                CurrentNode = default; //discard
            }
        }

        protected abstract void ProcessCharacter(Char c);

        public T Deserialize<T>(IEnumerable<char> source)
        {
            _resultType = typeof(T);

            //check for BOM
            //https://stackoverflow.com/questions/1317700/strip-byte-order-mark-from-string-in-c-sharp
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                    return default;

                var c = iterator.Current;

                if (c > Byte.MaxValue)
                {
                    if (!iterator.MoveNext())
                        return default;
                }

                do
                {
                    c = iterator.Current;
                    
                    if (_isQuoteOpen)
                    {
                        if (!_isEscapeNext && IsQuote(c))
                        {
                            _isQuoteOpen = false;
                            continue;
                        }

                        //we can only escape one char and if we got here
                        //we can't already specified the escape or aren't 
                        //escaping at all_isQuoteOpen
                        _isEscapeNext = EscapeChars.Contains(c);

                        CurrentValue.Append(c);

                        continue;
                    }

                    var isQuote = IsQuote(c);

                    if (isQuote)
                        _isQuoteOpen = !_isQuoteOpen;
                    else switch (c)
                    {
                        case '\0':
                            break;
                        default:
                            ProcessCharacter(c);
                            break;
                    }
                }
                while (iterator.MoveNext());
            }

            return TextState.ObjectManipulator.
                CastDynamic<T>(RootNode.Value, _converter, Settings);
        }

        public void Invalidate()
        {
            _isQuoteOpen = _isEscapeNext = false;
            CurrentValue.Clear();
            _nodes.Put(RootNode);
            RootNode = null;
            CurrentNode = null;
            CurrentAttributes.Clear();
            CurrentTagName = null;
            _resultType = default;
        }
    }
}
