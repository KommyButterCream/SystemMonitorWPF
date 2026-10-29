using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemMonitor.Models;
using SystemMonitor.Services;

namespace SystemMonitor.ViewModels
{
    internal class DeviceCardViewModel : ViewModelBase
    {

        private readonly ThresholdEvaluator _thresholdEvaluator = new();

        private ThresholdRule _usageRule;
        private ThresholdRule _temperatureRule;

        public string DisplayName { get; }
        public double Usage { get; private set; }
        public ReadingStatus UsageStatus { get; private set; } = ReadingStatus.Pending;

        public double? Temperature { get; private set; }

        public ReadingStatus TemperatureStatus { get; private set; } = ReadingStatus.Pending;

        public string UsageText
        {
            get
            {
                if (UsageStatus == ReadingStatus.Available)
                {
                    return $"{Usage:F1}%";
                }

                return GetStatusText(UsageStatus);
            }
        }

        public string TemperatureText
        {
            get
            {
                if (TemperatureStatus == ReadingStatus.Available &&
                    Temperature is double temperature)
                {
                    return $"{temperature:F1} °C";
                }

                return GetStatusText(TemperatureStatus);
            }
        }

        public AlertLevel UsageAlertLevel
        {
            get
            {
                if (UsageStatus != ReadingStatus.Available)
                {
                    return AlertLevel.Unknown;
                }

                return _thresholdEvaluator.Evaluate(Usage, _usageRule);
            }
        }

        public AlertLevel TemperatureAlertLevel
        {
            get
            {
                if (TemperatureStatus != ReadingStatus.Available ||
                    Temperature is not double temperature)
                {
                    return AlertLevel.Unknown;
                }

                return _thresholdEvaluator.Evaluate(temperature, _temperatureRule);
            }
        }

        public string UsageAlertText
        {
            get
            {
                return GetAlertText(UsageAlertLevel);
            }
        }

        public string TemperatureAlertText
        {
            get
            {
                return GetAlertText(TemperatureAlertLevel);
            }
        }

        public string CapacityText { get; private set; } = string.Empty;

        public string TemperatureLabel { get; private set; } = "온도";
        public DeviceCardViewModel(
            string displayName,
            ThresholdRule usageRule,
            ThresholdRule temperatureRule)
        {
            DisplayName = displayName;

            _usageRule = usageRule ?? throw new ArgumentNullException(nameof(usageRule));
            _temperatureRule = temperatureRule ?? throw new ArgumentNullException(nameof(temperatureRule));
        }

        public void Update(
            double? usage,
            ReadingStatus usageStatus,
            double? temperature,
            ReadingStatus temperatureStatus,
            double? usedGiB = null,
            double? totalGiB = null,
            ReadingStatus capacityStatus = ReadingStatus.Unsupported,
            string temperatureLabel = "온도")
        {
            if (usageStatus == ReadingStatus.Available)
            {
                if (usage is double usageValue &&
                    double.IsFinite(usageValue) &&
                    usageValue >= 0.0 &&
                    usageValue <= 100.0)
                {
                    Usage = usageValue;
                    UsageStatus = ReadingStatus.Available;
                }
                else
                {
                    UsageStatus = ReadingStatus.Error;
                }
            }
            else
            {
                UsageStatus = usageStatus;
            }

            if (temperatureStatus == ReadingStatus.Available)
            {
                if (temperature is double temperatureValue &&
                    double.IsFinite(temperatureValue) &&
                    temperatureValue > 0.0)
                {
                    Temperature = temperatureValue;
                    TemperatureStatus = ReadingStatus.Available;
                }
                else
                {
                    TemperatureStatus = ReadingStatus.Error;
                }
            }
            else
            {
                TemperatureStatus = temperatureStatus;
            }

            TemperatureLabel = temperatureLabel;

            if (capacityStatus == ReadingStatus.Available)
            {
                if (usedGiB is double used &&
                    totalGiB is double total &&
                    double.IsFinite(used) &&
                    double.IsFinite(total) &&
                    used >= 0.0 &&
                    total > 0.0 &&
                    used <= total)
                {
                    CapacityText = $"{used:F2} / {total:F2} GiB";
                }
                else
                {
                    CapacityText = "용량 확인 불가";
                }
            }
            else
            {
                CapacityText = capacityStatus == ReadingStatus.Error ? "용량 측정 실패" : string.Empty;
            }

            NotifyCardProperties();
        }

        public void ApplyRules(ThresholdRule usageRule, ThresholdRule temperatureRule)
        {
            ArgumentNullException.ThrowIfNull(usageRule);
            ArgumentNullException.ThrowIfNull(temperatureRule);

            _usageRule = usageRule;
            _temperatureRule = temperatureRule;

            NotifyCardProperties();
        }

        private void NotifyCardProperties()
        {
            OnPropertyChanged(nameof(Usage));
            OnPropertyChanged(nameof(UsageStatus));
            OnPropertyChanged(nameof(UsageText));
            OnPropertyChanged(nameof(UsageAlertLevel));
            OnPropertyChanged(nameof(UsageAlertText));

            OnPropertyChanged(nameof(Temperature));
            OnPropertyChanged(nameof(TemperatureStatus));
            OnPropertyChanged(nameof(TemperatureText));
            OnPropertyChanged(nameof(TemperatureAlertLevel));
            OnPropertyChanged(nameof(TemperatureAlertText));

            OnPropertyChanged(nameof(CapacityText));
            OnPropertyChanged(nameof(TemperatureLabel));
        }

        private static string GetStatusText(ReadingStatus status)
        {
            return status switch
            {
                ReadingStatus.Pending => "측정 대기",
                ReadingStatus.Unsupported => "미지원",
                ReadingStatus.Error => "측정 실패",
                _ => "확인 불가"
            };
        }

        private static string GetAlertText(AlertLevel level)
        {
            return level switch
            {
                AlertLevel.Normal => "정상",
                AlertLevel.Warning => "주의",
                AlertLevel.Critical => "위험",
                _ => "확인 불가"
            };
        }
    }
}
