using Splitio.Integration_tests.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Splitio.Integration_tests
{
    public class HttpClientMock : IDisposable
    {
        private readonly FluentMockServer _mockServer;
        private readonly string rootFilePath;

        public HttpClientMock()
        {
            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif

            _mockServer = FluentMockServer.Start();
        }

        #region SplitChanges
        public void SplitChangesOk(string fileName, string since)
        {
            var jsonBody = File.ReadAllText($"{rootFilePath}{fileName}");

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath("/api/splitChanges")
                    .WithParam("since", since)
                    .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(jsonBody));
        }

        public void SplitChangesSequence(string firstFileName, string firstSince, string firstState, string secondFileName = null, string secondSince = null, string secondState = null)
        {
            var jsonBody = File.ReadAllText($"{rootFilePath}{firstFileName}");

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath("/api/splitChanges")
                    .WithParam("since", firstSince)
                    .UsingGet()
                )
                .InScenario(firstSince)
                .WillSetStateTo(firstState)
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(jsonBody));

            if (!string.IsNullOrEmpty(secondFileName))
            {
                jsonBody = File.ReadAllText($"{rootFilePath}{secondFileName}");

                _mockServer
                    .Given(
                        Request.Create()
                        .WithPath("/api/splitChanges")
                        .WithParam("since", secondSince)
                        .UsingGet()
                    )
                    .InScenario(firstSince)
                    .WhenStateIs(firstState)
                    .WillSetStateTo(secondState)
                    .RespondWith(
                        Response.Create()
                        .WithStatusCode(200)
                        .WithBody(jsonBody));
            }
        }

        public void SplitChangesError(StatusCodeEnum statusCode)
        {
            var body = string.Empty;

            switch (statusCode)
            {
                case StatusCodeEnum.BadRequest:
                    body = "Bad Request";
                    break;
                case StatusCodeEnum.InternalServerError:
                    body = "Internal Server Error";
                    break;
            }

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath("/api/splitChanges*")
                    .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode((int)statusCode)
                    .WithBody(body));
        }
        #endregion

        #region SegmentChanges        
        public void SegmentChangesOk(string since, string segmentName)
        {
            var json = File.ReadAllText($"{rootFilePath}split_{segmentName}.json");

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath($"/api/segmentChanges/{segmentName}")
                    .WithParam("since", since)
                    .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(json));
        }

        public void SegmentChangesOk(string since, string segmentName, string fileName)
        {
            var json = File.ReadAllText($"{rootFilePath}{fileName}.json");

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath($"/api/segmentChanges/{segmentName}")
                    .WithParam("since", since)
                    .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(json));
        }

        public void SegmentChangesSequence(string since, string segmentName, string fileName, string firstState, string secondSince, string secondFileName, string secondState, string thirdSince, string thirdFileName, string thirdState)
        {
            var json = File.ReadAllText($"{rootFilePath}{fileName}.json");

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath($"/api/segmentChanges/{segmentName}")
                    .WithParam("since", since)
                    .UsingGet()
                )
                .InScenario(segmentName)
                .WillSetStateTo(firstState)
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(json));

            json = File.ReadAllText($"{rootFilePath}{secondFileName}.json");

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath($"/api/segmentChanges/{segmentName}")
                    .WithParam("since", secondSince)
                    .UsingGet()
                )
                .InScenario(segmentName)
                .WhenStateIs(firstState)
                .WillSetStateTo(secondState)
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(json));
            

            json = File.ReadAllText($"{rootFilePath}{thirdFileName}.json");

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath($"/api/segmentChanges/{segmentName}")
                    .WithParam("since", thirdSince)
                    .UsingGet()
                )
                .InScenario(segmentName)
                .WhenStateIs(secondState)
                .WillSetStateTo(thirdState)
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(json));
        }

        public void SegmentChangesError(StatusCodeEnum statusCode)
        {
            var body = string.Empty;

            switch (statusCode)
            {
                case StatusCodeEnum.BadRequest:
                    body = "Bad Request";
                    break;
                case StatusCodeEnum.InternalServerError:
                    body = "Internal Server Error";
                    break;
            }

            _mockServer
                .Given(
                    Request.Create()
                    .WithPath($"/api/segmentChanges*")
                    .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode((int)statusCode)
                    .WithBody(body));
        }
        #endregion

        #region SSE
        public void SSE_Channels_Response(string bodyExpected)
        {
            _mockServer
                .Given(
                    Request.Create()
                    .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(bodyExpected));
        }

        public void SSE_Channels_Response_WithPath(string path, string bodyExpected)
        {
            _mockServer
                .Given(
                    Request.Create()
                    .WithPath(path)
                    .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(bodyExpected));
        }
        #endregion

        #region Auth Service
        public void AuthService_Response(string bodyExoected)
        {
            _mockServer
                .Given(
                    Request.Create()
                    .WithPath("/api/auth")
                    .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(200)
                    .WithBody(bodyExoected));
        }
        #endregion

        #region Posts
        public void Post_Response(string url, int statusCode, string bodyResponse)
        {
            _mockServer
                .Given(
                    Request.Create()
                    .WithPath(url)
                    .UsingPost()
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(statusCode)
                    .WithBody(bodyResponse));
        }

        public void Post_Response(string url, int statusCode, string data, string bodyResponse)
        {
            _mockServer
                .Given(
                    Request.Create()
                    .WithPath(url)
                    .UsingPost()
                    .WithBody(data)
                )
                .RespondWith(
                    Response.Create()
                    .WithStatusCode(statusCode)
                    .WithBody(bodyResponse));
        }
        #endregion

        public string GetUrl()
        {
            return _mockServer.Urls.FirstOrDefault();
        }

        public List<LogEntry> GetLogs()
        {
            if (_mockServer.LogEntries.Count() == 0)
                return new List<LogEntry>();

            return _mockServer.LogEntries.ToList();
        }

        public List<LogEntry> GetImpressionLogs()
        {
            return _mockServer
                .LogEntries
                .Where(l => l.RequestMessage.AbsolutePath.Contains("api/testImpressions/bulk"))
                .ToList();
        }

        public List<LogEntry> GetImpressionCountsLogs()
        {
            return _mockServer
                .LogEntries
                .Where(l => l.RequestMessage.AbsolutePath.Contains("api/testImpressions/count"))
                .ToList();
        }

        public List<LogEntry> GetEventsLog()
        {
            return _mockServer
                .LogEntries
                .Where(l => l.RequestMessage.AbsolutePath.Contains("api/events/bulk"))
                .ToList();
        }

        public List<LogEntry> GetMetricsConfigLog()
        {
            return _mockServer
                .LogEntries
                .Where(l => l.RequestMessage.AbsolutePath.Contains("metrics/config"))
                .ToList();
        }

        public List<LogEntry> GetMetricsUsageLog()
        {
            return _mockServer
                .LogEntries
                .Where(l => l.RequestMessage.AbsolutePath.Contains("metrics/usage"))
                .ToList();
        }

        public void Dispose()
        {
            _mockServer.Stop();
            _mockServer.Dispose();
        }
    }
}
