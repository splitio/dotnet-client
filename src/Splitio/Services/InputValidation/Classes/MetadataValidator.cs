using Splitio.Commons.Shared.Logger;
using Splitio.Commons.Shared.Utils;
using Splitio.Services.InputValidation.Interfaces;

namespace Splitio.Services.InputValidation.Classes
{
    public class SdkMetadataValidator : ISdkMetadataValidator
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SdkMetadataValidator));

        public string MachineNameValidation(string method, string machineName)
        {
            if (string.IsNullOrEmpty(machineName))
            {
                _log.Warn($"{method}: Machine name must be a non-empty string.");

                return Commons.Shared.Constants.Gral.Unknown;
            }

            if (Commons.Shared.Utils.Helper.HasNonASCIICharacters(machineName))
            {
                _log.Warn($"{method}: Machine name contains non-ASCII characters.");

                return Commons.Shared.Constants.Gral.Unknown;
            }

            return machineName;
        }
    }
}
