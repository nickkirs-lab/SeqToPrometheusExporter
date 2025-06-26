using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Seq.App.Prometheus.Tests.Support;
using Seq.Apps;
using Seq.Apps.Testing.Hosting;
using Serilog.Events;

namespace Seq.App.Prometheus.Tests;

public class AppTests
{
    [Fact]
    public void ItShouldIncrementCounter()
    {
        var meter = new Meter("");
        var metricServerFactory = new TestableMetricServerFactory();

        var metricCollector = new MetricCollector<long>(meter, "example_metric");

        var app = new PrometheusApp(meter, metricServerFactory)
        {
            Configuration = """
                            - name: "example_metric"
                              type: "counter"
                              help: "Number of events"
                              filter: "@Level = 'Information'"
                              labels:
                                - name: "name"
                                  value: "{Name}"
                                - name: "env"
                                  value: "{Environment}"
                            """
        };

        app.Attach(new TestAppHost());

        var evt = Some.InformationEvent("Hello, {Name}!", "some_name");
        
        app.On(new Event<LogEvent>("event-1", 123, DateTime.UtcNow, evt));

        var measurements = metricCollector.GetMeasurementSnapshot();
        
        Assert.Single(measurements);
        
        var measurement = measurements[0];
        
        Assert.Equal(1, measurement.Value);
        Assert.Contains(measurement.Tags, pair => pair is { Key: "name", Value: "some_name" });
        Assert.DoesNotContain(measurement.Tags, pair => pair.Key == "env");
    }
    
    [Fact]
    public void ItShouldIgnoreEvent()
    {
        var meter = new Meter("");
        var metricServerFactory = new TestableMetricServerFactory();

        var metricCollector = new MetricCollector<long>(meter, "example_metric");

        var app = new PrometheusApp(meter, metricServerFactory)
        {
            Configuration = """
                            - name: "example_metric"
                              type: "counter"
                              help: "Number of events"
                              filter: "@Level = 'Error'"
                              labels:
                                - name: "name"
                                  value: "{Name}"
                            """
        };

        app.Attach(new TestAppHost());

        var evt = Some.InformationEvent("Hello, {Name}!", "some_name");
        
        app.On(new Event<LogEvent>("event-1", 123, DateTime.UtcNow, evt));

        var measurements = metricCollector.GetMeasurementSnapshot();
        
        Assert.Empty(measurements);
    }
}