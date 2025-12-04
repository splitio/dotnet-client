using Splitio.Domain;
using Splitio.Services.Client.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface ISdkEventTask
    {
        Task OnExecute(SplitClient splitClient, EventMetadata eventMetadata);
    }
}
