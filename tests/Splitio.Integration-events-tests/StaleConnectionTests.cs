using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Integration_events_tests
{
    /// <summary>
    /// Tests that reproduce the "response ended prematurely" (HttpIOException / IOException)
    /// error caused by server-side connection closures on keep-alive connections.
    ///
    /// Root cause: When using HTTP keep-alive, if the server closes an idle connection
    /// while the client's connection pool still considers it active, the next POST request
    /// sent on that stale connection will fail with "The response ended prematurely".
    ///
    /// .NET does NOT auto-retry POST requests (non-idempotent) on connection failures,
    /// unlike GET requests which are retried up to 3 times automatically.
    ///
    /// See: https://github.com/dotnet/runtime/issues/125489
    ///      https://github.com/dotnet/runtime/issues/122583
    /// </summary>
    [TestClass, TestCategory("Integration")]
    public class StaleConnectionTests
    {
#if NET5_0_OR_GREATER
        [TestMethod]
        public async Task PostAsync_WhenServerClosesWithoutResponding_ReturnsFailedResult()
        {
            // Arrange: A server that reads the POST request but closes the connection
            // without sending a response. This simulates what happens when a load balancer
            // or server closes a keep-alive connection while the client is sending a request.
            using var server = new PrematureCloseServer(failFirstNRequests: int.MaxValue);
            _ = Task.Run(() => server.AcceptConnectionsAsync());

            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 5000,
                HttpReadTimeout = 5000
            };
            using var httpClient = new SplitioHttpClient("test-api-key", config, new Dictionary<string, string>());

            // Act
            var result = await httpClient.PostAsync(
                $"{server.Url}/api/testImpressions/bulk",
                "[{\"f\":\"feature\",\"i\":[{\"k\":\"key\",\"t\":\"on\",\"m\":1}]}]");

            // Assert: PostAsync catches the exception and returns a failed result
            Assert.IsFalse(result.IsSuccessStatusCode,
                "PostAsync should return failed result when server closes connection prematurely");
            Assert.AreEqual(0, (int)result.StatusCode,
                "StatusCode should be 0 (default) since no HTTP response was received");
        }

        [TestMethod]
        public async Task PostAsync_KeepAlive_SecondRequestFailsOnStaleConnection()
        {
            // Arrange: Server responds to the first POST, then closes the connection
            // without responding to the second POST. This is the exact scenario the
            // customer (Compare the Market) experiences: the keep-alive connection
            // goes stale between impression flush intervals.
            using var server = new PrematureCloseServer(failFirstNRequests: 0, respondOnSameConnection: 1);
            _ = Task.Run(() => server.AcceptConnectionsAsync());

            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 5000,
                HttpReadTimeout = 5000
            };
            using var httpClient = new SplitioHttpClient("test-api-key", config, new Dictionary<string, string>());

            var url = $"{server.Url}/api/testImpressions/bulk";
            var payload = "[{\"f\":\"feature\",\"i\":[{\"k\":\"key\",\"t\":\"on\",\"m\":1}]}]";

            // Act: First POST succeeds
            var result1 = await httpClient.PostAsync(url, payload);
            Assert.IsTrue(result1.IsSuccessStatusCode, "First POST should succeed");

            // Act: Second POST should fail because the server closes the keep-alive connection
            var result2 = await httpClient.PostAsync(url, payload);

            // Assert: Second request fails due to stale connection
            Assert.IsFalse(result2.IsSuccessStatusCode,
                "Second POST should fail when server closes the keep-alive connection");
        }

        [TestMethod]
        public async Task SendBulkImpressionsAsync_WhenFirstAttemptFails_RetrySucceedsOnNewConnection()
        {
            // Arrange: Server fails the first request (closes without responding),
            // but succeeds on the second (retry opens a new connection).
            // This tests that the retry loop in BuildJsonAndPostAsync recovers
            // from transient connection failures.
            using var server = new PrematureCloseServer(failFirstNRequests: 1);
            _ = Task.Run(() => server.AcceptConnectionsAsync());

            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 5000,
                HttpReadTimeout = 5000
            };
            using var httpClient = new SplitioHttpClient("test-api-key", config, new Dictionary<string, string>());

            var telemetry = new Mock<ITelemetryRuntimeProducer>();
            var wrapperAdapter = WrapperAdapter.Instance();

            var impressions = new List<KeyImpression>
            {
                new KeyImpression("key-1", "feature-1", "on", 1, 1, "label", "bucket", false)
            };

            var apiClient = new ImpressionsSdkApiClient(
                httpClient, telemetry.Object, server.Url, wrapperAdapter, 5000);

            // Act: SendBulkImpressionsAsync should retry and succeed
            await apiClient.SendBulkImpressionsAsync(impressions);

            // Assert: Server received 2 requests (1 failed + 1 retry succeeded)
            // Allow a brief moment for the server to process
            await Task.Delay(500);
            Assert.AreEqual(2, server.TotalRequestsReceived,
                "Server should receive 2 requests: 1 failed attempt + 1 successful retry");
            Assert.AreEqual(1, server.SuccessfulResponses,
                "Server should have sent 1 successful response (on the retry)");
        }
#endif

        #region Helper: PrematureCloseServer

        /// <summary>
        /// A minimal TCP server that simulates premature connection closures.
        ///
        /// It accepts HTTP POST requests and can be configured to:
        /// - Close the connection without responding (simulating server-side keep-alive timeout)
        /// - Respond normally to some requests before closing
        ///
        /// This is used instead of WireMock because WireMock doesn't support
        /// simulating mid-connection closures after reading the request body.
        /// </summary>
        private class PrematureCloseServer : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly int _failFirstNRequests;
            private readonly int _respondOnSameConnection;
            private int _globalRequestCount;
            private int _totalRequestsReceived;
            private int _successfulResponses;

            /// <param name="failFirstNRequests">
            /// Number of initial requests (globally, across all connections) to fail
            /// by closing the connection without responding. Subsequent requests get 200 OK.
            /// </param>
            /// <param name="respondOnSameConnection">
            /// Number of requests to respond to on the same keep-alive connection before
            /// closing it. Use this to simulate the keep-alive stale connection scenario.
            /// When set to > 0, overrides failFirstNRequests for per-connection behavior.
            /// Default: 0 (use failFirstNRequests for global behavior).
            /// </param>
            public PrematureCloseServer(int failFirstNRequests, int respondOnSameConnection = 0)
            {
                _failFirstNRequests = failFirstNRequests;
                _respondOnSameConnection = respondOnSameConnection;
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
            }

            public string Url => $"http://localhost:{((IPEndPoint)_listener.LocalEndpoint).Port}";
            public int TotalRequestsReceived => _totalRequestsReceived;
            public int SuccessfulResponses => _successfulResponses;

            public async Task AcceptConnectionsAsync()
            {
                try
                {
                    while (true)
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        _ = HandleClientAsync(client);
                    }
                }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }
            }

            private async Task HandleClientAsync(TcpClient client)
            {
                int perConnectionCount = 0;

                using (client)
                {
                    var stream = client.GetStream();

                    while (true)
                    {
                        // Read HTTP request line
                        var requestLine = await ReadLineAsync(stream);
                        if (string.IsNullOrEmpty(requestLine)) break;

                        // Read headers
                        int contentLength = 0;
                        string line;
                        while ((line = await ReadLineAsync(stream)) != "")
                        {
                            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                                contentLength = int.Parse(line.Substring("Content-Length:".Length).Trim());
                        }

                        // Read body
                        if (contentLength > 0)
                        {
                            var body = new byte[contentLength];
                            int totalRead = 0;
                            while (totalRead < contentLength)
                            {
                                var read = await stream.ReadAsync(body, totalRead, contentLength - totalRead);
                                if (read == 0) break;
                                totalRead += read;
                            }
                        }

                        Interlocked.Increment(ref _totalRequestsReceived);
                        perConnectionCount++;

                        bool shouldRespond;
                        if (_respondOnSameConnection > 0)
                        {
                            // Per-connection mode: respond to first N requests on this connection
                            shouldRespond = perConnectionCount <= _respondOnSameConnection;
                        }
                        else
                        {
                            // Global mode: fail first N requests across all connections
                            var globalCount = Interlocked.Increment(ref _globalRequestCount);
                            shouldRespond = globalCount > _failFirstNRequests;
                        }

                        if (shouldRespond)
                        {
                            var response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: keep-alive\r\n\r\nok";
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                            await stream.FlushAsync();
                            Interlocked.Increment(ref _successfulResponses);
                        }
                        else
                        {
                            // Close without responding — simulates premature connection close
                            break;
                        }
                    }
                }
            }

            private static async Task<string> ReadLineAsync(NetworkStream stream)
            {
                var sb = new StringBuilder();
                var buffer = new byte[1];
                while (true)
                {
                    int read;
                    try { read = await stream.ReadAsync(buffer, 0, 1); }
                    catch { return sb.ToString(); }
                    if (read == 0) return sb.ToString();
                    var c = (char)buffer[0];
                    if (c == '\n') return sb.ToString().TrimEnd('\r');
                    sb.Append(c);
                }
            }

            public void Dispose()
            {
                _listener.Stop();
            }
        }

        #endregion
    }
}
