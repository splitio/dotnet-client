﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Tests.Common;
using System.Collections.Generic;
using System.Threading;

namespace Splitio.Integration_tests
{
    [DeploymentItem(@"Resources\splits_push.json")]
    [DeploymentItem(@"Resources\splits_push2.json")]
    [DeploymentItem(@"Resources\splits_push3.json")]
    [DeploymentItem(@"Resources\splits_push4.json")]
    [DeploymentItem(@"Resources\split_segment4.json")]
    [DeploymentItem(@"Resources\split_segment4_empty.json")]
    [DeploymentItem(@"Resources\split_segment4_updated.json")]
    [DeploymentItem(@"Resources\split_segment4_updated_empty.json")]
    [TestClass, TestCategory("Integration")]
    public class StreamingClientTests
    {
        public static string EventSourcePath => "/eventsource";

        [TestMethod]
        public void GetTreatment_SplitUpdate_ShouldFetch()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time", "splits_push3.json", "1585948850109", "Second_Time");
                httpClientMock.SplitChangesSequence("splits_push4.json", "1585948850111", "First_Time_1");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "fb\r\nid: 123\nevent: message\ndata: {\"id\":\"1\",\"clientId\":\"emptyClientId\",\"connectionId\":\"1\",\"timestamp\":1582045421733,\"channel\":\"mauroc\",\"data\":\"{\\\"type\\\" : \\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\": 1585948850111}\",\"name\":\"asdasd\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = EvaluateWithDelay("admin", "push_test", "after_fetch", client);
                Assert.AreEqual("after_fetch", result);

                client.Destroy();
            }
        }

        [TestMethod]
        public void GetTreatment_SplitUpdate_ShouldNotFetch()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "fb\r\nid: 123\nevent: message\ndata: {\"id\":\"1\",\"clientId\":\"emptyClientId\",\"connectionId\":\"1\",\"timestamp\":1582045421733,\"channel\":\"mauroc\",\"data\":\"{\\\"type\\\" : \\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\": 1585948850100}\",\"name\":\"asdasd\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = EvaluateWithDelay("admin", "push_test", "on", client);

                Assert.AreEqual("on", result);

                client.Destroy();
            }
        }

        [Ignore]
        [TestMethod]
        public void GetTreatment_SplitKill_ShouldFetch()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time", "splits_push3.json", "1585948850109", "Second_Time");
                httpClientMock.SplitChangesSequence("splits_push4.json", "1585948850111", "First_Time_1");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "fb\r\nid: 123\nevent: message\ndata: {\"id\":\"1\",\"clientId\":\"emptyClientId\",\"connectionId\":\"1\",\"timestamp\":1582045421733,\"channel\":\"mauroc\",\"data\":\"{\\\"type\\\" : \\\"SPLIT_KILL\\\",\\\"changeNumber\\\": 1585948850111, \\\"defaultTreatment\\\" : \\\"off_kill\\\", \\\"splitName\\\" : \\\"push_test\\\"}\",\"name\":\"asdasd\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);
                Thread.Sleep(5000);

                var result = client.GetTreatment("admin", "push_test");

                Assert.AreEqual("after_fetch", result);

                client.Destroy();
            }
        }

        [TestMethod]
        [Ignore]
        public void GetTreatment_SplitKill_ShouldNotFetch()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "fb\r\nid: 123\nevent: message\ndata: {\"id\":\"1\",\"clientId\":\"emptyClientId\",\"connectionId\":\"1\",\"timestamp\":1582045421733,\"channel\":\"mauroc\",\"data\":\"{\\\"type\\\" : \\\"SPLIT_KILL\\\",\\\"changeNumber\\\": 1585948850110, \\\"defaultTreatment\\\" : \\\"off_kill\\\", \\\"splitName\\\" : \\\"push_test\\\"}\",\"name\":\"asdasd\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = EvaluateWithDelay("admin", "push_test", "off_kill", client);
                Assert.AreEqual("off_kill", result);

                client.Destroy();
            }
        }

        [Ignore]
        [TestMethod]
        public void GetTreatment_SegmentUpdate_ShouldFetch()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time");
                httpClientMock.SegmentChangesSequence("-1", "segment4", "split_segment4", "First_time", "1470947453878", "split_segment4_empty", "Second_time", "1470947453878", "split_segment4_updated", "Third_time");
                httpClientMock.SegmentChangesOk("1470947453879", "segment4", "split_segment4_updated_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "fb\r\nid: 123\nevent: message\ndata: {\"id\":\"1\",\"clientId\":\"emptyClientId\",\"connectionId\":\"1\",\"timestamp\":1582045421733,\"channel\":\"mauroc\",\"data\":\"{\\\"type\\\" : \\\"SEGMENT_UPDATE\\\",\\\"changeNumber\\\": 1470947453879, \\\"segmentName\\\" : \\\"segment4\\\"}\",\"name\":\"asdasd\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);
                Thread.Sleep(5000);

                var result = client.GetTreatment("test_in_segment", "feature_segment");

                Assert.AreEqual("def_test", result);

                client.Destroy();
            }
        }

        [TestMethod]
        public void Occupancy_WithPublishersAvailable()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time", "splits_push3.json", "1585948850109", "Second_Time");
                httpClientMock.SplitChangesSequence("splits_push4.json", "1585948850111", "First_Time_1");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "d4\r\nevent: message\ndata: {\"id\":\"123\",\"timestamp\":1586803930362,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"metrics\\\":{\\\"publishers\\\":2}}\",\"name\":\"[meta]occupancy\"}\n\nfb\r\nid: 123\nevent: message\ndata: {\"id\":\"1\",\"clientId\":\"emptyClientId\",\"connectionId\":\"1\",\"timestamp\":1582045421733,\"channel\":\"mauroc\",\"data\":\"{\\\"type\\\" : \\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\": 1585948850111}\",\"name\":\"asdasd\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = EvaluateWithDelay("admin", "push_test", "after_fetch", client);
                Assert.AreEqual("after_fetch", result);

                client.Destroy();
            }
        }

        [TestMethod]
        public void Occupancy_WithoutPublishersAvailable()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time", "splits_push3.json", "1585948850109", "Second_Time");
                httpClientMock.SplitChangesSequence("splits_push4.json", "1585948850111", "First_Time_1");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "d4\r\nevent: message\ndata: {\"id\":\"123\",\"timestamp\":1586803930362,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"metrics\\\":{\\\"publishers\\\":0}}\",\"name\":\"[meta]occupancy\"}\n\nfb\r\nid: 123\nevent: message\ndata: {\"id\":\"1\",\"clientId\":\"emptyClientId\",\"connectionId\":\"1\",\"timestamp\":1582045421733,\"channel\":\"mauroc\",\"data\":\"{\\\"type\\\" : \\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\": 1585948850111}\",\"name\":\"asdasd\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = client.GetTreatment("admin", "push_test");

                Assert.AreEqual("on", result);

                client.Destroy();
            }
        }

        [TestMethod]
        public void ControlMessage_StreamingPaused()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time", "splits_push3.json", "1585948850109", "Second_Time");
                httpClientMock.SplitChangesSequence("splits_push4.json", "1585948850111", "First_Time_1");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "d4\r\nevent: message\ndata: {\"id\":\"123\",\"clientId\":\"emptyClientId\",\"timestamp\":1582056812285,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_PAUSED\\\"}\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = client.GetTreatment("admin", "push_test");

                Assert.AreEqual("on", result);

                client.Destroy();
            }
        }

        [TestMethod]
        public void ControlMessage_StreamingResumed()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time", "splits_push3.json", "1585948850109", "Second_Time");
                httpClientMock.SplitChangesSequence("splits_push4.json", "1585948850111", "First_Time_1");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "d4\r\nevent: message\ndata: {\"id\":\"123\",\"clientId\":\"emptyClientId\",\"timestamp\":1582056812285,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_RESUMED\\\"}\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = client.GetTreatment("admin", "push_test");

                Assert.AreEqual("on", result);

                client.Destroy();
            }
        }

        [TestMethod]
        public void ControlMessage_StreamingDisabled()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SplitChangesSequence("splits_push.json", "-1", "First_Time_2");
                httpClientMock.SplitChangesSequence("splits_push2.json", "1585948850109", "First_Time", "splits_push3.json", "1585948850109", "Second_Time");
                httpClientMock.SplitChangesSequence("splits_push4.json", "1585948850111", "First_Time_1");
                httpClientMock.SegmentChangesOk("-1", "segment4");
                httpClientMock.SegmentChangesOk("1470947453878", "segment4", "split_segment4_empty");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                var notification = "d4\r\nevent: message\ndata: {\"id\":\"123\",\"clientId\":\"emptyClientId\",\"timestamp\":1582056812285,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_DISABLED\\\"}\"}\n\n\r\n";
                httpClientMock.SSE_Channels_Response_WithPath(EventSourcePath, notification);

                var authResponse = new AuthenticationResponse
                {
                    PushEnabled = true,
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9"
                };

                httpClientMock.AuthService_Response(JsonConvertWrapper.SerializeObject(authResponse));

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    FeaturesRefreshRate = 3000,
                    SegmentsRefreshRate = 3000,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = client.GetTreatment("admin", "push_test");

                Assert.AreEqual("on", result);

                client.Destroy();
            }
        }

        [TestMethod]
        public void GetTreatment_WithAuthBadReques_ShouldFallbackToPolling()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                // Arrange.
                var changes1 = new TargetingRulesDto
                {
                    RuleBasedSegments = new ChangesDto<RuleBasedSegmentDto>
                    {
                        Since = -1,
                        Till = 11,
                        Data = new List<RuleBasedSegmentDto>
                        {
                            new RuleBasedSegmentDto
                            {
                                Status = "ACTIVE",
                                Name = "rbs_test",
                                ChangeNumber = 1,
                                Excluded = new Excluded
                                {
                                    Keys = new List<string>(),
                                    Segments = new List<ExcludedSegments>()
                                },
                                Conditions = new List<ConditionDefinition>
                                {
                                    new ConditionDefinition
                                    {
                                        matcherGroup = new MatcherGroupDefinition
                                        {
                                            matchers = new List<MatcherDefinition>
                                            {
                                                new MatcherDefinition
                                                {
                                                    whitelistMatcherData = new WhitelistData
                                                    {
                                                        whitelist = new List<string>{ "mauro" }
                                                    },
                                                    matcherType = "WHITELIST"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    FeatureFlags = new ChangesDto<Split>
                    {
                        Since = -1,
                        Till = 10,
                        Data = new List<Split>()
                        {
                            new Split
                            {
                                name = "split-name-1",
                                changeNumber = 10,
                                conditions = new List<ConditionDefinition>
                                {
                                    new ConditionDefinition
                                    {
                                        conditionType = "ROLLOUT",
                                        partitions = new List<PartitionDefinition>
                                        {
                                            new PartitionDefinition
                                            {
                                                size = 100,
                                                treatment = "on"
                                            }
                                        },
                                        matcherGroup = new MatcherGroupDefinition
                                        {
                                            matchers = new List<MatcherDefinition>
                                            {
                                                new MatcherDefinition
                                                {
                                                    whitelistMatcherData = new WhitelistData
                                                    {
                                                        whitelist = new List<string>{ "mauro" }
                                                    },
                                                    matcherType = "WHITELIST"
                                                }
                                            }
                                        }
                                    }
                                },
                                defaultTreatment = "on",
                                killed = false,
                                status = "ACTIVE"
                            }
                        }
                    }
                };
                var changes2 = new TargetingRulesDto
                {
                    RuleBasedSegments = new ChangesDto<RuleBasedSegmentDto>
                    {
                        Since = 11,
                        Till = 11,
                        Data = new List<RuleBasedSegmentDto>()
                    },
                    FeatureFlags = new ChangesDto<Split>
                    {
                        Since = 10,
                        Till = 10,
                        Data = new List<Split>()
                    }
                };

                var changes3 = new TargetingRulesDto
                {
                    RuleBasedSegments = new ChangesDto<RuleBasedSegmentDto>
                    {
                        Since = 11,
                        Till = 11,
                        Data = new List<RuleBasedSegmentDto>()
                    },
                    FeatureFlags = new ChangesDto<Split>
                    {
                        Since = 10,
                        Till = 11,
                        Data = new List<Split>()
                        {
                            new Split
                            {
                                name = "split-name-1",
                                changeNumber = 11,
                                conditions = new List<ConditionDefinition>
                                {
                                    new ConditionDefinition
                                    {
                                        conditionType = "ROLLOUT",
                                        partitions = new List<PartitionDefinition>
                                        {
                                            new PartitionDefinition
                                            {
                                                size = 100,
                                                treatment = "off"
                                            }
                                        },
                                        matcherGroup = new MatcherGroupDefinition
                                        {
                                            matchers = new List<MatcherDefinition>
                                            {
                                                new MatcherDefinition
                                                {
                                                    whitelistMatcherData = new WhitelistData
                                                    {
                                                        whitelist = new List<string>{ "mauro" }
                                                    },
                                                    matcherType = "WHITELIST"
                                                }
                                            }
                                        }
                                    }
                                },
                                defaultTreatment = "off",
                                killed = false,
                                status = "ACTIVE"
                            }
                        }
                    }
                };

                var changes4 = new TargetingRulesDto
                {
                    RuleBasedSegments = new ChangesDto<RuleBasedSegmentDto>
                    {
                        Since = 11,
                        Till = 11,
                        Data = new List<RuleBasedSegmentDto>()
                    },
                    FeatureFlags = new ChangesDto<Split>
                    {
                        Since = 11,
                        Till = 11,
                        Data = new List<Split>()
                    }
                };

                httpClientMock.SplitChangesOkWithBody(JsonConvertWrapper.SerializeObject(changes1), "-1", "-1");
                httpClientMock.SplitChangesOkWithBody(JsonConvertWrapper.SerializeObject(changes2), "10", "11");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                httpClientMock.AuthService_Response_BadRequest();

                var url = httpClientMock.GetUrl();
                var config = new ConfigurationOptions
                {
                    Endpoint = url,
                    EventsEndpoint = url,
                    AuthServiceURL = $"{url}/api/auth",
                    StreamingServiceURL = $"{url}{EventSourcePath}",
                    StreamingEnabled = true,
                    Logger = SplitLogger.Console(Level.Debug),
                    FeaturesRefreshRate = 1
                };

                var splitFactory = new SplitFactory("api-key-fallback", config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                // Act and Assert.
                var result = client.GetTreatment("admin", "split-name-1");
                Assert.AreEqual("on", result);

                httpClientMock.SplitChangesOkWithBody(JsonConvertWrapper.SerializeObject(changes3), "10", "11");
                httpClientMock.SplitChangesOkWithBody(JsonConvertWrapper.SerializeObject(changes4), "11", "11");

                Thread.Sleep(3000);

                result = client.GetTreatment("admin", "split-name-1");
                Assert.AreEqual("off", result);

                client.Destroy();
            }
        }

        public static string EvaluateWithDelay(string key, string splitName, string expected, ISplitClient client, int attemps = 10)
        {
            var result = string.Empty;
            for (int i = 0; i < attemps; i++)
            {
                result = client.GetTreatment(key, splitName);
                if (result == expected) break;

                Thread.Sleep(1000);
            }

            return result;
        }
    }
}
