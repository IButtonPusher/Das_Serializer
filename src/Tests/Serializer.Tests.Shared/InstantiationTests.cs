using System;
using System.Threading.Tasks;
using Das.Serializer;

namespace Serializer.Tests;

//[TestClass]
public class InstantiationTests
{
   //[TestCategory("instantiation"), TestMethod]
   public void NewInterface()
   {
      var srl = new DasSerializer();
      var thisIs = srl.ObjectInstantiator.BuildDefault<INiceAndEasy>(false);
      thisIs.YouCan<Int32>(DoIt);
   }

   private static void DoIt<T>(T something)
   {
   }
}