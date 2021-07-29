using System;

namespace Splitio.Services.Common
{
    public class BackOff : IBackOff
    {
        private readonly int _backOffBase;
        private readonly double _maxAllowed;
        private int _attempt;

        public BackOff(int backOffBase, int attempt = 0, double maxAllowed = 1800)
        {
            _backOffBase = backOffBase;
            _maxAllowed = maxAllowed;
            _attempt = attempt;
        }

        public int GetAttempt()
        {
            return _attempt;
        }

        public double GetInterval(bool inMiliseconds = false)
        {
            var interval = 0d;

            if (_attempt > 0)
            {
                interval = _backOffBase * Math.Pow(2, _attempt);
            }

            _attempt++;

            var result = interval >= _maxAllowed ? _maxAllowed : interval;

            return inMiliseconds ? result * 1000 : result;
        }

        public void Reset()
        {
            _attempt = 0;
        }
    }
}
