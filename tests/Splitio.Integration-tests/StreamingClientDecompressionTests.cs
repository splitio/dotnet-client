﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Tests.Common;

namespace Splitio.Integration_tests
{
    [TestClass, TestCategory("Integration")]
    public class StreamingClientDecompressionTests
    {
        private const string TreatmentExpected = "v5";

        [TestMethod]
        public void GetTreatmentSplitUpdateBase64ShouldAddOrUpdate()
        {
            // Arrange.
            var notification = "id: 123123\nevent: message\ndata: {\"id\":\"1111\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1684265694505,\\\"pcn\\\":-1,\\\"c\\\":0,\\\"d\\\":\\\"eyJ0cmFmZmljVHlwZU5hbWUiOiJ1c2VyIiwiaWQiOiJkNDMxY2RkMC1iMGJlLTExZWEtOGE4MC0xNjYwYWRhOWNlMzkiLCJuYW1lIjoibWF1cm9famF2YSIsInRyYWZmaWNBbGxvY2F0aW9uIjoxMDAsInRyYWZmaWNBbGxvY2F0aW9uU2VlZCI6LTkyMzkxNDkxLCJzZWVkIjotMTc2OTM3NzYwNCwic3RhdHVzIjoiQUNUSVZFIiwia2lsbGVkIjpmYWxzZSwiZGVmYXVsdFRyZWF0bWVudCI6Im9mZiIsImNoYW5nZU51bWJlciI6MTY4NDMyOTg1NDM4NSwiYWxnbyI6MiwiY29uZmlndXJhdGlvbnMiOnt9LCJjb25kaXRpb25zIjpbeyJjb25kaXRpb25UeXBlIjoiV0hJVEVMSVNUIiwibWF0Y2hlckdyb3VwIjp7ImNvbWJpbmVyIjoiQU5EIiwibWF0Y2hlcnMiOlt7Im1hdGNoZXJUeXBlIjoiV0hJVEVMSVNUIiwibmVnYXRlIjpmYWxzZSwid2hpdGVsaXN0TWF0Y2hlckRhdGEiOnsid2hpdGVsaXN0IjpbImFkbWluIiwibWF1cm8iLCJuaWNvIl19fV19LCJwYXJ0aXRpb25zIjpbeyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9XSwibGFiZWwiOiJ3aGl0ZWxpc3RlZCJ9LHsiY29uZGl0aW9uVHlwZSI6IlJPTExPVVQiLCJtYXRjaGVyR3JvdXAiOnsiY29tYmluZXIiOiJBTkQiLCJtYXRjaGVycyI6W3sia2V5U2VsZWN0b3IiOnsidHJhZmZpY1R5cGUiOiJ1c2VyIn0sIm1hdGNoZXJUeXBlIjoiSU5fU0VHTUVOVCIsIm5lZ2F0ZSI6ZmFsc2UsInVzZXJEZWZpbmVkU2VnbWVudE1hdGNoZXJEYXRhIjp7InNlZ21lbnROYW1lIjoibWF1ci0yIn19XX0sInBhcnRpdGlvbnMiOlt7InRyZWF0bWVudCI6Im9uIiwic2l6ZSI6MH0seyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9LHsidHJlYXRtZW50IjoiVjQiLCJzaXplIjowfSx7InRyZWF0bWVudCI6InY1Iiwic2l6ZSI6MH1dLCJsYWJlbCI6ImluIHNlZ21lbnQgbWF1ci0yIn0seyJjb25kaXRpb25UeXBlIjoiUk9MTE9VVCIsIm1hdGNoZXJHcm91cCI6eyJjb21iaW5lciI6IkFORCIsIm1hdGNoZXJzIjpbeyJrZXlTZWxlY3RvciI6eyJ0cmFmZmljVHlwZSI6InVzZXIifSwibWF0Y2hlclR5cGUiOiJBTExfS0VZUyIsIm5lZ2F0ZSI6ZmFsc2V9XX0sInBhcnRpdGlvbnMiOlt7InRyZWF0bWVudCI6Im9uIiwic2l6ZSI6MH0seyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9LHsidHJlYXRtZW50IjoiVjQiLCJzaXplIjowfSx7InRyZWF0bWVudCI6InY1Iiwic2l6ZSI6MH1dLCJsYWJlbCI6ImRlZmF1bHQgcnVsZSJ9XX0=\\\"}\"}\n\n";

            // Act.
            var result = EvaluateGetTreatment(notification, "admin", "mauro_java", "off");

            // Assert.
            Assert.AreEqual("off", result);
        }

        [TestMethod]
        public void GetTreatmentSplitUpdateZLibShouldAddOrUpdate()
        {
            // Arrange.
            var notification = "id: 123123\nevent: message\ndata: {\"id\":\"1111\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1684265694505,\\\"pcn\\\":-1,\\\"c\\\":2,\\\"d\\\":\\\"eJzMk99u2kwQxV8lOtdryQZj8N6hD5QPlThSTVNVEUKDPYZt1jZar1OlyO9emf8lVFWv2ss5zJyd82O8hTWUZSqZvW04opwhUVdsIKBSSKR+10vS1HWW7pIdz2NyBjRwHS8IXEopTLgbQqDYT+ZUm3LxlV4J4mg81LpMyKqygPRc94YeM6eQTtjphp4fegLVXvD6Qdjt9wPXF6gs2bqCxPC/2eRpDIEXpXXblpGuWCDljGptZ4bJ5lxYSJRZBoFkTcWKozpfsoH0goHfCXpB6PfcngDpVQnZEUjKIlOr2uwWqiC3zU5L1aF+3p7LFhUkPv8/mY2nk3gGgZxssmZzb8p6A9n25ktVtA9iGI3ODXunQ3HDp+AVWT6F+rZWlrWq7MN+YkSWWvuTDvkMSnNV7J6oTdl6qKTEvGnmjcCGjL2IYC/ovPYgUKnvvPtbmrmApiVryLM7p2jE++AfH6fTx09/HvuF32LWnNjStM0Xh3c8ukZcsZlEi3h8/zCObsBpJ0acqYLTmFdtqitK1V6NzrfpdPBbLmVx4uK26e27izpDu/r5yf/16AXun2Cr4u6w591xw7+LfDidLj6Mv8TXwP8xbofv/c7UmtHMmx8BAAD//0fclvU=\\\"}\"}\n\n";

            // Act.
            var result = EvaluateGetTreatment(notification, "admin", "mauro_java", TreatmentExpected);

            // Assert.
            Assert.AreEqual(TreatmentExpected, result);
        }

        [TestMethod]
        public void GetTreatmentSplitUpdateGZipShouldAddOrUpdate()
        {
            // Arrange.
            var notification = "id: 123123\nevent: message\ndata: {\"id\":\"1111\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1684265694505,\\\"pcn\\\":-1,\\\"c\\\":1,\\\"d\\\":\\\"H4sIAAAAAAAA/8yT327aTBDFXyU612vJxoTgvUMfKB8qcaSapqoihAZ7DNusvWi9TpUiv3tl/pdQVb1qL+cwc3bOj/EGzlKeq3T6tuaYCoZEXbGFgMogkXXDIM0y31v4C/aCgMnrU9/3gl7Pp4yilMMIAuVusqDamvlXeiWIg/FAa5OSU6aEDHz/ip4wZ5Be1AmjoBsFAtVOCO56UXh31/O7ApUjV1eQGPw3HT+NIPCitG7bctIVC2ScU63d1DK5gksHCZPnEEhXVC45rosFW8ig1++GYej3g85tJEB6aSA7Aqkpc7Ws7XahCnLTbLVM7evnzalsUUHi8//j6WgyTqYQKMilK7b31tRryLa3WKiyfRCDeHhq2Dntiys+JS/J8THUt5VyrFXlHnYTQ3LU2h91yGdQVqhy+0RtTeuhUoNZ08wagTVZdxbBndF5vYVApb7z9m9pZgKaFqwhT+6coRHvg398nEweP/157Bd+S1hz6oxtm88O73B0jbhgM47nyej+YRRfgdNODDlXJWcJL9tUF5SqnRqfbtPr4LdcTHnk4rfp3buLOkG7+Pmp++vRM9w/wVblzX7Pm8OGfxf5YDKZfxh9SS6B/2Pc9t/7ja01o5k1PwIAAP//uTipVskEAAA=\\\"}\"}\n\n";

            // Act.
            var result = EvaluateGetTreatment(notification, "admin", "mauro_java", "v5");

            // Assert.
            Assert.AreEqual(TreatmentExpected, result);
        }

        private static string EvaluateGetTreatment(string notification, string key, string featureName, string treatmentExpected)
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var payload = @"{'rbs':{'d': [],'s': -1,'t': -1}, 'ff':{'d': [],'s': -1,'t': -1}}";
                httpClientMock.SplitChangesOkWithBody(payload, "-1", "-1");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, "ok");

                httpClientMock.SSE_Channels_Response_WithPath(StreamingClientTests.EventSourcePath, notification);

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
                    StreamingServiceURL = $"{url}{StreamingClientTests.EventSourcePath}",
                    StreamingEnabled = true
                };

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, config);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                var result = StreamingClientTests.EvaluateWithDelay(key, featureName, treatmentExpected, client);

                client.Destroy();

                return result;
            }
        }
    }
}
