using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventsManagerConfigTests
    {

        [TestMethod]
        public void BuildInstance()
        {
            //Act
            EventsManagerConfig config = EventsManagerConfig.BuildEventsManagerConfig();

            //Assert
            config.ExecutionLimits.TryGetValue(SdkEvent.SdkReadyTimeout, out var timout);
            Assert.AreEqual(1, timout);
            config.ExecutionLimits.TryGetValue(SdkEvent.SdkUpdate, out var update);
            Assert.AreEqual(-1, update);

            config.RequireAny.TryGetValue(SdkEvent.SdkReadyTimeout, out var require1);
            Assert.AreEqual(1, require1.Count);
            Assert.IsTrue(require1.Contains(SdkInternalEvent.SdkTimedOut));
            config.RequireAny.TryGetValue(SdkEvent.SdkUpdate, out var require2);
            Assert.AreEqual(5, require2.Count);
            Assert.IsTrue(require2.Contains(SdkInternalEvent.SegmentsUpdated));
            Assert.IsTrue(require2.Contains(SdkInternalEvent.LargeSegmentsUpdated));
            Assert.IsTrue(require2.Contains(SdkInternalEvent.RuleBasedSegmentsUpdated));
            Assert.IsTrue(require2.Contains(SdkInternalEvent.FlagKilledNotification));
            Assert.IsTrue(require2.Contains(SdkInternalEvent.FlagsUpdated));
        }
    }
}
