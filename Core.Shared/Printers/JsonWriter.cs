using Das.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable UnusedMember.Global

namespace Serializer.Printers
{
    public class JsonWriter
    {
        protected static void Block(ITextRemunerable sb, params Action<ITextRemunerable>[] blocks)
        {
            ConditionalBlock(sb, Cast(blocks).ToArray());

            IEnumerable<Func<ITextRemunerable, Boolean>> Cast(
                IEnumerable<Action<ITextRemunerable>> actions)
            {
                foreach (var action in actions)
                    yield return (s) =>
                    {
                        action(s);
                        return true;
                    };
            }
        }

        protected static void ConditionalBlock(ITextRemunerable sb,
            params Func<ITextRemunerable, Boolean>[] blocks)
        {
            sb.Append("{ ");
            Boolean lastPrinted;
            var isDeficit = false;

            if (blocks?.Length > 0)
                lastPrinted = blocks[0](sb);
            else return;

            for (var c = 1; c < blocks.Length; c++)
            {
                if (lastPrinted)
                    sb.Append(",");
                var nowPrinted = blocks[c](sb);
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

        protected static void Block(ITextRemunerable sb, String key,
            Action<ITextRemunerable > action)
        {
            sb.Append("\"" + key + "\": { ");
            action(sb);
            sb.Append(" }");
        }

        protected static void Collection<T>(ITextRemunerable sb, String key,
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

        protected static void Text(ITextRemunerable sb, String key, ITextAccessor value)
        {
            sb.Append('\"');
            sb.Append(key);
            sb.Append("\": \"");
            sb.Append(value);
            sb.Append('\"');

            //sb.Append("\"" + key + "\": \"" + value + "\"");
        }

        protected void Texts(ITextRemunerable sb, IDictionary<String, String> texts)
        {
            foreach (var kvp in texts)
                sb.Append("\"" + kvp.Key + "\": \"" + kvp.Value + "\"");
        }

        protected static void Number(ITextRemunerable sb, String key, Int32 value)
        {
            sb.Append("\"" + key + "\":" + value);
        }

        protected static void Boolean(ITextRemunerable sb, String key, Boolean value)
        {
            sb.Append("\"" + key + "\":" + (value ? "true" : "false"));
        }
    }
}