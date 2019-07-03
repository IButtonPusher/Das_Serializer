using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// ReSharper disable UnusedMember.Global

namespace Serializer.Printers
{
	public class JsonWriter
	{
		protected void Block(StringBuilder sb, params Action[] blocks)
		{
			ConditionalBlock(sb, Cast(blocks).ToArray());

			IEnumerable<Func<Boolean>> Cast(IEnumerable<Action> actions)
			{
				foreach (var action in actions)
					yield return () => { action(); return true; };
			}
		}

		protected void ConditionalBlock(StringBuilder sb, params Func<Boolean>[] blocks)
		{
			sb.Append("{ ");
			bool lastPrinted;
			var isDeficit=false;

			if (blocks?.Length > 0)
				lastPrinted = blocks[0]();
			else return;

			for (var c = 1; c < blocks.Length; c++)
			{
				if (lastPrinted)
					sb.Append(",");
				var nowPrinted = blocks[c]();
				if (!nowPrinted && lastPrinted)
					isDeficit = true;
				else if (nowPrinted && !lastPrinted)
					isDeficit = false;
				lastPrinted = nowPrinted;
			}

			if (isDeficit)
				sb.Remove(sb.Length - 1, 1);

			sb.Append(" }");
		}

		protected void Block(StringBuilder sb, String key,
			Action<StringBuilder> action)
		{
			sb.Append("\"" + key + "\": { ");
			action(sb);
			sb.Append(" }");
		}

		protected void Collection<T>(StringBuilder sb, String key,
					IEnumerable<T> items, Action<T> action)
		{
			sb.Append("\"" + key + "\": [ ");

			using (var itar = items.GetEnumerator())
			{
				if (itar.MoveNext())
				{
					action(itar.Current);

					while (itar.MoveNext())
					{
						sb.Append(",");

						action(itar.Current);
					}
				}
			}

			sb.Append(" ]");
		}

		protected void Text(StringBuilder sb, String key, String value)
		{
			sb.Append("\"" + key + "\": \"" + value + "\"");
		}

		protected void Texts(StringBuilder sb, IDictionary<String, String> texts)
		{
			foreach (var kvp in texts)
				sb.Append("\"" + kvp.Key + "\": \"" + kvp.Value + "\"");
		}

		protected void Number(StringBuilder sb, String key, Int32 value)
		{
			sb.Append("\"" + key + "\":" + value);
		}

		protected void Boolean(StringBuilder sb, String key, Boolean value)
		{
			sb.Append("\"" + key + "\":" + (value ? "true" : "false"));
		}
	}
}
