using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IWrapperAdapter
    {
        SdkMetadata BuildSdkMetadata(ConfigurationOptions config, ISplitLogger log);
        Task<Task> WhenAnyAsync(params Task[] tasks);
        ISplitLogger GetLogger(string type);
        ISplitLogger GetLogger(Type type);

        void SetCustomerLogger(ISplitLogger splitLogger);
    }
}
