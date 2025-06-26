# Seq.App.Prometheus

A Seq app that exposes Prometheus metrics based on a YAML configuration file, allowing you to monitor your Seq event log through Prometheus.

## Overview

Seq.App.Prometheus is a plugin for [Seq](https://datalust.co/seq) that enables you to expose custom metrics from your Seq event log in Prometheus format. This allows you to integrate your Seq logs with Prometheus monitoring and create dashboards using tools like Grafana.

## Features

- Expose custom metrics from Seq logs in Prometheus format
- Configure metrics through YAML configuration
- Support for counter metrics
- Optional Bearer token authentication
- Customizable HTTP endpoint and port
- Built on .NET 8.0

## Installation

1. Download the latest release from the releases page
2. Install the app in your Seq instance through the Seq UI:
   - Go to Settings > Apps
   - Click "Add App"
   - Upload the downloaded package

## Configuration

The app requires the following settings:

### Required Settings

- **YAML configuration**: A YAML file that defines your metrics
- **HTTP Listener port**: The port number where the metrics endpoint will be exposed

### Optional Settings

- **Bearer token**: Authentication token for securing the metrics endpoint
- **URL for metrics**: Custom URL path for the metrics endpoint (defaults to `/metrics`)

### YAML Configuration Format

The YAML configuration file defines the metrics you want to expose. Each metric is defined as an object in the configuration array.

#### Schema

```yaml
- name: string              # Required: Name of the metric
  description: string       # Optional: Description of what the metric measures
  type: string              # Required: Type of metric (currently only 'counter' is supported)
  filter: string            # Required: Seq expression language filter condition
  labels:                   # Optional: Array of label configurations
    - name: string          # Required: Name of the label
      value: string         # Required: Value template using Seq property syntax
```

#### Examples

Basic counter metric:

```yaml
- name: log_events_total
  description: Total number of log events
  type: counter
  labels:
    - name: level
      value: "{@Level}"
  filter: "@Level = 'Error'"
```

Multiple labels with filter:

```yaml
- name: api_requests_total
  description: Total number of API requests
  type: counter
  labels:
    - name: method
      value: "{@Properties['HttpMethod']}"
    - name: endpoint
      value: "{@Properties['Path']}"
    - name: status
      value: "{@Properties['StatusCode']}"
  filter: "@Properties['HttpMethod'] = 'POST' and @Properties['StatusCode'] >= 400"
```

Using custom properties with complex filter:

```yaml
- name: business_events_total
  description: Total number of business events
  type: counter
  labels:
    - name: event_type
      value: "{@Properties['EventType']}"
    - name: tenant
      value: "{@Properties['TenantId']}"
    - name: environment
      value: "{@Properties['Environment']}"
  filter: "@Properties['EventType'] in ('OrderCreated', 'OrderCompleted') and @Properties['Environment'] = 'Production'"
```

#### Label Value Templates

Label values can use Seq's property syntax to extract values from log events:

- `{@Level}` - Log level
- `{@Properties['PropertyName']}` - Custom property value
- `{@Message}` - Log message
- `{@Timestamp}` - Event timestamp
- `{@Exception}` - Exception details (if present)

#### Filter Expressions

The `filter` field uses Seq's expression language to determine which events should be counted. Examples of valid filter expressions:

- `@Level = 'Error'` - Count only error level events
- `@Properties['HttpMethod'] = 'POST'` - Count only POST requests
- `@Properties['StatusCode'] >= 400` - Count only error responses
- `@Properties['EventType'] in ('OrderCreated', 'OrderCompleted')` - Count specific event types
- `@Properties['Environment'] = 'Production'` - Count only production events
- `@Level = 'Error' and @Properties['Source'] = 'ApiController'` - Count errors from specific source
- `@Properties['Duration'] > 1000` - Count slow operations

## Usage

1. Configure the app in Seq with your desired settings
2. Access the metrics endpoint at `http://your-seq-server:port/metrics` (or your custom URL)
3. Configure Prometheus to scrape the metrics endpoint
4. Use the metrics in your Prometheus dashboards

### Authentication

If you've configured a Bearer token:

1. Include the token in your requests using the `Authorization` header
2. Format: `Authorization: Bearer your-token-here`

## Development

### Prerequisites

- .NET 8.0 SDK or later

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

## Dependencies

- prometheus-net (8.2.1)
- Seq.Apps (2023.4.0)
- Seq.Syntax (1.0.0)
- YamlDotNet (16.3.0)
