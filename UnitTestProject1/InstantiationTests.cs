using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Das;

namespace UnitTestProject1
{
    [TestClass]
    public class InstantiationTests
    {
        [TestCategory("instantiation"), TestMethod]
        public void NewInterface()
        {
            var srl = new DasSerializer();
            var thisIs = srl.BuildDefault<INiceAndEasy>(false);
            thisIs.YouCan<Int32>(DoIt);
        }

        private void DoIt<T>(T something)
        {
            
        }
    }
}
