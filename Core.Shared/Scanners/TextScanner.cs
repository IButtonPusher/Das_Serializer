using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public abstract class TextScanner : SerializerCore, ITextScanner
    {
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

        public ITextNode RootNode { get; protected set; }

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

        protected Boolean HasCurrentTag => !String.IsNullOrWhiteSpace(CurrentTagName);

        private T GetResult<T>()
        {
            return RootNode.Value != null
                ? TextState.ObjectManipulator.CastDynamic<T>(RootNode.Value, _converter, Settings)
                : default!;
        }

        protected abstract Boolean IsQuote(Char c);


        protected void OpenNode()
        {
            var hold = CurrentNode;

            CurrentNode = _nodes.Get(CurrentTagName, CurrentAttributes, hold, this);

            if (NullNode != hold)
            {
                hold.AddChild(CurrentNode);
            }
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

        protected abstract void ProcessCharacter(Char c);

        private static readonly NullNode NullNode = NullNode.Instance;
        private readonly IObjectConverter _converter;

        private readonly ITextNodeProvider _nodes;
        protected readonly Dictionary<String, String> CurrentAttributes;
        protected readonly StringBuilder CurrentValue;
        protected readonly IStringPrimitiveScanner PrimitiveScanner;

        protected readonly INodeSealer<ITextNode> Sealer;

        protected readonly ITextContext TextState;
        protected readonly INodeManipulator Types;
        private Boolean _isEscapeNext;

        private Boolean _isQuoteOpen;
        private Type? _resultType;

        [NotNull] protected ITextNode CurrentNode;

        [NotNull] protected String CurrentTagName;

        protected List<Char> EscapeChars;
        protected List<Char> WhiteSpaceChars;
    }
}