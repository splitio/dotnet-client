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
            EventsManagerConfig config = new EventsManagerConfig();

            //Assert
            config.RequireAll.TryGetValue(SdkEvent.SdkReady, out var require1);
            Assert.AreEqual(1, require1.Count);
            Assert.IsTrue(require1.Contains(SdkInternalEvent.SdkReady));

            config.Prerequisites.TryGetValue(SdkEvent.SdkUpdate, out var ready2);
            Assert.IsTrue(ready2.Contains(SdkEvent.SdkReady));

            config.ExecutionLimits.TryGetValue(SdkEvent.SdkUpdate, out var update);
            Assert.AreEqual(-1, update);
            config.ExecutionLimits.TryGetValue(SdkEvent.SdkReady, out var ready);
            Assert.AreEqual(1, ready);

            config.RequireAny.TryGetValue(SdkEvent.SdkUpdate, out var require2);
            Assert.AreEqual(4, require2.Count);
            Assert.IsTrue(require2.Contains(SdkInternalEvent.SegmentsUpdated));
            Assert.IsTrue(require2.Contains(SdkInternalEvent.RuleBasedSegmentsUpdated));
            Assert.IsTrue(require2.Contains(SdkInternalEvent.FlagKilledNotification));
            Assert.IsTrue(require2.Contains(SdkInternalEvent.FlagsUpdated));

            int order = 0;
            Assert.AreEqual(2, config.EvaluationOrder.Count);
            foreach (var sdkEvent in config.EvaluationOrder)
            {
                order++;
                switch (order)
                {
                    case 1:
                        Assert.AreEqual(SdkEvent.SdkReady, sdkEvent);
                        break;
                    case 2:
                        Assert.AreEqual(SdkEvent.SdkUpdate, sdkEvent);
                        break;
                }
            }
        }
    }
}
