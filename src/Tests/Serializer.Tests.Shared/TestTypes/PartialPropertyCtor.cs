using System;
using System.Threading.Tasks;

namespace Serializer.Tests.TestTypes;

public class PartialPropertyCtor
{
   public PartialPropertyCtor(Boolean boolean1,
                              String string1,
                              String string2,
                              Boolean boolean2,
                              String string3,
                              String string4,
                              Boolean boolean3,
                              String string5,
                              String string6,
                              Double double1,
                              Int32 int1,
                              Boolean boolean4,
                              Boolean boolean5,
                              String string7,
                              String string8,
                              Double double2)
      : this(boolean1, string1, string2, boolean2, string3,
         string4, boolean3, string5, string6,
         double1, double2)
   {
      Int1 = int1;
      Boolean4 = boolean4;
      Boolean5 = boolean5;
      String7 = string7;
      String8 = string8;
   }


   private PartialPropertyCtor(Boolean boolean1,
                               String string1,
                               String string2,
                               Boolean boolean2,
                               String string3,
                               String string4,
                               Boolean boolean3,
                               String string5,
                               String string6,
                               Double double1,
                               Double double2)
   {
      Boolean1 = boolean1;
      String1 = string1;
      String2 = string2;
      Boolean2 = boolean2;
      String3 = string3;
      String4 = string4;
      Boolean3 = boolean3;
      String5 = string5;
      String6 = string6;
      Double1 = double1;
      Double2 = double2;

      String8 = String.Empty;
      String7 = String.Empty;
   }

   public Boolean Boolean1 { get; set; }

   public String String1 { get; set; }

   public String String2 { get; set; }


   public Boolean Boolean2 { get; set; }

   public String String3 { get; set; }

   public String String4 { get; set; }


   public Boolean Boolean3 { get; set; }

   public String String5 { get; set; }

   public String String6 { get; set; }

   public Double Double1 { get; set; }

   public Double Double2 { get; set; }

   /// <summary>
   ///     0 = IP, 1 == OOP, 2 == even more OOPer
   /// </summary>
   public Int32 Int1 { get; set; }

   public String String7 { get; set; }

   public String String8 { get; set; }

   public Boolean Boolean4 { get; set; }

   public Boolean Boolean5 { get; set; }

   public Boolean Boolean6 { get; set; }
}