﻿using Splitio.CommonLibraries;
using Splitio.Services.EventSource;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class PushManager : IPushManager
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly ISplitLogger _log;
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ISSEHandler _sseHandler;
        private readonly IBackOff _backOff;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;

        private CancellationTokenSource _cancellationTokenSourceRefreshToken;

        public PushManager(ISSEHandler sseHandler,
            IAuthApiClient authApiClient,
            IWrapperAdapter wrapperAdapter,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            IBackOff backOff)
        {
            _sseHandler = sseHandler;
            _authApiClient = authApiClient;
            _log = WrapperAdapter.Instance().GetLogger(typeof(PushManager));
            _wrapperAdapter = wrapperAdapter;
            _backOff = backOff;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
        }

        #region Public Methods
        public async Task<bool> StartSseAsync()
        {
            try
            {
                var response = await _authApiClient.AuthenticateAsync();

                _log.Debug($"Auth service response pushEnabled: {response.PushEnabled}.");

                if (response.PushEnabled.Value && _sseHandler.Start(response.Token, response.Channels))
                {                    
                    _backOff.Reset();
                    ScheduleNextTokenRefresh(response.Expiration.Value);
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.TokenRefresh, CalcularteNextTokenExpiration(response.Expiration.Value)));
                    return true;
                }
                
                StopSse();

                if (response.Retry.Value)
                {
                    var interval = _backOff.GetInterval();

                    _log.Info($"Streaming service temporarily unavailable, working in polling mode and retrying in {interval} seconds.");
                    ScheduleNextTokenRefresh(interval);
                }
                else
                {
                    ForceCancellationToken();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"StartSse: {ex.Message}");
            }

            return false;
        }

        public void StopSse()
        {
            try
            {
                _sseHandler.Stop();
            }
            catch (Exception ex)
            {
                _log.Error($"StopSse: {ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        private void ScheduleNextTokenRefresh(double time)
        {
            try
            {
                ForceCancellationToken();
                _cancellationTokenSourceRefreshToken = new CancellationTokenSource();

                var sleepTime = Convert.ToInt32(time) * 1000;
                _log.Debug($"ScheduleNextTokenRefresh sleep time : {sleepTime} miliseconds.");

                _wrapperAdapter
                    .TaskDelay(sleepTime)
                    .ContinueWith(async (t) =>
                    {
                        _log.Debug("Starting ScheduleNextTokenRefresh ...");
                        StopSse();
                        await StartSseAsync();
                    }, _cancellationTokenSourceRefreshToken.Token);
            }
            catch (Exception ex)
            {
                _log.Error($"ScheduleNextTokenRefresh: {ex.Message}");
            }
        }

        private void ForceCancellationToken()
        {
            if (_cancellationTokenSourceRefreshToken != null)
                _cancellationTokenSourceRefreshToken.Cancel();            
        }

        private long CalcularteNextTokenExpiration(double time)
        {
            return CurrentTimeHelper.CurrentTimeMillis() + Convert.ToInt64(time * 1000);
        }
        #endregion
    }
}
