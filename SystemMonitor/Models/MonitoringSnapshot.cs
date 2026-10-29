using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemMonitor.Models
{
    internal class MonitoringSnapshot
    {
        public DeviceReading Cpu { get; }
        public DeviceReading Ram { get; }

        public DeviceReading Gpu { get; }

        public MonitoringSnapshot(DeviceReading cpu, DeviceReading ram, DeviceReading gpu)
        {
            Cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
            Ram = ram ?? throw new ArgumentNullException(nameof(ram));
            Gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
        }
    }
}
