using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.EventSource;

namespace Splitio_Tests.Unit_Tests.EventSource
{
    [TestClass]
    public class NotificationParserTests
    {
        private readonly INotificationParser _notificationParser;

        public NotificationParserTests()
        {
            _notificationParser = new NotificationParser();
        }

        #region WithoutSpace
        [TestMethod]
        public void Parse_Control_StreamingDisabledShouldReturnParsedEvent_WithoutSpace()
        {
            // Arrange.
            var text = "event:message\ndata:{\"id\":\"2222\",\"clientId\":\"3333\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_DISABLED\\\"}\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(NotificationType.CONTROL, result.Type);
            Assert.AreEqual(ControlType.STREAMING_DISABLED, ((ControlNotification)result).ControlType);
            Assert.AreEqual("control_pri", result.Channel);
        }
        #endregion

        [TestMethod]
        public void Parse_SlitUpdate_ShouldReturnParsedEvent()
        {
            // Arrange.
            var text = "id: e7dsDAgMQAkPkG@1588254699243-0\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1585867723838}\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(1585867723838, ((SplitChangeNotification)result).ChangeNumber);
            Assert.AreEqual(NotificationType.SPLIT_UPDATE, result.Type);
            Assert.AreEqual("xxxx_xxxx_splits", result.Channel);
        }

        [TestMethod]
        public void Parse_SlitKill_ShouldReturnParsedEvent()
        {
            // Arrange.
            var text = "id: e7dsDAgMQAkPkG@1588254699243-0\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_KILL\\\",\\\"changeNumber\\\":1585868246622,\\\"defaultTreatment\\\":\\\"off\\\",\\\"splitName\\\":\\\"test-split\\\"}\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(1585868246622, ((SplitKillNotification)result).ChangeNumber);
            Assert.AreEqual("off", ((SplitKillNotification)result).DefaultTreatment);
            Assert.AreEqual("test-split", ((SplitKillNotification)result).SplitName);
            Assert.AreEqual(NotificationType.SPLIT_KILL, result.Type);
            Assert.AreEqual("xxxx_xxxx_splits", result.Channel);
        }

        [TestMethod]
        public void Parse_SegmentUpdate_ShouldReturnParsedEvent()
        {
            // Arrange.
            var text = "id: e7dsDAgMQAkPkG@1588254699243-0\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_segments\",\"data\":\"{\\\"type\\\":\\\"SEGMENT_UPDATE\\\",\\\"changeNumber\\\":1588254698186,\\\"segmentName\\\":\\\"test-segment\\\"}\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(1588254698186, ((SegmentChangeNotification)result).ChangeNumber);
            Assert.AreEqual("test-segment", ((SegmentChangeNotification)result).SegmentName);
            Assert.AreEqual(NotificationType.SEGMENT_UPDATE, result.Type);
            Assert.AreEqual("xxxx_xxxx_segments", result.Channel);
        }

        [TestMethod]
        public void Parse_IncorrectFormat_ShouldReturnException()
        {
            // Arrange.
            var text = @"{ 'event': 'message', 
                           'data': {
                            'id':'1',
                            'channel':'mauroc',
                            'content': {
                                'type': 'CONTROL', 
                                'controlType': 'test-control-type'
                            },
                            'name':'name-test'
                         }
                        }";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Parse_NotificationError_ShouldReturnError()
        {
            // Arrange.
            var text = "event: error\ndata: {\"message\":\"Token expired\",\"code\":40142,\"statusCode\":401,\"href\":\"https://help.ably.io/error/40142\"}\n\n";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(NotificationType.ERROR, result.Type);
            Assert.AreEqual(40142, ((NotificationError)result).Code);
            Assert.AreEqual(401, ((NotificationError)result).StatusCode);
        }

        [TestMethod]
        public void Parse_Occupancy_ControlPri_ShouldReturnParsedEvent()
        {
            // Arrange.
            var text = "event: message\ndata: {\"id\":\"NhK8u2JPan:0:0\",\"timestamp\":1588254668328,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"metrics\\\":{\\\"publishers\\\":2}}\",\"name\":\"[meta]occupancy\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(NotificationType.OCCUPANCY, result.Type);
            Assert.AreEqual(2, ((OccupancyNotification)result).Metrics.Publishers);
            Assert.AreEqual("control_pri", result.Channel);
        }

        [TestMethod]
        public void Parse_Occupancy_ControlSec_ShouldReturnParsedEvent()
        {
            // Arrange.
            var text = "event: message\ndata: {\"id\":\"NhK8u2JPan:0:0\",\"timestamp\":1588254668328,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_sec\",\"data\":\"{\\\"metrics\\\":{\\\"publishers\\\":1}}\",\"name\":\"[meta]occupancy\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(NotificationType.OCCUPANCY, result.Type);
            Assert.AreEqual(1, ((OccupancyNotification)result).Metrics.Publishers);
            Assert.AreEqual("control_sec", result.Channel);
        }

        [TestMethod]
        public void Parse_Control_StreamingPaused_ShouldReturnParsedEvent()
        {
            // Arrange.
            var text = "event: message\ndata: {\"id\":\"2222\",\"clientId\":\"3333\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_PAUSED\\\"}\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(NotificationType.CONTROL, result.Type);
            Assert.AreEqual(ControlType.STREAMING_PAUSED, ((ControlNotification)result).ControlType);
            Assert.AreEqual("control_pri", result.Channel);
        }

        [TestMethod]
        public void Parse_Control_StreamingResumed_ShouldReturnParsedEvent()
        {
            // Arrange.
            var text = "event: message\ndata: {\"id\":\"2222\",\"clientId\":\"3333\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_RESUMED\\\"}\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(NotificationType.CONTROL, result.Type);
            Assert.AreEqual(ControlType.STREAMING_RESUMED, ((ControlNotification)result).ControlType);
            Assert.AreEqual("control_pri", result.Channel);
        }

        [TestMethod]
        public void Parse_Control_StreamingDisabledShouldReturnParsedEvent()
        {
            // Arrange.
            var text = "event: message\ndata: {\"id\":\"2222\",\"clientId\":\"3333\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_DISABLED\\\"}\"}";

            // Act.
            var result = _notificationParser.Parse(text);

            // Assert.
            Assert.AreEqual(NotificationType.CONTROL, result.Type);
            Assert.AreEqual(ControlType.STREAMING_DISABLED, ((ControlNotification)result).ControlType);
            Assert.AreEqual("control_pri", result.Channel);
        }

        [TestMethod]
        public void ParseSlitUpdateGZipShouldReturnParsedEvent()
        {
            // Arrange.
            var message = "id: 123123\nevent: message\ndata: {\"id\":\"1111\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1684265694505,\\\"pcn\\\":111,\\\"c\\\":1,\\\"d\\\":\\\"H4sIAAAAAAAA/8yT327aTBDFXyU612vJxoTgvUMfKB8qcaSapqoihAZ7DNusvWi9TpUiv3tl/pdQVb1qL+cwc3bOj/EGzlKeq3T6tuaYCoZEXbGFgMogkXXDIM0y31v4C/aCgMnrU9/3gl7Pp4yilMMIAuVusqDamvlXeiWIg/FAa5OSU6aEDHz/ip4wZ5Be1AmjoBsFAtVOCO56UXh31/O7ApUjV1eQGPw3HT+NIPCitG7bctIVC2ScU63d1DK5gksHCZPnEEhXVC45rosFW8ig1++GYej3g85tJEB6aSA7Aqkpc7Ws7XahCnLTbLVM7evnzalsUUHi8//j6WgyTqYQKMilK7b31tRryLa3WKiyfRCDeHhq2Dntiys+JS/J8THUt5VyrFXlHnYTQ3LU2h91yGdQVqhy+0RtTeuhUoNZ08wagTVZdxbBndF5vYVApb7z9m9pZgKaFqwhT+6coRHvg398nEweP/157Bd+S1hz6oxtm88O73B0jbhgM47nyej+YRRfgdNODDlXJWcJL9tUF5SqnRqfbtPr4LdcTHnk4rfp3buLOkG7+Pmp++vRM9w/wVblzX7Pm8OGfxf5YDKZfxh9SS6B/2Pc9t/7ja01o5k1PwIAAP//uTipVskEAAA=\\\"}\"}";

            // Act.
            var result = _notificationParser.Parse(message);

            // Assert.
            Assert.AreEqual(NotificationType.SPLIT_UPDATE, result.Type);
            Assert.AreEqual("xxxx_xxxx_splits", result.Channel);
            var changeNotification = (SplitChangeNotification)result;
            Assert.AreEqual(1684265694505, changeNotification.ChangeNumber);
            Assert.AreEqual(111, changeNotification.PreviousChangeNumber);
            Assert.AreEqual(CompressionType.Gzip, changeNotification.CompressionType);
            Assert.AreEqual("mauro_java", changeNotification.FeatureFlag.name);
            Assert.AreEqual("ACTIVE", changeNotification.FeatureFlag.status);
            Assert.AreEqual("off", changeNotification.FeatureFlag.defaultTreatment);
        }

        [TestMethod]
        public void ParseSlitUpdateZLibShouldReturnParsedEvent()
        {
            // Arrange.
            var message = "id: 123123\nevent: message\ndata: {\"id\":\"1111\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1684265694505,\\\"pcn\\\":111,\\\"c\\\":2,\\\"d\\\":\\\"eJzMk99u2kwQxV8lOtdryQZj8N6hD5QPlThSTVNVEUKDPYZt1jZar1OlyO9emf8lVFWv2ss5zJyd82O8hTWUZSqZvW04opwhUVdsIKBSSKR+10vS1HWW7pIdz2NyBjRwHS8IXEopTLgbQqDYT+ZUm3LxlV4J4mg81LpMyKqygPRc94YeM6eQTtjphp4fegLVXvD6Qdjt9wPXF6gs2bqCxPC/2eRpDIEXpXXblpGuWCDljGptZ4bJ5lxYSJRZBoFkTcWKozpfsoH0goHfCXpB6PfcngDpVQnZEUjKIlOr2uwWqiC3zU5L1aF+3p7LFhUkPv8/mY2nk3gGgZxssmZzb8p6A9n25ktVtA9iGI3ODXunQ3HDp+AVWT6F+rZWlrWq7MN+YkSWWvuTDvkMSnNV7J6oTdl6qKTEvGnmjcCGjL2IYC/ovPYgUKnvvPtbmrmApiVryLM7p2jE++AfH6fTx09/HvuF32LWnNjStM0Xh3c8ukZcsZlEi3h8/zCObsBpJ0acqYLTmFdtqitK1V6NzrfpdPBbLmVx4uK26e27izpDu/r5yf/16AXun2Cr4u6w591xw7+LfDidLj6Mv8TXwP8xbofv/c7UmtHMmx8BAAD//0fclvU=\\\"}\"}";
            
            // Act.
            var result = _notificationParser.Parse(message);

            // Assert.
            Assert.AreEqual(NotificationType.SPLIT_UPDATE, result.Type);
            Assert.AreEqual("xxxx_xxxx_splits", result.Channel);
            var changeNotification = (SplitChangeNotification)result;
            Assert.AreEqual(1684265694505, changeNotification.ChangeNumber);
            Assert.AreEqual(111, changeNotification.PreviousChangeNumber);
            Assert.AreEqual(CompressionType.Zlib, changeNotification.CompressionType);
            Assert.AreEqual("mauro_java", changeNotification.FeatureFlag.name);
            Assert.AreEqual("ACTIVE", changeNotification.FeatureFlag.status);
            Assert.AreEqual("off", changeNotification.FeatureFlag.defaultTreatment);
        }

        [TestMethod]
        public void ParseSlitUpdateBase64ShouldReturnParsedEvent()
        {
            // Arrange.
            var message = "id: 123123\nevent: message\ndata: {\"id\":\"1111\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1684265694505,\\\"pcn\\\":111,\\\"c\\\":0,\\\"d\\\":\\\"eyJ0cmFmZmljVHlwZU5hbWUiOiJ1c2VyIiwiaWQiOiJkNDMxY2RkMC1iMGJlLTExZWEtOGE4MC0xNjYwYWRhOWNlMzkiLCJuYW1lIjoibWF1cm9famF2YSIsInRyYWZmaWNBbGxvY2F0aW9uIjoxMDAsInRyYWZmaWNBbGxvY2F0aW9uU2VlZCI6LTkyMzkxNDkxLCJzZWVkIjotMTc2OTM3NzYwNCwic3RhdHVzIjoiQUNUSVZFIiwia2lsbGVkIjpmYWxzZSwiZGVmYXVsdFRyZWF0bWVudCI6Im9mZiIsImNoYW5nZU51bWJlciI6MTY4NDMyOTg1NDM4NSwiYWxnbyI6MiwiY29uZmlndXJhdGlvbnMiOnt9LCJjb25kaXRpb25zIjpbeyJjb25kaXRpb25UeXBlIjoiV0hJVEVMSVNUIiwibWF0Y2hlckdyb3VwIjp7ImNvbWJpbmVyIjoiQU5EIiwibWF0Y2hlcnMiOlt7Im1hdGNoZXJUeXBlIjoiV0hJVEVMSVNUIiwibmVnYXRlIjpmYWxzZSwid2hpdGVsaXN0TWF0Y2hlckRhdGEiOnsid2hpdGVsaXN0IjpbImFkbWluIiwibWF1cm8iLCJuaWNvIl19fV19LCJwYXJ0aXRpb25zIjpbeyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9XSwibGFiZWwiOiJ3aGl0ZWxpc3RlZCJ9LHsiY29uZGl0aW9uVHlwZSI6IlJPTExPVVQiLCJtYXRjaGVyR3JvdXAiOnsiY29tYmluZXIiOiJBTkQiLCJtYXRjaGVycyI6W3sia2V5U2VsZWN0b3IiOnsidHJhZmZpY1R5cGUiOiJ1c2VyIn0sIm1hdGNoZXJUeXBlIjoiSU5fU0VHTUVOVCIsIm5lZ2F0ZSI6ZmFsc2UsInVzZXJEZWZpbmVkU2VnbWVudE1hdGNoZXJEYXRhIjp7InNlZ21lbnROYW1lIjoibWF1ci0yIn19XX0sInBhcnRpdGlvbnMiOlt7InRyZWF0bWVudCI6Im9uIiwic2l6ZSI6MH0seyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9LHsidHJlYXRtZW50IjoiVjQiLCJzaXplIjowfSx7InRyZWF0bWVudCI6InY1Iiwic2l6ZSI6MH1dLCJsYWJlbCI6ImluIHNlZ21lbnQgbWF1ci0yIn0seyJjb25kaXRpb25UeXBlIjoiUk9MTE9VVCIsIm1hdGNoZXJHcm91cCI6eyJjb21iaW5lciI6IkFORCIsIm1hdGNoZXJzIjpbeyJrZXlTZWxlY3RvciI6eyJ0cmFmZmljVHlwZSI6InVzZXIifSwibWF0Y2hlclR5cGUiOiJBTExfS0VZUyIsIm5lZ2F0ZSI6ZmFsc2V9XX0sInBhcnRpdGlvbnMiOlt7InRyZWF0bWVudCI6Im9uIiwic2l6ZSI6MH0seyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9LHsidHJlYXRtZW50IjoiVjQiLCJzaXplIjowfSx7InRyZWF0bWVudCI6InY1Iiwic2l6ZSI6MH1dLCJsYWJlbCI6ImRlZmF1bHQgcnVsZSJ9XX0=\\\"}\"}";


            // Act.
            var result = _notificationParser.Parse(message);

            // Assert.
            Assert.AreEqual(NotificationType.SPLIT_UPDATE, result.Type);
            Assert.AreEqual("xxxx_xxxx_splits", result.Channel);
            var changeNotification = (SplitChangeNotification)result;
            Assert.AreEqual(1684265694505, changeNotification.ChangeNumber);
            Assert.AreEqual(111, changeNotification.PreviousChangeNumber);
            Assert.AreEqual(CompressionType.NotCompressed, changeNotification.CompressionType);
            Assert.AreEqual("mauro_java", changeNotification.FeatureFlag.name);
            Assert.AreEqual("ACTIVE", changeNotification.FeatureFlag.status);
            Assert.AreEqual("off", changeNotification.FeatureFlag.defaultTreatment);
        }
    }
}
