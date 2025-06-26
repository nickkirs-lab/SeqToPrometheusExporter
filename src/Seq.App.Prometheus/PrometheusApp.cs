using System.Diagnostics.Metrics;
using System.Net;
using Prometheus;
using Seq.Apps;
using Serilog.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Seq.App.Prometheus;

[SeqApp("Prometheus Metrics",
    Description = """
                  Exposes Prometheus metrics based on a YAML configuration file.
                  Metrics can be used to monitor the Seq event log.
                  """)]
public sealed class PrometheusApp : SeqApp, ISubscribeTo<LogEvent>, IDisposable
{
    [SeqAppSetting(DisplayName = "YAML configuration", InputType = SettingInputType.LongText, IsOptional = false, Syntax = "YAML")]
    public string Configuration { get; set; }

    [SeqAppSetting(DisplayName = "Metrics expire after", InputType = SettingInputType.Text, IsOptional = true,
        HelpText =
            "In TimeSpan format. Cannot be greater than 1 day. See https://github.com/prometheus-net/prometheus-net/tree/master/Prometheus/MeterAdapterOptions.cs#L18-L23")]
    public string MetricsExpireAfter { get; set; }

    [SeqAppSetting(DisplayName = "Bearer token", InputType = SettingInputType.Password, IsOptional = true,
        HelpText = "If empty, no authentication will be used")]
    public string? BearerToken { get; set; }

    [SeqAppSetting(DisplayName = "HTTP Listener port", InputType = SettingInputType.Integer, IsOptional = false)]
    public int Port { get; set; }

    [SeqAppSetting(DisplayName = "URL for metrics", InputType = SettingInputType.Text, IsOptional = true,
        HelpText = "If empty /metrics is used")]
    public string? Url { get; set; }

    private MetricHandler? _metricServer;

    private readonly List<Metric> _metrics = [];

    private readonly Meter _meter;
    private readonly IMetricServerFactory _metricServerFactory;

    public PrometheusApp() : this(new Meter("Seq.App.Prometheus"), new DefaultMetricServerFactory())
    {
    }

    internal PrometheusApp(Meter meter, IMetricServerFactory metricServerFactory)
    {
        _meter = meter;
        _metricServerFactory = metricServerFactory;
    }

    protected override void OnAttached()
    {
        Metrics.SuppressDefaultMetrics(new SuppressDefaultMetricOptions
        {
            SuppressProcessMetrics = true,
            SuppressEventCounters = true,
            SuppressDebugMetrics = true,
        });

        Metrics.ConfigureMeterAdapter(o =>
        {
            o.InstrumentFilterPredicate = i => i.Meter == _meter;
            
            if (!string.IsNullOrWhiteSpace(MetricsExpireAfter))
                o.MetricsExpireAfter = TimeSpan.Parse(MetricsExpireAfter);
        });

        var deserializer = new DeserializerBuilder()
            .WithEnforceRequiredMembers()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var metricDescriptors = deserializer.Deserialize<List<MetricDescriptor>>(Configuration);

        foreach (var metricDescriptor in metricDescriptors)
        {
            var metric = metricDescriptor.Type switch
            {
                "counter" => new CounterMetric(_meter, metricDescriptor),
                _ => throw new SeqAppException($"Metric type '{metricDescriptor.Type}' is not supported.")
            };

            _metrics.Add(metric);
        }

        var url = string.IsNullOrEmpty(Url) ? "metrics/" : Url;

        _metricServer = _metricServerFactory.Create(Port, url);
        _metricServer.Start();

        if (!string.IsNullOrEmpty(BearerToken) && _metricServer is MetricServer metricServer)
        {
            metricServer.RequestPredicate = Authenticate;
        }
    }

    private bool Authenticate(HttpListenerRequest request)
    {
        var authorization = request.Headers["Authorization"];

        if (string.IsNullOrEmpty(authorization))
            return false;

        if (!authorization.StartsWith("Bearer "))
            return false;

        var token = authorization.Substring("Bearer ".Length).Trim();
        return token == BearerToken;
    }

    public void On(Event<LogEvent> evt)
    {
        foreach (var metric in _metrics)
            metric.Accept(evt.Data);
    }

    public void Dispose()
    {
        _metricServer?.Dispose();
    }
}