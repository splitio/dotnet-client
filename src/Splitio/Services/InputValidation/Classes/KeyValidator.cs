using Splitio.Commons.Dto;
using Splitio.Commons.Shared.Logger;
using Splitio.Commons.Shared.Utils;
using Splitio.Services.InputValidation.Interfaces;

namespace Splitio.Services.InputValidation.Classes
{
    public class KeyValidator : IKeyValidator
    {
        private const int KEY_MAX_LENGTH = 250;

        protected readonly ISplitLogger _log;

        public KeyValidator(ISplitLogger log = null)
        {
            _log = log ?? WrapperAdapter.Instance().GetLogger(typeof(KeyValidator));
        }

        public bool IsValid(Key key, Enums.API method)
        {
            var matchingKeyIsValid = Validate(key?.matchingKey, method, nameof(key.matchingKey));
            var bucketingKeyIsValid = Validate(key?.bucketingKey, method, nameof(key.bucketingKey));

            return matchingKeyIsValid && bucketingKeyIsValid;
        }

        private bool Validate(string key, Enums.API method, string type)
        {
            if (key == null)
            {
                _log.Error($"{method}: you passed a null {type}, the {type} must be a non-empty string.");
                return false;
            }

            if (key == string.Empty)
            {
                _log.Error($"{method}: you passed an empty string, {type} must be a non-empty string.");
                return false;
            }

            if (key.Length > KEY_MAX_LENGTH)
            {
                _log.Error($"{method}: {type} too long - must be 250 characters or less.");
                return false;
            }

            return true;
        }
    }
}
