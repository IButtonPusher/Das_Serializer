using System;
using System.Collections.Generic;
using System.Text;

namespace Das.Serializer
{
    public abstract class TextScanner : SerializerCore, ITextScanner
    {
        protected List<Char> EscapeChars;
        protected List<Char> WhiteSpaceChars;

        private static readonly NullNode NullNode = NullNode.Instance;

        protected abstract Boolean IsQuote(Char c);

        private readonly ITextNodeProvider _nodes;

        private Boolean _isQuoteOpen;
        private Boolean _isEscapeNext;
        protected readonly StringBuilder CurrentValue;
        
        public ITextNode RootNode { get; protected set; }

        [NotNull]
        protected ITextNode CurrentNode;
        protected readonly Dictionary<String, String> CurrentAttributes;
        protected readonly IStringPrimitiveScanner PrimitiveScanner;

        [NotNull]
        protected String CurrentTagName;

        protected Boolean HasCurrentTag => !String.IsNullOrWhiteSpace(CurrentTagName);

        protected readonly ITextContext TextState;

        protected readonly INodeSealer<ITextNode> Sealer;
        protected readonly INodeManipulator Types;
        private Type? _resultType;
        private readonly IObjectConverter _converter;

        protected TextScanner(IConverterProvider converterProvider, ITextContext state)
            : base(state, state.Settings)
        {
            CurrentTagName = Const.Empty;
            CurrentNode = NullNode;

            SerializationDepth = state.Settings.SerializationDepth;
            IsOmitDefaultValues = state.Settings.IsOmitDefaultValues;
            _converter = converterProvider.ObjectConverter;
            CurrentValue = new StringBuilder();
            CurrentAttributes = new Dictionary<String, String>();
            TextState = state;
            Sealer = state.ScanNodeProvider.Sealer;
            Types = state.ScanNodeManipulator;

            PrimitiveScanner = state.PrimitiveScanner;
            EscapeChars = new List<Char> {Const.BackSlash};
            WhiteSpaceChars = new List<Char>
            {
                Const.CarriageReturn, '\n', '\t', Const.Space
            };

            RootNode = NullNode;

            _nodes = state.ScanNodeProvider;
        }


        protected void OpenNode()
        {
            var hold = CurrentNode;

            CurrentNode = _nodes.Get(CurrentTagName, CurrentAttributes, hold, this);

            if (NullNode != hold)
                hold.AddChild(CurrentNode);
            else if (!CurrentNode.IsEmpty)
            {
                RootNode = CurrentNode;
                var rootType = _resultType;
                if (!TextState.TypeInferrer.IsUseless(rootType) &&
                    TextState.TypeInferrer.IsUseless(RootNode.Type))

                    CurrentNode.Type = _resultType!;
            }
            else
            {
                CurrentNode = NullNode; //discard
            }
        }

        protected abstract void ProcessCharacter(Char c);

        public T Deserialize<T>(Char[] source)
        {
            _resultType = typeof(T);

            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                PreProcessCharacter(ref c);
            }

            return GetResult<T>();
        }

        public T Deserialize<T>(String source)
        {
            _resultType = typeof(T);

            var len = source.Length;

            for (var c = 0; c < len; c++)
            {
                var current = source[c];
                PreProcessCharacter(ref current);
            }

            return GetResult<T>();
        }

        private T GetResult<T>() => TextState.ObjectManipulator.
            CastDynamic<T>(RootNode.Value, _converter, Settings);

        private void PreProcessCharacter(ref Char c)
        {
            var iq = IsQuote(c);

            if (_isQuoteOpen)
            {
                if (!_isEscapeNext && iq)
                {
                    _isQuoteOpen = false;
                    return;
                }

                //we can only escape one char and if we got here
                //we can't already specified the escape or aren't 
                //escaping at all_isQuoteOpen
                _isEscapeNext = EscapeChars.Contains(c);

                CurrentValue.Append(c);

                return;
            }

            if (iq)
                _isQuoteOpen = !_isQuoteOpen;
            else
                switch (c)
                {
                    case '\0':
                        break;
                    default:
                        ProcessCharacter(c);
                        break;
                }
        }

       

        public void Invalidate()
        {
            _isQuoteOpen = _isEscapeNext = false;
            CurrentValue.Clear();
            if (NullNode != RootNode)
            {
                _nodes.Put(RootNode);
                RootNode = NullNode;
            }

            CurrentNode = NullNode;
            CurrentAttributes.Clear();
            CurrentTagName = Const.Empty;
            _resultType = default;
        }

        public Boolean IsOmitDefaultValues { get; }
        public SerializationDepth SerializationDepth { get; }
        public virtual Boolean IsRespectXmlIgnore => false;
    }
}