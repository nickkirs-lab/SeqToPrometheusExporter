﻿using Serilog;
using Serilog.Events;
using Xunit.Sdk;

namespace Seq.App.Prometheus.Tests.Support;

internal static class Some
{
    public static LogEvent InformationEvent(string messageTemplate = "Hello, world!", params object?[] propertyValues)
    {
        return LogEvent(LogEventLevel.Information, messageTemplate, propertyValues);
    }

    public static LogEvent LogEvent(LogEventLevel level, string messageTemplate = "Hello, world!", params object?[] propertyValues)
    {
        var log = new LoggerConfiguration().CreateLogger();
#pragma warning disable Serilog004 // Constant MessageTemplate verifier
        if (!log.BindMessageTemplate(messageTemplate, propertyValues, out var template, out var properties))
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
        {
            throw new XunitException("Template could not be bound.");
        }

        return new LogEvent(DateTimeOffset.Now, level, null, template, properties);
    }
}