﻿using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Splitio.Services.Client.Classes
{
    public class InMemoryReadinessGatesCache : IReadinessGatesCache
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(InMemoryReadinessGatesCache));

        private readonly CountdownEvent _sdkInternalReady;
        private readonly CountdownEvent _splitsAreReady;
        private readonly Dictionary<string, CountdownEvent> _segmentsAreReady;
        private readonly Stopwatch _splitsReadyTimer;
        private readonly Stopwatch _segmentsReadyTimer;

        public InMemoryReadinessGatesCache()
        {
            _sdkInternalReady = new CountdownEvent(1);
            _splitsAreReady = new CountdownEvent(1);
            _segmentsAreReady = new Dictionary<string, CountdownEvent>();
            _splitsReadyTimer = new Stopwatch();
            _segmentsReadyTimer = new Stopwatch();

            _segmentsReadyTimer.Start();
            _splitsReadyTimer.Start();
        }

        #region Internal Ready
        public void SdkInternalReady()
        {
            if (_sdkInternalReady.IsSet) return;

            _sdkInternalReady.Signal();
        }

        public void WaitUntilSdkInternalReady()
        {
            if (_sdkInternalReady.IsSet) return;

            _sdkInternalReady.Wait();
        }
        #endregion

        public bool IsSDKReady(int milliseconds)
        {
            var clock = new Stopwatch();
            clock.Start();

            if (!AreSplitsReady(milliseconds))
            {
                return false;
            }

            var timeLeft = milliseconds - (int)clock.ElapsedMilliseconds;

            var ready = AreSegmentsReady(timeLeft);

            if (ready) SdkInternalReady();

            return ready;
        }       

        public void SplitsAreReady()
        {
            if (!_splitsAreReady.IsSet)
            {
                _splitsAreReady.Signal();

                if (_splitsAreReady.IsSet)
                {
                    _splitsReadyTimer.Stop();

                    if (_log.IsDebugEnabled)
                    {
                        _log.Debug($"Splits are ready in {_splitsReadyTimer.ElapsedMilliseconds} milliseconds");
                    }
                }
            }
        }

        public void SegmentIsReady(string segmentName)
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

        public bool AreSplitsReady(int milliseconds)
        {
            return _splitsAreReady.Wait(milliseconds);
        }

        public bool RegisterSegment(string segmentName)
        {
            if (string.IsNullOrEmpty(segmentName) || AreSplitsReady(0))
            {
                return false;
            }

            try
            {
                _segmentsAreReady.Add(segmentName, new CountdownEvent(1));

                if (_log.IsDebugEnabled)
                {
                    _log.Debug("Registered segment: " + segmentName);
                }
            }
            catch (ArgumentException e)
            {
                _log.Warn("Already registered segment: " + segmentName, e);
            }

            return true;
        }

        public bool AreSegmentsReady(int milliseconds)
        {
            var clock = new Stopwatch();
            clock.Start();
            int timeLeft = milliseconds;

            foreach (var entry in _segmentsAreReady)
            {
                var segmentName = entry.Key;
                var countdown = entry.Value;

                if (timeLeft >= 0 && !countdown.Wait(timeLeft))
                {
                    return false;
                }

                if (timeLeft < 0 && !countdown.Wait(0))
                {
                    return false;
                }

                timeLeft = timeLeft - (int)clock.ElapsedMilliseconds;
            }

            _segmentsReadyTimer.Stop();

            if (_log.IsDebugEnabled)
            {
                _log.Debug($"Segments are ready in {_segmentsReadyTimer.ElapsedMilliseconds} milliseconds");
            }

            return true;
        }
    }
}
