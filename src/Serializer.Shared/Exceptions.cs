using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer;

public class CircularReferenceException : Exception
{
   public CircularReferenceException(IList<String> pathStack,
                                     Char pathSeparator)
   {
      var path = pathStack.ToString(pathSeparator, '[');
      _aboutMe = $"Circular reference {pathSeparator}{path}";
   }

   public override string ToString()
   {
      return _aboutMe;
   }

   private readonly String _aboutMe;
}