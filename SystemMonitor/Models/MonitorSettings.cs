using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemMonitor.Models
{
    internal class MonitorSettings
    {
        public const int DefaultUpdateIntervalMilliseconds = 1000;
        public const int MinimumUpdateIntervalMilliseconds = 500;
        public const int MaximumUpdateIntervalMilliseconds = 5000;

        public ThresholdRule CpuUsageRule { get; }
        public ThresholdRule CpuTemperatureRule { get; }

        public ThresholdRule RamUsageRule { get; }
        public ThresholdRule RamTemperatureRule { get; }

        public ThresholdRule GpuUsageRule { get; }
        public ThresholdRule GpuTemperatureRule { get; }
        public int UpdateIntervalMilliseconds { get; }
        public static MonitorSettings CreateDefault()
        {
            return new MonitorSettings(new ThresholdRule(70.0, 90.0), new ThresholdRule(75.0, 85.0));
        }
        public MonitorSettings(
            ThresholdRule cpuUsageRule, ThresholdRule cpuTemperatureRule, 
            int updateIntervalMilliseconds = DefaultUpdateIntervalMilliseconds,
            ThresholdRule? ramUsageRule = null, ThresholdRule? ramTemperatureRule = null,
            ThresholdRule? gpuUsageRule = null, ThresholdRule? gpuTemperatureRule = null)
        {
            ArgumentNullException.ThrowIfNull(cpuUsageRule);
            ArgumentNullException.ThrowIfNull(cpuTemperatureRule);


            CpuUsageRule = cpuUsageRule;
            CpuTemperatureRule = cpuTemperatureRule;

            RamUsageRule = ramUsageRule ?? new ThresholdRule(70.0, 90.0);
            RamTemperatureRule = ramTemperatureRule ?? new ThresholdRule(50.0, 70.0);

            GpuUsageRule = gpuUsageRule ?? new ThresholdRule(70.0, 90.0);
            GpuTemperatureRule = gpuTemperatureRule ?? new ThresholdRule(75.0, 85.0);

            ValidateUsageRule(CpuUsageRule, "CPU");
            ValidateUsageRule(RamUsageRule, "RAM");
            ValidateUsageRule(GpuUsageRule, "GPU");

            if (updateIntervalMilliseconds < MinimumUpdateIntervalMilliseconds  || updateIntervalMilliseconds > MaximumUpdateIntervalMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(updateIntervalMilliseconds), "갱신 간격은 500~5000ms 범위여야 합니다.");
            }

          
            UpdateIntervalMilliseconds = updateIntervalMilliseconds;
        }

        private static void ValidateUsageRule(ThresholdRule rule, string deviceName)
        {
            if(rule.Warning < 0.0 || rule.Critical > 100.0)
            {
                throw new ArgumentException($"{deviceName} 사용량 임계값은 0~100 범위여야 합니다.");
            }
        }
    }
}
