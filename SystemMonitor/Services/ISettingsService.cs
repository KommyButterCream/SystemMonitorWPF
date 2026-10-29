using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemMonitor.Models;

namespace SystemMonitor.Services
{
    internal interface ISettingsService
    {
        Task<MonitorSettings> LoadAsync();

        Task SaveAsync(MonitorSettings settings);
    }
}
