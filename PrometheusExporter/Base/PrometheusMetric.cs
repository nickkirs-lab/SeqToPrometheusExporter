using Prometheus;
using System;
using System.Collections.Generic;

namespace PrometheusExporter.Base;

public class PrometheusMetric
{
    public string Name { get; }
    public string[] Labels { get; }
    public Counter Counter { get; }

    public PrometheusMetric(string name, string[] labels)
    {
        Name = name;
        Labels = labels ?? throw new ArgumentNullException(nameof(labels));
        Counter = Metrics.CreateCounter(name, $"Metric for {name}", labels);
    }

    public void Inc(Dictionary<string, string> labelValues)
    {
        if (labelValues == null)
            throw new ArgumentNullException(nameof(labelValues));

        var values = new string[Labels.Length];
        for (int i = 0; i < Labels.Length; i++)
        {
            if (!labelValues.TryGetValue(Labels[i], out var value))
            {
                throw new ArgumentException($"Missing value for label {Labels[i]}");
            }
            values[i] = value;
        }

        Counter.WithLabels(values).Inc();
    }
}