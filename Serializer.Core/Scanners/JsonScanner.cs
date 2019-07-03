﻿using System;
using Das.Serializer;
using Serializer;

namespace Das.Scanners
{
	internal class JsonScanner : TextScanner
	{
		internal JsonScanner(IConverterProvider converterProvider, ITextContext state) :
			base(converterProvider, state)
		{  }

        protected override bool IsQuote(char c) => c == Const.Quote;

        protected override void ProcessCharacter(char c)
		{
			switch (c)
			{
				case ':':
                    CurrentTagName = CurrentValue.ToString();
                    CurrentValue.Clear();
					return;
				case '[':
					CreateNode();
                    CurrentTagName = null;
					break;
				case '{':
					CreateNode();
                    CurrentTagName = null;
					return;
				case ']':
					AddAttribute();
					CloseNode();
					break;
				case '}':
					AddAttribute();
					CloseNode();
					break;
				case ',':
					AddAttribute();
					break;
				default:
					if (!WhiteSpaceChars.Contains(c))
					{
                        //whitespace outside of quotes is immaterial
                        CurrentNode?.Text?.Append(c);
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
                CurrentTagName = null;
                return;
            }
            else if (RootNode == null && CurrentTagName == null)
                CurrentTagName = Const.Root;
            OpenNode();
		}

		private void CloseNode()
		{
			if (CurrentNode.Attributes.TryGetValue(DasCoreSerializer.Val, out var val))
                CurrentNode.SetText(val);

            Sealer.CloseNode(CurrentNode);

			if (CurrentNode.Parent != null)
				CurrentNode = CurrentNode.Parent;
		}

		private void AddAttribute()
		{
			if (CurrentTagName != null && CurrentValue.Length > 0)
			{
				CurrentNode.Attributes.Add(CurrentTagName,
					 PrimitiveScanner.Descape(CurrentValue.ToString()));

                CurrentTagName = null;
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
					var val = PrimitiveScanner.GetValue(str, GetGermaneType(CurrentNode.Type));
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
