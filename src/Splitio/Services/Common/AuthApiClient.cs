using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly ISplitLogger _log;
        private readonly ISplitioHttpClient _splitioHttpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly string _url;

        public AuthApiClient(string url,
            string apiKey,
            ISplitioHttpClient splitioHttpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ISplitLogger log = null)
        {
            _url = url;
            _splitioHttpClient = splitioHttpClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _log = log ?? WrapperAdapter.GetLogger(typeof(AuthApiClient));            
        }

        #region Public Methods
        public async Task<AuthenticationResponse> AuthenticateAsync()
        {
            var clock = new Stopwatch();
            clock.Start();

            try
            {
                var response = await _splitioHttpClient.GetAsync(_url);

                if (response.statusCode == HttpStatusCode.OK)
                {
                    _log.Debug($"Success connection to: {_url}");

                    _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.TokenSync, Util.Metrics.Bucket(clock.ElapsedMilliseconds));
                    _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.TokenSync, CurrentTimeHelper.CurrentTimeMillis());

                    return GetSuccessResponse(response.content);
                }
                else if (response.statusCode >= HttpStatusCode.BadRequest && response.statusCode < HttpStatusCode.InternalServerError)
                {
                    _log.Debug($"Problem to connect to : {_url}. Response status: {response.statusCode}");

                    _telemetryRuntimeProducer.RecordAuthRejections();
                    return new AuthenticationResponse { PushEnabled = false, Retry = false };
                }

                _telemetryRuntimeProducer.RecordSyncError(ResourceEnum.TokenSync, (int)response.statusCode);
                return new AuthenticationResponse { PushEnabled = false, Retry = true };
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);

                return new AuthenticationResponse { PushEnabled = false, Retry = false };
            }
        }
        #endregion

        #region Private Methods
        private AuthenticationResponse GetSuccessResponse(string content)
        {
            var authResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(content);

            if (authResponse.PushEnabled == true)
            {
                var tokenDecoded = DecodeJwt(authResponse.Token);
                var token = JsonConvert.DeserializeObject<Jwt>(tokenDecoded);

                authResponse.Channels = GetChannels(token);
                authResponse.Expiration = GetExpirationSeconds(token);

                _telemetryRuntimeProducer.RecordTokenRefreshes();
            }

            authResponse.Retry = false;

            return authResponse;
        }

        private string GetChannels(Jwt token)
        {
            var capability = (JObject)JsonConvert.DeserializeObject(token.Capability);
            var channelsList = capability
                .Children()
                .Select(c => c.First.Path)
                .ToList();

            var channels = AddPrefixControlChannels(string.Join(",", channelsList));

            return channels;
        }

        private string AddPrefixControlChannels(string channels)
        {
            channels = channels
                .Replace(Constants.Push.ControlPri, $"{Constants.Push.OccupancyPrefix}{Constants.Push.ControlPri}")
                .Replace(Constants.Push.ControlSec, $"{Constants.Push.OccupancyPrefix}{Constants.Push.ControlSec}");

            return channels;
        }

        private double GetExpirationSeconds(Jwt token)
        {
            return token.Expiration - token.IssuedAt - Constants.Push.SecondsBeforeExpiration;
        }

        private string DecodeJwt(string token)
        {
            var split_string = token.Split('.');
            var base64EncodedBody = split_string[1];

            int mod4 = base64EncodedBody.Length % 4;
            if (mod4 > 0)
            {
                base64EncodedBody += new string('=', 4 - mod4);
            }

            var base64EncodedBytes = Convert.FromBase64String(base64EncodedBody);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
#endregion
    }
}
