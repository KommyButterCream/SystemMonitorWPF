using System;
using SystemMonitor.Models;

namespace SystemMonitor.Services
{
    internal class ThresholdEvaluator
    {
        public AlertLevel Evaluate(double value, ThresholdRule rule)
        {
            if (!double.IsFinite(value))
            {
                return AlertLevel.Unknown;
            }

            if(value >= rule.Critical)
            {
                return AlertLevel.Critical;
            }

            if(value >= rule.Warning)
            {
                return AlertLevel.Warning;
            }

            return AlertLevel.Normal;
        }
    }
}
