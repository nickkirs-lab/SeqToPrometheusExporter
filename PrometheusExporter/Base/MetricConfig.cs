using System.Collections.Generic;

namespace PrometheusExporter.Base;

public class MetricConfig
{
    public string? Job { get; set; }
    public List<MetricDefinition> Metrics { get; set; } = new();
}