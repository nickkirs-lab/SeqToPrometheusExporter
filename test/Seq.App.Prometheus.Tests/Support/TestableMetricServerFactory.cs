using Prometheus;

namespace Seq.App.Prometheus.Tests.Support;

internal class TestableMetricServerFactory : IMetricServerFactory
{
    public MetricHandler Create(int port, string url = "metrics/") => new DummyMetricHandler();

    private class DummyMetricHandler : MetricHandler
    {
        protected override Task StartServer(CancellationToken cancel)
        {
            return Task.CompletedTask;
        }
    }
}