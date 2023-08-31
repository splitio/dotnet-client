using Splitio.CommonLibraries;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EventSource;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class PushManager : IPushManager
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly ISplitLogger _log;
        private readonly ISSEHandler _sseHandler;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly INotificationManagerKeeper _notificationManagerKeeper;
        private readonly ISplitTask _refreshTokenTask;
        private readonly IStatusManager _statusManager;

        private double _intervalToken;

        public PushManager(ISSEHandler sseHandler,
            IAuthApiClient authApiClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            INotificationManagerKeeper notificationManagerKeeper,
            ISplitTask periodicTask,
            IStatusManager statusManager)
        {
            _sseHandler = sseHandler;
            _authApiClient = authApiClient;
            _log = WrapperAdapter.Instance().GetLogger(typeof(PushManager));
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _notificationManagerKeeper = notificationManagerKeeper;
            _refreshTokenTask = periodicTask;
            _refreshTokenTask.SetFunction(RefreshTokenLogicAsync);
            _statusManager = statusManager;
        }

        #region Public Methods
        public async Task StartAsync()
        {
            if (_statusManager.IsDestroyed()) return;

            try
            {
                var response = await _authApiClient.AuthenticateAsync();

                _log.Debug($"Auth service response pushEnabled: {response.PushEnabled}.");

                if (!_statusManager.IsDestroyed() && response.PushEnabled.Value && _sseHandler.Start(response.Token, response.Channels))
                {
                    _intervalToken = response.Expiration.Value;
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.TokenRefresh, CalcularteNextTokenExpiration(_intervalToken)));
                    return;
                }

                if (!_statusManager.IsDestroyed() && response.Retry.Value)
                {
                    _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.RETRYABLE_ERROR);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"StartSse: {ex.Message}", ex);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                await _sseHandler.StopAsync();
                await _refreshTokenTask.StopAsync();
            }
            catch (Exception ex)
            {
                _log.Error($"StopSse: {ex.Message}");
            }
        }

        public async Task ScheduleConnectionResetAsync()
        {
            if (_refreshTokenTask.IsRunning())
            {
                _log.Debug("ScheduleConnectionReset task is running. Stoping and creating a new one.");
                await _refreshTokenTask.StopAsync();
            }

            var intervalTime = Convert.ToInt32(_intervalToken) * 1000;
            _log.Debug($"ScheduleNextTokenRefresh interval time : {intervalTime} milliseconds.");

            _refreshTokenTask.SetInterval(intervalTime);
            _refreshTokenTask.Start();

        }
        #endregion

        #region Private Methods
        private static long CalcularteNextTokenExpiration(double time)
        {
            return CurrentTimeHelper.CurrentTimeMillis() + Convert.ToInt64(time * 1000);
        }

        private async Task RefreshTokenLogicAsync()
        {
            try
            {
                _log.Debug("Starting Streaming Refresh Token...");
                await _sseHandler.StopAsync();
                await StartAsync();
            }
            catch (Exception ex)
            {
                _log.Debug($"Somenthing went wrong refreshing streaming token.", ex);
            }
        }
        #endregion
    }
}
