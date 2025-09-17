using HandlebarsDotNet.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Splitio_Tests.Unit_Tests.Util
{
    [TestClass]
    public class HelperTests
    {
        [TestMethod]
        public void ChunkByTest()
        {
            // Arrange.
            List<String> mainList = new List<string> { "one", "two", "three", "four", "five" };

            // Act.
            var result = Helper.ChunkBy(mainList, 2);

            // Assert.
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(2, result[0].Count);
            Assert.AreEqual(2, result[1].Count);
            Assert.AreEqual(1, result[2].Count);

        }
    }
}
