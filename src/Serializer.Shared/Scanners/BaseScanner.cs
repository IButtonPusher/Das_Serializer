using System;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer
{
    public abstract class BaseScanner
    {
        protected BaseScanner(ITypeManipulator typeManipulator)
        {
            _typeManipulator = typeManipulator;
        }

        protected Object? GetFromXPath(Object root,
                                       String xPath,
                                       StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
            Object? current = null;

            // xPath[0] should always be '/'
            for (var c = 1; c < xPath.Length; c++)
            {
                var currentChar = xPath[c];
                switch (currentChar)
                {
                    case '/':
                        if (!UpdateCurrentFromPathToken(ref current, root,
                            stringBuilder.GetConsumingString()))
                            return default;

                        break;

                    case '[':
                        break;

                    default:
                        stringBuilder.Append(currentChar);
                        break;
                }
            }

            if (stringBuilder.Length > 0)
                UpdateCurrentFromPathToken(ref current, root, stringBuilder.ToString());

            return current;
        }

        private Boolean UpdateCurrentFromPathToken(ref Object? current,
                                                   Object root,
                                                   String pathToken)
        {
            if (current == null)
            {
                if (String.Equals(pathToken,
                    root.GetType().FullName))
                {
                    current = root;
                    return true;
                }

                return false;
            }

            if (_typeManipulator.IsCollection(current.GetType()))
                throw new NotImplementedException();

            var prop = _typeManipulator.FindPublicProperty(current.GetType(),
                pathToken);
            if (prop == null)
                return false;

            current = prop.GetValue(current, null);
            return current != null;
        }

        protected readonly ITypeManipulator _typeManipulator;
    }
}
