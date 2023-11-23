using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Common;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class BackOffTests
    {
        [TestMethod]
        public void GetIntervalsAndReset()
        {
            var backOff = new BackOff(backOffBase: 1, attempt: 0);

            var result = backOff.GetInterval();
            Assert.AreEqual(0, result);

            result = backOff.GetInterval();
            Assert.AreEqual(2, result);

            result = backOff.GetInterval();
            Assert.AreEqual(4, result);

            backOff.Reset();

            result = backOff.GetInterval();
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetIntervalsAndReset_WithMax()
        {
            var backOff = new BackOff(backOffBase: 5, attempt: 0, maxAllowed: 30);

            var result = backOff.GetInterval();
            Assert.AreEqual(0, result);

            result = backOff.GetInterval();
            Assert.AreEqual(10, result);

            result = backOff.GetInterval();
            Assert.AreEqual(20, result);

            result = backOff.GetInterval();
            Assert.AreEqual(30, result);

            result = backOff.GetInterval();
            Assert.AreEqual(30, result);

            result = backOff.GetInterval();
            Assert.AreEqual(30, result);

            result = backOff.GetInterval();
            Assert.AreEqual(30, result);

            backOff.Reset();

            result = backOff.GetInterval();
            Assert.AreEqual(0, result);
        }
    }
}
