using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Splitio.Constants;
using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(AuthApiClient));

        private readonly ISplitioHttpClient _splitioHttpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly string _url;

        public AuthApiClient(string url,
            ISplitioHttpClient splitioHttpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer)
        {
            _url = $"{url}?s={ApiVersions.LatestFlagsSpec}";
            _splitioHttpClient = splitioHttpClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
        }

        #region Public Methods
        public async Task<AuthenticationResponse> AuthenticateAsync()
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var response = await _splitioHttpClient.GetAsync(_url).ConfigureAwait(false);

                    Util.Helper.RecordTelemetrySync(nameof(AuthenticateAsync), response, ResourceEnum.TokenSync, clock, _telemetryRuntimeProducer, _log);

                    if (response.IsSuccessStatusCode)
                    {
                        _log.Debug($"Success connection to: {_url}");
                        return GetSuccessResponse(response.Content);
                    }
                    else if (response.StatusCode >= HttpStatusCode.BadRequest && response.StatusCode < HttpStatusCode.InternalServerError)
                    {
                        _log.Debug($"Problem to connect to : {_url}. Response status: {response.StatusCode}");

                        _telemetryRuntimeProducer.RecordAuthRejections();
                        return new AuthenticationResponse { PushEnabled = false, Retry = false };
                    }

                    return new AuthenticationResponse { PushEnabled = false, Retry = true };
                }
                catch (Exception ex)
                {
                    _log.Error("Somenthing went wrong getting Auth authentication", ex);

                    return new AuthenticationResponse { PushEnabled = false, Retry = false };
                }
            }
        }
        #endregion

        #region Private Methods
        private AuthenticationResponse GetSuccessResponse(string content)
        {
            var authResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(content);
            authResponse.Retry = authResponse.PushEnabled;

            if (authResponse.PushEnabled == false) 
                return authResponse;

            var tokenDecoded = DecodeJwt(authResponse.Token);
            var token = JsonConvert.DeserializeObject<Jwt>(tokenDecoded);

            authResponse.Channels = GetChannels(token);
            authResponse.Expiration = GetExpirationSeconds(token);

            _telemetryRuntimeProducer.RecordTokenRefreshes();

            return authResponse;
        }

        private static string GetChannels(Jwt token)
        {
            var capability = (JObject)JsonConvert.DeserializeObject(token.Capability);
            var channelsList = capability
                .Children()
                .Select(c => c.First.Path)
                .ToList();

            var channels = AddPrefixControlChannels(string.Join(",", channelsList));

            return channels;
        }

        private static string AddPrefixControlChannels(string channels)
        {
            channels = channels
                .Replace(Constants.Push.ControlPri, $"{Constants.Push.OccupancyPrefix}{Constants.Push.ControlPri}")
                .Replace(Constants.Push.ControlSec, $"{Constants.Push.OccupancyPrefix}{Constants.Push.ControlSec}");

            return channels;
        }

        private static double GetExpirationSeconds(Jwt token)
        {
            return token.Expiration - token.IssuedAt - Constants.Push.SecondsBeforeExpiration;
        }

        private static string DecodeJwt(string token)
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
