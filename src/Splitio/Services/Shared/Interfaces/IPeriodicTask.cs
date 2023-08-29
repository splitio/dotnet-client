﻿using System.Threading.Tasks;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IPeriodicTask
    {
        void Start();
        void Stop();
    }
}
