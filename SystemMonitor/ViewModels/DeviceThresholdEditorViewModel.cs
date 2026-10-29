using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemMonitor.Models;

namespace SystemMonitor.ViewModels
{
    internal class DeviceThresholdEditorViewModel : ViewModelBase
    {
        public string DisplayName { get; }
        public string UsageWarningText { get; set; }
        public string UsageCriticalText { get; set; }

        public string TemperatureWarningText { get; set; }
        public string TemperatureCriticalText { get; set; }

        public DeviceThresholdEditorViewModel(string displayName, ThresholdRule usageRule, ThresholdRule temperatureRule)
        {
            DisplayName = displayName;

            UsageWarningText = usageRule.Warning.ToString(CultureInfo.CurrentCulture);
            UsageCriticalText = usageRule.Critical.ToString(CultureInfo.CurrentCulture);

            TemperatureWarningText = temperatureRule.Warning.ToString(CultureInfo.CurrentCulture);
            TemperatureCriticalText = temperatureRule.Critical.ToString(CultureInfo.CurrentCulture);
        }

        public ThresholdRule CreateUsageRule()
        {
            return CreateRule(UsageWarningText, UsageCriticalText, "사용량", true);
        }

        public ThresholdRule CreateTemperatureRule()
        {
            return CreateRule(TemperatureWarningText, TemperatureCriticalText, "온도", false);
        }

        public ThresholdRule CreateRule(string warningText, string criticalText, string metricName, bool isUsage)
        {
            double warning = ParseNumber(
                warningText,
                $"{metricName} 주의");

            double critical = ParseNumber(
                criticalText,
                $"{metricName} 위험");

            if (isUsage &&
                (warning < 0.0 || warning > 100.0 ||
                 critical < 0.0 || critical > 100.0))
            {
                throw new ArgumentException(
                    $"{DisplayName} 사용량은 0~100 범위로 입력해주세요.");
            }

            if (warning >= critical)
            {
                throw new ArgumentException(
                    $"{DisplayName} {metricName} 주의 값은 " +
                    "위험 값보다 작아야 합니다.");
            }

            return new ThresholdRule(warning, critical);
        }

        private double ParseNumber(string text, string fieldName)
        {
            if(!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) || !double.IsFinite(value))
            {
                throw new ArgumentException($"{DisplayName} {fieldName}에 " + "올바른 숫자를 입력해주세요.");
            }

            return value;
        }
    }
}
