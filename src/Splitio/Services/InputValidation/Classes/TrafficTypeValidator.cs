using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System.Linq;

namespace Splitio.Services.InputValidation.Classes
{
    public class TrafficTypeValidator : ITrafficTypeValidator
    {
        private readonly ISplitLogger _log;
        private readonly ISplitCache _splitCache;
        private readonly IBlockUntilReadyService _blockUntilReadyService;

        public TrafficTypeValidator(ISplitCache splitCache, IBlockUntilReadyService blockUntilReadyService, ISplitLogger log = null)
        {
            _splitCache = splitCache;
            _blockUntilReadyService = blockUntilReadyService;
            _log = log ?? WrapperAdapter.Instance().GetLogger(typeof(TrafficTypeValidator));
        }

        public ValidatorResult IsValid(string trafficType, string method)
        {
            if (!_blockUntilReadyService.IsSdkReady())
            {
                return new ValidatorResult { Success = true, Value = trafficType };
            }

            if (trafficType == null)
            {
                _log.Error($"{method}: you passed a null traffic_type, traffic_type must be a non-empty string");
                return new ValidatorResult { Success = false };
            }

            if (trafficType == string.Empty)
            {
                _log.Error($"{method}: you passed an empty traffic_type, traffic_type must be a non-empty string");
                return new ValidatorResult { Success = false };
            }

            if (trafficType.Any(ch => char.IsUpper(ch)))
            {
                _log.Warn($"{method}: {trafficType} should be all lowercase - converting string to lowercase");

                trafficType = trafficType.ToLower();
            }

            if (!_splitCache.TrafficTypeExists(trafficType))
            {
                _log.Warn($"Track: Traffic Type {trafficType} does not have any corresponding Splits in this environment, make sure you’re tracking your events to a valid traffic type defined in the Split console.");
            }

            return new ValidatorResult { Success = true, Value = trafficType };
        }
    }
}
