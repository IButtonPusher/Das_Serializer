using System;
using Das.Serializer;


namespace Serializer.Tests
{
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

        private void DoIt<T>(T something)
        {
            
        }
    }
}
