using System.Collections.Generic;

namespace PrometheusExporter.Base;

public class MetricDefinition
{
    public string Type { get; set; } = "counter";
    public string Name { get; set; } = "";
    public string Filter { get; set; } = "";
    public List<string>? Tags { get; set; }
}