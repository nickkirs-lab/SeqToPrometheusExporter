using Seq.App.Prometheus;
using Serilog;

using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    //.WriteTo.Seq("http://localhost:15341")
    .AuditTo.SeqApp<PrometheusApp>(new Dictionary<string, string>
    {
        [nameof(PrometheusApp.Configuration)] = """
                                                - name: "example_metric"
                                                  type: "counter"
                                                  help: "Number of events"
                                                  filter: "1 = 1"
                                                  labels:
                                                    - name: "name"
                                                      value: "{Name}"
                                                """,
        [nameof(PrometheusApp.Port)] = "31335"
    })
    .CreateLogger();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("Ctrl-C Terminating...");
    cts.Cancel();
    e.Cancel = true;
};

var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

while (await timer.WaitForNextTickAsync(cts.Token))
    logger.Information("Hello, {Name}!", Environment.UserName);
