using System;
using System.Collections.Generic;

// ReSharper disable All

namespace Serializer.Tests.TestTypes;

public class NotAbstractType : AbstractType2<String>
{
   public NotAbstractType(String tValue,
                          List<SimpleClassObjectProperty> items) : base(tValue, items)
   {
   }
}

public abstract class AbstractType2<T> : AbstractType1
{
   private readonly T _tValue;

   public AbstractType2(T tValue,
                        List<SimpleClassObjectProperty> items)
      : base(items)
   {
      _tValue = tValue;
   }
}

public abstract class AbstractType1
{
   private List<SimpleClassObjectProperty> _items;

   protected AbstractType1(List<SimpleClassObjectProperty> items)
   {
      _items = items;
   }
}

public static class AbstractTypeFactory
{
   public static AbstractType1 GetInstance()
   {
      var items = new List<SimpleClassObjectProperty>();
      items.Add(new SimpleClassObjectProperty("abcdefg"));
      items.Add(new SimpleClassObjectProperty("hijklmnop"));
      return new NotAbstractType("test string value", items);
   }
}