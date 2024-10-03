using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Services.InputValidation.Classes
{
    public class ApiKeyValidator : IApiKeyValidator
    {
        protected readonly ISplitLogger _log;

        public ApiKeyValidator(ISplitLogger log = null)
        {
            _log = log ?? WrapperAdapter.Instance().GetLogger(typeof(ApiKeyValidator));
        }

        public void Validate(string apiKey)
        {
            if (apiKey == string.Empty)
            {
                _log.Error("factory instantiation: you passed and empty api_key, api_key must be a non-empty string.");
                throw new ArgumentException("API Key must be set to initialize Split SDK.");
            }

            if (apiKey == null)
            {
                _log.Error("factory instantiation: you passed a null api_key, api_key must be a non-empty string.");
                throw new ArgumentNullException("API Key must be set to initialize Split SDK.");
            }
        }
    }
}
