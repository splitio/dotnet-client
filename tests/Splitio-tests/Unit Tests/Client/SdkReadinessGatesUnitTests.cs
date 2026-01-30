using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Client.Classes;
using Splitio.Services.Tasks;

namespace Splitio_Tests.Unit_Tests.Client
{
    [TestClass]
    public class InMemoryReadinessGatesCacheUnitTests
    {

        [TestMethod]
        public void IsSDKReadyShouldReturnFalseIfSplitsAreNotReady()
        {
            //Arrange
            var internalEventsTask = new Mock<IInternalEventsTask>();
            var gates = new InMemoryReadinessGatesCache(internalEventsTask.Object);

            //Act
            var result = gates.IsReady();

            //Assert
            Assert.IsFalse(result);
        }
    }
}
