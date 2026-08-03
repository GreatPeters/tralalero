using System;
using System.Collections.Generic;

namespace IndianOceanAssets.ShooterSurvival.Analytics
{
    public enum AnalyticsParameterKind
    {
        String,
        Long,
        Double
    }

    public sealed class AnalyticsParameterValue
    {
        public AnalyticsParameterValue(string name, string value)
        {
            Name = RequireName(name);
            Kind = AnalyticsParameterKind.String;
            StringValue = value ?? string.Empty;
        }

        public AnalyticsParameterValue(string name, long value)
        {
            Name = RequireName(name);
            Kind = AnalyticsParameterKind.Long;
            LongValue = value;
        }

        public AnalyticsParameterValue(string name, double value)
        {
            Name = RequireName(name);
            Kind = AnalyticsParameterKind.Double;
            DoubleValue = value;
        }

        public string Name { get; }
        public AnalyticsParameterKind Kind { get; }
        public string StringValue { get; } = string.Empty;
        public long LongValue { get; }
        public double DoubleValue { get; }

        private static string RequireName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Analytics parameter name is required.", nameof(name));

            return name;
        }
    }

    public sealed class AnalyticsEventData
    {
        public AnalyticsEventData(string name, params AnalyticsParameterValue[] parameters)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Analytics event name is required.", nameof(name));

            Name = name;

            AnalyticsParameterValue[] copy =
                parameters == null
                    ? Array.Empty<AnalyticsParameterValue>()
                    : (AnalyticsParameterValue[])parameters.Clone();

            for (int i = 0; i < copy.Length; i++)
            {
                if (copy[i] == null)
                    throw new ArgumentException("Analytics parameters cannot contain null.", nameof(parameters));
            }

            Parameters = Array.AsReadOnly(copy);
        }

        public string Name { get; }
        public IReadOnlyList<AnalyticsParameterValue> Parameters { get; }
    }

    public interface IAnalyticsSink
    {
        bool IsReady { get; }
        void LogEvent(AnalyticsEventData eventData);
        void Flush();
    }
}
