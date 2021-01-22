using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Das.Extensions;

namespace Das.Serializer.Xml
{
    public partial class XmlExpress
    {
       

        private IEnumerable<T> IterateCollection<T>(String xml,
                                                    StringBuilder stringBuilder)
        {
            var currentIndex = _iteratingIndex;

            while (true)
            {
                GetUntil(ref currentIndex, xml, stringBuilder, '<');
                stringBuilder.Clear();

                var currentChar = xml[currentIndex];

                if (currentChar == '/')
                {
                    GetUntil(ref currentIndex, xml, stringBuilder, '>');
                    stringBuilder.Clear();
                    yield break;
                }

                GetCurrentTagName(ref currentIndex, xml, stringBuilder);

                stringBuilder.Clear();

                if (typeof(T).IsPrimitive || typeof(T) == Const.StrType)
                {
                    currentIndex++;
                    GetUntil(ref currentIndex, xml, stringBuilder, '<');
                    var current = _primitiveScanner.GetValue(
                        stringBuilder.GetConsumingString(), typeof(T), true);
                    if (current is T good)
                        yield return good;
                    else throw new InvalidOperationException();
                }
                else if (_typeInference.HasEmptyConstructor(typeof(T)))
                {
                    var current = _instantiator.BuildDefault<T>(true) ?? throw new XmlException();
                    Object oCurrent = current;
                    DeserializeTag(ref oCurrent, ref currentIndex, xml, typeof(T), stringBuilder);
                    yield return current;
                }
                else if (_typeInference.TryGetPropertiesConstructor(typeof(T), out var ctor))
                {
                    var ctorParams = ctor.GetParameters();
                    var bldr = new ValueArgsBuilder(ctorParams);
                    Object nada = null!;
                    DeserializeTagImpl(ref nada, ref currentIndex, xml, typeof(T),
                        stringBuilder, bldr);


                    var res = ctor.Invoke(bldr.Values);
                    if (res is T good)
                        yield return good;
                    else throw new InvalidOperationException();
                }
            }
        }

        [ThreadStatic]
        private static Int32 _iteratingIndex;
    }
}
