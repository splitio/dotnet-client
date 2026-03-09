using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Tasks
{
    [TestClass]
    public class InternalEvenstTaskTests
    {

        [TestMethod]
        public async Task TestFiringEvents()
        {
            //Act
            SplitQueue<SdkEventNotification> internalEventsQueue = new SplitQueue<SdkEventNotification>();
            Mock<IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata>> eventManager = new Mock<IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata>>();
            InternalEventsTask internalEventsTask = new InternalEventsTask(eventManager.Object, internalEventsQueue);
            internalEventsTask.Start();

            await internalEventsQueue.EnqueueAsync(new SdkEventNotification(SdkInternalEvent.SdkReady, null));
            System.Threading.SpinWait.SpinUntil(() => eventManager.Invocations.Count>0, TimeSpan.FromMilliseconds(2000));
            eventManager.Verify(mock => mock.NotifyInternalEvent(SdkInternalEvent.SdkReady, null), Times.Exactly(1));

            var metadata = new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } });
            await internalEventsTask.AddToQueue(SdkInternalEvent.FlagsUpdated, metadata);
            System.Threading.SpinWait.SpinUntil(() => eventManager.Invocations.Count > 1, TimeSpan.FromMilliseconds(2000));
            eventManager.Verify(mock => mock.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, metadata), Times.Exactly(1));

            internalEventsTask.Stop();
        }
    }
}
