using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;

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

                return Constants.Gral.Unknown;
            }

            if (Util.Helper.HasNonASCIICharacters(machineName))
            {
                _log.Warn($"{method}: Machine name contains non-ASCII characters.");

                return Constants.Gral.Unknown;
            }

            return machineName;
        }
    }
}
