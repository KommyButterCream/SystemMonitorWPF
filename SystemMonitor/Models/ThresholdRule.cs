using System;

namespace SystemMonitor.Models
{
    internal class ThresholdRule
    {
        public double Warning { get; }
        public double Critical { get; }

        public ThresholdRule(double warning, double critical)
        {
            if (!double.IsFinite(warning) || !double.IsFinite(critical))
            {
                throw new ArgumentException("임계값은 유한한 숫자여야 합니다.");
            }

            if (warning >= critical)
            {
                throw new ArgumentException("주의 임계값은 위험 임계값보다 작아야 합니다.");
            }

            Warning = warning;
            Critical = critical;
        }
    }
}
