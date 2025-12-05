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
            config.RequireAll.TryGetValue(SdkEvent.SdkReady, out var require1);
            Assert.AreEqual(4, require1.Count);
            Assert.IsTrue(require1.Contains(SdkInternalEvent.SegmentsUpdated));
            Assert.IsTrue(require1.Contains(SdkInternalEvent.LargeSegmentsUpdated));
            Assert.IsTrue(require1.Contains(SdkInternalEvent.RuleBasedSegmentsUpdated));
            Assert.IsTrue(require1.Contains(SdkInternalEvent.FlagsUpdated));

            config.Prerequisites.TryGetValue(SdkEvent.SdkUpdate, out var ready2);
            Assert.IsTrue(ready2.Contains(SdkInternalEvent.SdkReady));

            config.ExecutionLimits.TryGetValue(SdkEvent.SdkReadyTimeout, out var timout);
            Assert.AreEqual(1, timout);
            config.ExecutionLimits.TryGetValue(SdkEvent.SdkUpdate, out var update);
            Assert.AreEqual(-1, update);
            config.ExecutionLimits.TryGetValue(SdkEvent.SdkReady, out var ready);
            Assert.AreEqual(1, ready);

            config.RequireAny.TryGetValue(SdkEvent.SdkReadyTimeout, out var require3);
            Assert.AreEqual(1, require3.Count);
            Assert.IsTrue(require3.Contains(SdkInternalEvent.SdkTimedOut));
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
