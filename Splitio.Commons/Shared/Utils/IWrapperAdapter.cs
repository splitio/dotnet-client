using Splitio.Commons.Dto;
using Splitio.Commons.Shared.Logger;
using System;
using System.Threading.Tasks;

namespace Splitio.Commons.Shared.Utils
{
    public interface IWrapperAdapter
    {
        SdkMetadata BuildSdkMetadata(string sdkMachineName, string sdkMachineIp, bool? ipAddressEnabled, AdapterType? adapterType, ISplitLogger log);
        Task<Task> WhenAnyAsync(params Task[] tasks);
        ISplitLogger GetLogger(string type);
        ISplitLogger GetLogger(Type type);

        void SetCustomerLogger(ISplitLogger splitLogger);
    }
}
