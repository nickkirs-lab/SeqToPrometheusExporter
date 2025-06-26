using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Seq.Syntax.Expressions;
using Seq.Syntax.Templates;
using Serilog.Events;

namespace Seq.App.Prometheus;

public abstract class Metric
{
    private readonly CompiledExpression _filter;

    private readonly List<MetricLabel>? _labels;

    protected Metric(Meter meter, MetricDescriptor descriptor)
    {
        _filter = SerilogExpression.Compile(descriptor.Filter);

        if (descriptor.Labels != null)
        {
            _labels = new List<MetricLabel>();
            
            foreach (var label in descriptor.Labels)
                _labels.Add(new MetricLabel(label));
        }
    }

    protected abstract void Observe(LogEvent evt);

    public void Accept(LogEvent evt)
    {
        var result = _filter(evt);

        if (result is ScalarValue s && (bool)(s.Value ?? false))
            Observe(evt);
    }

    protected TagList GetLabels(LogEvent evt)
    {
        var result = new TagList();

        if (_labels == null)
            return result;

        foreach (var label in _labels)
        {
            if (label.TryBuild(evt, out var labelValue)) 
                result.Add(labelValue.Value);
        }

        return result;
    }

    private sealed class MetricLabel(MetricLabelDescriptor descriptor)
    {
        private readonly string _name = descriptor.Name;
        private readonly ExpressionTemplate _value = new ExpressionTemplate(descriptor.Value);

        public bool TryBuild(LogEvent evt, [NotNullWhen(true)]out KeyValuePair<string, object?>? label)
        {
            using var writer = ReusableStringWriter.GetOrCreate();
        
            _value.Format(evt, writer);
        
            var labelValue = writer.ToString();

            if (string.IsNullOrEmpty(labelValue))
            {
                label = null;
                return false;
            }

            label = new KeyValuePair<string, object?>(_name, labelValue);
            
            return true;
        }
    }
}

public sealed class CounterMetric(Meter meter, MetricDescriptor descriptor) : Metric(meter, descriptor)
{
    private readonly Counter<long> _counter = meter.CreateCounter<long>(descriptor.Name, description: descriptor.Help);

    protected override void Observe(LogEvent evt)
    {
        _counter.Add(1, GetLabels(evt));
    }
}