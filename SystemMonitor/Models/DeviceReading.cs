using System;

namespace SystemMonitor.Models
{
    internal class DeviceReading
    {
        public double? UsagePercent { get; }

        public ReadingStatus Status { get; }

        public double? TemperatureCelsius { get; }

        public ReadingStatus TemperatureStatus { get; }

        public double? UsedGiB { get; }

        public double? TotalGiB { get; }

        public ReadingStatus CapacityStatus { get; }

        public string TemperatureLabel { get; }

        public DateTimeOffset MeasuredAt { get; }

        public DeviceReading(
            double? usagePercent,
            DateTimeOffset measuredAt,
            ReadingStatus status,
            double? temperatureCelsius = null,
            ReadingStatus temperatureStatus = ReadingStatus.Unsupported,
            double? usedGiB = null,
            double? totalGiB = null,
            ReadingStatus capacityStatus = ReadingStatus.Unsupported,
            string temperatureLabel = "온도")
        {
            UsagePercent = usagePercent;
            MeasuredAt = measuredAt;
            Status = status;

            TemperatureCelsius = temperatureCelsius;
            TemperatureStatus = temperatureStatus;

            UsedGiB = usedGiB;
            TotalGiB = totalGiB;
            CapacityStatus = capacityStatus;

            TemperatureLabel = temperatureLabel;
        }
    }
}
