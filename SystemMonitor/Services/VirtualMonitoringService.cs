using HidSharp.Reports;
using System;
using SystemMonitor.Models;

namespace SystemMonitor.Services
{
    internal class VirtualMonitoringService : IMonitoringService
    {
        private readonly double[] _usageValues =
        {
            30.0,
            50.0,
            70.0,
            90.0,
            100.0,
            0.0
        };

        private int _nextIndex;

        public MonitoringSnapshot ReadAll()
        {
            double usage = _usageValues[_nextIndex];

            _nextIndex++;

            if (_nextIndex >= _usageValues.Length)
            {
                _nextIndex = 0;
            }

            DateTimeOffset now = DateTimeOffset.Now;

            var cpu = new DeviceReading(
                usage,
                now,
                ReadingStatus.Available,
                55.0,
                ReadingStatus.Available);

            var ram = new DeviceReading(
                45.0,
                now,
                ReadingStatus.Available,
                34.0,
                ReadingStatus.Available,
                usedGiB: 14.4,
                totalGiB: 32.0,
                capacityStatus: ReadingStatus.Available,
                temperatureLabel: "온도 · 가상 모듈 최대 (2/2)");

            var gpu = new DeviceReading(
                35.0,
                now,
                ReadingStatus.Available,
                48.0,
                ReadingStatus.Available,
                usedGiB: 2.0,
                totalGiB: 8.0,
                capacityStatus: ReadingStatus.Available,
                temperatureLabel: "온도 · 가상 GPU Core");

            return new MonitoringSnapshot(cpu, ram, gpu);
        }

        public void Dispose()
        {

        }
    }
}
