using Prometheus;

namespace Seq.App.Prometheus;

public interface IMetricServerFactory
{
    MetricHandler Create(int port, string url = "metrics/");
}

internal class DefaultMetricServerFactory : IMetricServerFactory
{
    public MetricHandler Create(int port, string url)
    {
        return new MetricServer(port, url);
    }
}