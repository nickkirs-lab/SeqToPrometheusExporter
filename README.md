
# Seq Prometheus Exporter App

A Seq App plugin that exports filtered log events as Prometheus metrics.

## 🚀 Features

- Export `counter` metrics based on log filters
- Flexible YAML-based metric configuration
- Supports tag extraction from log properties
- Built-in fallback for common labels like `{AssemblyName}` and `{Host}`
- Supports logical expressions: `and`, `or`, parentheses

## 📦 Example YAML Configuration

```yaml
job: seq-logs
metrics:
  - type: counter
    name: aspnetcore_500_errors
    filter: Level = 'Error' and StatusCode = 500
    tags:
      - '{AssemblyName}'
  - type: counter
    name: graphql_errors
    filter: Level = 'Error' and EndPointName = 'Hot GraphQL Pipeline'
    tags:
      - '{AssemblyName}'
      - '{Host}'
```

---

## ⚙️ Setup

1. Build the project and package it as a Seq App (`.pkg` or NuGet).
2. Go to **Settings > Apps** in your Seq instance.
3. Install this app and provide the YAML configuration using one of the following:
   - Directly in the **Metric Configuration** field
   - Or via environment variable: `SEQ_APP_METRICYAML_FILE=/path/to/your.yaml`


## 🧪 Local Testing

You can simulate log ingestion using Serilog:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.WithProperty("AssemblyName", "MyApp")
    .Enrich.WithProperty("Host", "api.myservice.local")
    .CreateLogger();
```

## 📚 Technologies Used

- .NET 6+
- [prometheus-net](https://github.com/prometheus-net/prometheus-net)
- [YamlDotNet](https://github.com/aaubry/YamlDotNet)
- [Seq.Apps SDK](https://docs.datalust.co/docs/extending-seq)

## 📄 License

MIT License

## 💡 Roadmap Ideas

- Support for `gauge`, `histogram`, and `summary` metric types
- Filter expression caching for performance
- Live reload of configuration without restart
- UI integration for testing filters within Seq
