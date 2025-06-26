using System.Diagnostics;

namespace Seq.App.Prometheus;

[DebuggerDisplay("{Name} ({Type})")]
public sealed class MetricDescriptor
{
    public required string Name { get; set; }
    
    public string? Help { get; set; }
    public required string Type { get; set; }
    
    public required string Filter { get; set; }
    
    public List<MetricLabelDescriptor>? Labels { get; set; }
}

[DebuggerDisplay("{Name} = {Value}")]
public sealed class MetricLabelDescriptor
{
    public required string Name { get; set; }
    
    public required string Value { get; set; }
}