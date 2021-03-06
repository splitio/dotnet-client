﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ISplitFetcher
    {
        void Start();
        void Stop();
        Task<IList<string>> FetchSplits();
        void Clear();
    }
}
