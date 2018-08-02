using System;
using KP.PackageVersionConflictLibray;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KP.UnitTest
{
    [TestClass]
    public class PackageVersionConflictUnitTest
    {
        [TestMethod]
        public void TestPackageVersionConflict()
        {
            Processor objProcessor = new Processor();

            objProcessor.Process();

            Assert.AreEqual(false, objProcessor.PackageConflictResult());

        }
    }
}
