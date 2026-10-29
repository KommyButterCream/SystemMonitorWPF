using System;
using SystemMonitor.Models;

namespace SystemMonitor.Services
{
    internal interface IMonitoringService : IDisposable
    {
        MonitoringSnapshot ReadAll();
    }
}
