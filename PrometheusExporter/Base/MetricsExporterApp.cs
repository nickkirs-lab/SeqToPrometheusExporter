using Prometheus;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrometheusExporter.Base;

[SeqApp("Prometheus Metric Exporter", Description = "Exports filtered logs as Prometheus metrics.")]
public class SeqMetricExporterApp : SeqApp, ISubscribeTo<LogEventData>, IDisposable
{
    [SeqAppSetting(DisplayName = "Job Name")]
    public string? Job { get; set; }

    [SeqAppSetting(InputType = SettingInputType.LongText, DisplayName = "Metric Configuration (YAML)")]
    public string? MetricYaml { get; set; }

    private readonly List<(MetricDefinition def, PrometheusMetric metric)> _metrics = new();
    private MetricServer? _server;
    private bool _disposed;

    private readonly Dictionary<string, string> DefaultLabelValues = new();

    private readonly Dictionary<string, string> LabelToPropertyMap = new()
    {
        ["assembly"] = "AssemblyName",
        ["host"] = "Host",
    };

    protected override void OnAttached()
    {
        var hostName = Environment.MachineName;
        DefaultLabelValues["host"] = hostName;
        DefaultLabelValues["assembly"] = AppDomain.CurrentDomain.FriendlyName;

        var yamlContent = !string.IsNullOrWhiteSpace(MetricYaml)
            ? MetricYaml
            : TryLoadYamlFromEnv();

        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            Log.Error("Metric YAML is not provided and SEQ_APP_METRICYAML_FILE not set.");
            return;
        }

        var config = ParseConfig(yamlContent);
        Job ??= config.Job;

        foreach (var def in config.Metrics)
        {
            if (string.IsNullOrWhiteSpace(def.Name) || string.IsNullOrWhiteSpace(def.Filter))
            {
                Log.Warning("Skipping invalid metric definition with missing name or filter.");
                continue;
            }

            if (!string.Equals(def.Type, "counter", StringComparison.OrdinalIgnoreCase))
                continue;

            var tagKeys = def.Tags?
                .Select(t =>
                {
                    var prop = t.Trim('{', '}');
                    return LabelToPropertyMap.FirstOrDefault(kvp => kvp.Value == prop).Value ?? prop.ToLowerInvariant();
                })
                .ToArray() ?? Array.Empty<string>();

            var metric = new PrometheusMetric(def.Name, tagKeys);
            _metrics.Add((def, metric));
        }

        _server = new MetricServer(hostname: "localhost", port: 9091);
        _server.Start();
    }

    public void On(Event<LogEventData> evt)
    {
        foreach (var (def, metric) in _metrics)
        {
            try
            {
                if (!EventMatchesFilter(evt.Data, def.Filter))
                    continue;

                var tagValues = new Dictionary<string, string>();
                bool skip = false;

                if (def.Tags != null)
                {
                    foreach (var label in def.Tags)
                    {
                        var propKey = label.Trim('{', '}');
                        var labelKey = LabelToPropertyMap.FirstOrDefault(kvp => kvp.Value == propKey).Key ?? propKey.ToLowerInvariant();

                        if (!evt.Data.Properties.TryGetValue(propKey, out var value) || value == null)
                        {
                            if (DefaultLabelValues.TryGetValue(labelKey, out var fallback))
                            {
                                tagValues[labelKey] = fallback;
                            }
                            else
                            {
                                Log.Warning("Skipping metric {Metric} due to missing property {Property}", def.Name, propKey);
                                skip = true;
                                break;
                            }
                        }
                        else
                        {
                            tagValues[labelKey] = value.ToString().Trim('"');
                        }
                    }
                }

                if (!skip)
                {
                    metric.Inc(tagValues);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating metric {MetricName}", def.Name);
            }
        }
    }

    private static string? TryLoadYamlFromEnv()
    {
        var path = Environment.GetEnvironmentVariable("SEQ_APP_METRICYAML_FILE");
        return path != null && File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static MetricConfig ParseConfig(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<MetricConfig>(yaml);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _server?.Stop();
        _server?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private abstract record Expr;
    private record AndExpr(Expr Left, Expr Right) : Expr;
    private record OrExpr(Expr Left, Expr Right) : Expr;
    private record ConditionExpr(string Key, string Value) : Expr;

    private static bool EventMatchesFilter(LogEventData data, string filter)
    {
        try
        {
            var expr = ParseExpression(filter);
            return EvaluateExpression(expr, data);
        }
        catch
        {
            return false;
        }
    }

    private static Expr ParseExpression(string input)
    {
        var tokens = Tokenize(input);
        return ParseOr(tokens);
    }

    private static Queue<string> Tokenize(string input)
    {
        var tokens = new Queue<string>();
        var buffer = new StringBuilder();
        bool inQuotes = false;
        char quoteChar = '\0';

        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (inQuotes)
            {
                buffer.Append(c);
                if (c == quoteChar)
                {
                    inQuotes = false;
                    tokens.Enqueue(buffer.ToString());
                    buffer.Clear();
                }
                continue;
            }

            if (c == '"' || c == '\'')
            {
                inQuotes = true;
                quoteChar = c;
                buffer.Append(c);
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (buffer.Length > 0)
                {
                    tokens.Enqueue(buffer.ToString());
                    buffer.Clear();
                }
                continue;
            }

            if (c == '(' || c == ')' || c == '=')
            {
                if (buffer.Length > 0)
                {
                    tokens.Enqueue(buffer.ToString());
                    buffer.Clear();
                }
                tokens.Enqueue(c.ToString());
            }
            else
            {
                buffer.Append(c);
            }
        }

        if (buffer.Length > 0)
            tokens.Enqueue(buffer.ToString());

        return tokens;
    }

    private static Expr ParseOr(Queue<string> tokens)
    {
        var left = ParseAnd(tokens);

        while (tokens.Count > 0 && string.Equals(tokens.Peek(), "or", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Dequeue();
            var right = ParseAnd(tokens);
            left = new OrExpr(left, right);
        }

        return left;
    }

    private static Expr ParseAnd(Queue<string> tokens)
    {
        var left = ParsePrimary(tokens);

        while (tokens.Count > 0 && string.Equals(tokens.Peek(), "and", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Dequeue();
            var right = ParsePrimary(tokens);
            left = new AndExpr(left, right);
        }

        return left;
    }

    private static Expr ParsePrimary(Queue<string> tokens)
    {
        if (tokens.Count == 0)
            throw new FormatException("Unexpected end of tokens");

        var token = tokens.Dequeue();

        if (token == "(")
        {
            var expr = ParseOr(tokens);
            if (tokens.Dequeue() != ")")
                throw new FormatException("Mismatched parentheses");
            return expr;
        }

        var key = token;
        if (tokens.Count < 2 || tokens.Dequeue() != "=")
            throw new FormatException("Expected '=' in condition");

        var valToken = tokens.Dequeue();
        var val = valToken.Trim().Trim('"', '\'');
        return new ConditionExpr(key, val);
    }

    private static bool EvaluateExpression(Expr expr, LogEventData data)
    {
        return expr switch
        {
            ConditionExpr cond => EvaluateCondition(cond, data),
            AndExpr and => EvaluateExpression(and.Left, data) && EvaluateExpression(and.Right, data),
            OrExpr or => EvaluateExpression(or.Left, data) || EvaluateExpression(or.Right, data),
            _ => false
        };
    }

    private static bool EvaluateCondition(ConditionExpr cond, LogEventData data)
    {
        var key = cond.Key;
        var expected = cond.Value;

        if (string.Equals(key, "Level", StringComparison.OrdinalIgnoreCase))
            return string.Equals(data.Level.ToString(), expected, StringComparison.OrdinalIgnoreCase);

        if (data.Properties.TryGetValue(key, out var propVal))
            return string.Equals(propVal?.ToString()?.Trim('"'), expected, StringComparison.OrdinalIgnoreCase);

        return false;
    }
}
