using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.State;

namespace Das.Serializer;

public abstract class BaseExpress
{
   protected BaseExpress(Char endArrayChar,
                         Char endBlockChar,
                         ITypeManipulator typeManipulator,
                         IInstantiator instantiator)
   {
      _endArrayChar = endArrayChar;
      _endBlockChar = endBlockChar;
      _types = typeManipulator;
      _instantiator = instantiator;
   }

   public abstract T Deserialize<T>(String txt,
                                    ISerializerSettings settings,
                                    Object[] ctorValues);

   public abstract IEnumerable<T> DeserializeMany<T>(String txt);

   [MethodImpl(256)]
   protected Boolean AdvanceUntil(ref Int32 currentIndex,
                                  String txt,
                                  Char target)
   {
      if (currentIndex >= txt.Length)
         return false;

      if (txt[currentIndex] == target)
         return true;

      //++currentIndex <-- this assumes there are spaces which fails when we print compact
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];

         if (current == target)
            return true;

         if (current == _endBlockChar || current == _endArrayChar)
            return false;
      }

      return false;
   }

   [MethodImpl(256)]
   protected static Char AdvanceUntilAny(Char[] targets,
                                         ref Int32 currentIndex,
                                         String txt)
   {
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];

         for (var k = 0; k < targets.Length; k++)
            if (current == targets[k])
               return current;
      }

      throw new InvalidOperationException();
   }

   protected static ConstructorInfo? GetConstructorWithStringParam(Type type)
   {
      var allMyCtors = type.GetConstructors();
      for (var c = 0; c < allMyCtors.Length; c++)
      {
         var allMyParams = allMyCtors[c].GetParameters();
         if (allMyParams.Length == 1 && allMyParams[0].ParameterType == Const.StrType)
            return allMyCtors[c];
      }

      return default;
   }

   protected Object? GetFromXPath(Object root,
                                  String xPath,
                                  StringBuilder stringBuilder)
   {
      ClearStringBuilder(stringBuilder);
      
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
         UpdateCurrentFromPathToken(ref current, root, stringBuilder.GetConsumingString()); //stringBuilder.ToString());

      return current;
   }

   [MethodImpl(256)]
   protected static void GetUntil(ref Int32 currentIndex,
                                  String txt,
                                  StringBuilder sbString,
                                  Char target)
   {
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];
         if (current == target)
            return;

         sbString.Append(current);
      }

      throw new InvalidOperationException();
   }

   [MethodImpl(256)]
   protected static void GetUntil(ref Int32 currentIndex,
                                  String txt,
                                  Char target)
   {
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];
         if (current == target)
            return;
      }

      throw new InvalidOperationException();
   }

   /// <summary>
   ///     Advances currentIndex to the found index + 1
   /// </summary>
   protected static void GetUntilAny(ref Int32 currentIndex,
                                     String txt,
                                     StringBuilder sbString,
                                     Char[] targets,
                                     out Char foundChar)
   {
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];

         for (var k = 0; k < targets.Length; k++)
            if (current == targets[k])
            {
               foundChar = current;
               currentIndex++;
               return;
            }

         sbString.Append(current);
      }

      throw new InvalidOperationException();
   }

   /// <summary>
   ///     Advances currentIndex to the found index + 1
   /// </summary>
   protected static void GetUntilAny(ref Int32 currentIndex,
                                     String txt,
                                     Char[] targets,
                                     out Char foundChar)
   {
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];

         for (var k = 0; k < targets.Length; k++)
            if (current == targets[k])
            {
               foundChar = current;
               currentIndex++;
               return;
            }
      }

      throw new InvalidOperationException();
   }

   /// <summary>
   ///     Advances currentIndex to the found index + 1
   /// </summary>
   protected static void GetUntilAny(ref Int32 currentIndex,
                                     String txt,
                                     StringBuilder sbString,
                                     Char[] targets)
   {
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];

         for (var k = 0; k < targets.Length; k++)
            if (current == targets[k])
            {
               currentIndex++;
               return;
            }

         sbString.Append(current);
      }

      throw new InvalidOperationException();
   }

   /// <summary>
   ///     Advances currentIndex to the found index + 1
   /// </summary>
   protected static void GetUntilAny(ref Int32 currentIndex,
                                     String txt,
                                     Char[] targets)
   {
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];

         for (var k = 0; k < targets.Length; k++)
            if (current == targets[k])
            {
               currentIndex++;
               return;
            }

         
      }

      throw new InvalidOperationException();
   }

   /// <summary>
   ///     advances currentIndex until the stopAt is found + 1
   /// </summary>
   [MethodImpl(256)]
   protected static void SkipUntil(ref Int32 currentIndex,
                                   String txt,
                                   Char stopAt)
   {
      if (TrySkipUntil(ref currentIndex, txt, stopAt))
         return;

      throw new InvalidOperationException();
   }

   [MethodImpl(256)]
   protected static Boolean TryAdvanceUntilAny(Char[] targets,
                                               ref Int32 currentIndex,
                                               String txt,
                                               out Char foundChar)
   {
      for (; currentIndex < txt.Length; currentIndex++)
      {
         var current = txt[currentIndex];

         for (var k = 0; k < targets.Length; k++)
            if (current == targets[k])
            {
               foundChar = current;
               return true;
            }
      }

      foundChar = '0';
      return false;
   }

   /// <summary>
   ///     advances currentIndex until the stopAt is found + 1
   /// </summary>
   [MethodImpl(256)]
   protected static Boolean TrySkipUntil(ref Int32 currentIndex,
                                         String txt,
                                         Char stopAt)
   {
      for (; currentIndex < txt.Length; currentIndex++)
         if (txt[currentIndex] == stopAt)
         {
            currentIndex++;
            return true;
         }

      return false;
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

      if (_types.IsCollection(current.GetType()))
         throw new NotImplementedException();

      var prop = _types.FindPublicProperty(current.GetType(),
         pathToken);
      if (prop == null)
         return false;

      current = prop.GetValue(current, null);
      return current != null;
   }

   [MethodImpl(256)]
   protected IList GetEmptyCollection(Type type,
                                      IPropertyAccessor? prop,
                                      Object? parent)
   {
      if (!type.IsArray && prop != null && 
          parent != null && parent is not IRuntimeObject)
      {
         var pVal = prop.GetPropertyValue(parent);

         if (pVal is IList list && type.IsAssignableFrom(pVal.GetType()))
            return list;
      }

      var res = type.IsArray
         ? _instantiator.BuildGenericList(_types.GetGermaneType(type))
         : _instantiator.BuildDefault(type, true);
            
      if (res is IList good)
         return good;

      if (res is ICollection collection &&
          _types.GetAdder(collection, type) is { } adder)
         return new ValueCollectionWrapper(collection, adder);


      if (type.IsGenericType)
         return GenericCollectionWrapper.Get(type);


      throw new InvalidOperationException();
   }

   protected static void ClearStringBuilder(StringBuilder sb)
   {
      if (sb.Length > 0)
      {

      }

      sb.Clear();
   }

   protected const Char ImpossibleChar = '\0';

   [ThreadStatic]
   protected static Object[]? _singleObjectArray;

   private readonly Char _endArrayChar;
   protected readonly Char _endBlockChar;
   protected readonly ITypeManipulator _types;
   protected readonly IInstantiator _instantiator;

   [ThreadStatic]
   protected static StringBuilder? _threadStringBuilder;
}