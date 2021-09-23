using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Splitio.Services.Client.Classes
{
    public class InMemoryReadinessGatesCache : IReadinessGatesCache
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(InMemoryReadinessGatesCache));

        private readonly CountdownEvent _sdkInternalReady;
        private readonly CountdownEvent _splitsAreReady;
        private readonly ConcurrentDictionary<string, CountdownEvent> _segmentsAreReady;
        private readonly Util.SplitStopwatch _splitsReadyTimer;

        public InMemoryReadinessGatesCache()
        {
            _sdkInternalReady = new CountdownEvent(1);
            _splitsAreReady = new CountdownEvent(1);
            _segmentsAreReady = new ConcurrentDictionary<string, CountdownEvent>();
            _splitsReadyTimer = new Util.SplitStopwatch();
            _splitsReadyTimer.Start();
        }

        #region Internal Ready
        public void SdkInternalReady()
        {
            try
            {
                if (_sdkInternalReady.IsSet) return;

                _sdkInternalReady.Signal();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        public void WaitUntilSdkInternalReady()
        {
            try
            {
                if (_sdkInternalReady.IsSet) return;

                _sdkInternalReady.Wait();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }
        #endregion

        public bool IsSDKReady(int milliseconds)
        {
            try
            {
                using (var clock = new Util.SplitStopwatch())
                {
                    clock.Start();

                    if (!AreSplitsReady(milliseconds))
                    {
                        return false;
                    }

                    milliseconds -= (int)clock.ElapsedMilliseconds;

                    return AreSegmentsReady(milliseconds);
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Something went wrong checking if sdk is ready.", ex);
                return false;
            }
        }

        public void SplitsAreReady()
        {
            try
            {
                if (_splitsAreReady.IsSet) return;

                _splitsAreReady.Signal();

                if (_splitsAreReady.IsSet)
                {
                    _splitsReadyTimer.Dispose();

                    if (_log.IsDebugEnabled && (int)_splitsReadyTimer.ElapsedMilliseconds != 0)
                    {
                        _log.Debug($"Splits are ready in {_splitsReadyTimer.ElapsedMilliseconds} milliseconds");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Something went wrong in SplitsAreReady.", ex);
            }
        }

        public void SegmentIsReady(string segmentName)
        {
            try
            {
                _segmentsAreReady.TryGetValue(segmentName, out CountdownEvent countDown);

                if ((countDown == null) || (countDown.IsSet))
                {
                    return;
                }

                countDown.Signal();

                if (countDown.IsSet && _log.IsDebugEnabled)
                {
                    _log.Debug($"{segmentName} segment is ready");
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Something went wrong in SegmentIsReady, Segment: {segmentName}.", ex);
            }
}

        public bool AreSplitsReady(int milliseconds)
        {
            return _splitsAreReady.Wait(milliseconds);
        }

        public bool RegisterSegment(string segmentName)
        {
            try
            {
                if (string.IsNullOrEmpty(segmentName) || AreSplitsReady(0))
                {
                    return false;
                }
            
                var success = _segmentsAreReady.TryAdd(segmentName, new CountdownEvent(1));

                if (_log.IsDebugEnabled)
                {
                    if (success)
                    {
                        _log.Debug("Registered segment: " + segmentName);
                    }
                    else
                    {
                        _log.Warn("Already registered segment: " + segmentName);
                    }                    
                }

                return true;
            }
            catch (Exception ex)
            {
                _log.Warn($"Something went wrong Registering {segmentName} Segment.", ex);
                return false;
            }
        }

        public bool AreSegmentsReady(int timeLeft)
        {
            try
            { 
                using (var segmentsClock = new Util.SplitStopwatch())
                {
                    segmentsClock.Start();

                    var values = _segmentsAreReady.Values;
                    foreach (var countdown in values)
                    {
                        using (var clock = new Util.SplitStopwatch())
                        {
                            clock.Start();

                            if (countdown == null)
                            {
                                return false;
                            }

                            if (timeLeft >= 0 && !countdown.Wait(timeLeft))
                            {
                                return false;
                            }

                            if (timeLeft < 0 && !countdown.IsSet)
                            {
                                return false;
                            }

                            timeLeft -= (int)clock.ElapsedMilliseconds;
                        }
                    }

                    if (_log.IsDebugEnabled && (int)segmentsClock.ElapsedMilliseconds != 0)
                    {
                        _log.Debug($"Segments are ready in {segmentsClock.ElapsedMilliseconds} milliseconds");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Something went wrong checking if segments are ready.", ex);
                return false;
            }
        }
    }
}
