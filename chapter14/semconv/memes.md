# Memes semantic conventions

This document describes semantic conventions for memes application. It covers attributes that can be used on different telemetry signals.

## Attributes

When recording information about meme, it is required to use following attributes. They are available in [`SemanticAttributes`](https://github.com/PacktPublishing/Modern-Distributed-Tracing-in-.NET/blob/main/chapter14/Memes.OpenTelemetry.Common/SemanticConventions.cs) class in `Memes.OpenTelemetry.Common` package. 

<!-- semconv memes.meme -->
| Attribute  | Type | Description  | Examples  | Requirement Level |
|---|---|---|---|---|
| `memes.meme.name` | string | Unique and sanitized meme name | `this is fine` | Required |
| `memes.meme.size` | int | Meme size in bytes. | `49335`; `12345` | Opt-In |
| `memes.meme.type` | string | Meme type. | `png`; `jpg` | Opt-In |

`memes.meme.type` has the following list of well-known values. If one of them applies, then the respective value MUST be used, otherwise a custom value MAY be used.

| Value  | Description |
|---|---|
| `png` | PNG image type. |
| `jpg` | JPG image type. |
| `unknown` | unknown type. |
<!-- endsemconv -->

## Events

Use [`EventService`](https://github.com/PacktPublishing/Modern-Distributed-Tracing-in-.NET/blob/main/chapter14/Memes.OpenTelemetry.Common/EventService.cs) to record meme events. Following events are available:

## Download meme event

<!-- semconv meme.download.event -->
The event name MUST be `download_meme`.

| Attribute  | Type | Description  | Examples  | Requirement Level |
|---|---|---|---|---|
| `memes.meme.name` | string | Unique and sanitized meme name | `this is fine` | Required |
| `memes.meme.size` | int | Meme size in bytes. | `49335`; `12345` | Required |
| `memes.meme.type` | string | Meme type. | `png`; `jpg` | Required |
<!-- endsemconv -->

Event is populated using .NET ILogger with following additional properties:
- Event Id: `1`
- Severity: `Information`
- Category: `Memes.OpenTelemetry.Common.EventService`
- Message: `download {memes.meme.name} {memes.meme.type} {memes.meme.size} {event.name} {event.domain}`

**Example:**

```text
Timestamp: 2023-05-14 17:46:03.0772945 +0000 UTC
SeverityText: Information
SeverityNumber: Info(9)
Body: Str(download {memes.meme.name} {memes.meme.type} {memes.meme.size} {event.name} {event.domain})
Attributes:
     -> dotnet.ilogger.category: Str(Memes.OpenTelemetry.Common.EventService)
     -> Id: Int(1)
     -> memes.meme.name: Str(this is fine)
     -> memes.meme.type: Str(png)
     -> memes.meme.size: Int(126864)
     -> event.name: Str(download_meme)
     -> event.domain: Str(memes)
Trace ID: 6e13520b9ceca7c0b1195624bdd563e1
Span ID: f5ae8eda716716c6
Flags: 0
```

## Upload meme event

<!-- semconv meme.upload.event -->
The event name MUST be `upload_meme`.

| Attribute  | Type | Description  | Examples  | Requirement Level |
|---|---|---|---|---|
| `memes.meme.name` | string | Unique and sanitized meme name | `this is fine` | Required |
| `memes.meme.size` | int | Meme size in bytes. | `49335`; `12345` | Required |
| `memes.meme.type` | string | Meme type. | `png`; `jpg` | Required |
<!-- endsemconv -->

Event is populated using .NET ILogger with following additional properties:
- Event Id: `2`
- Severity: `Information`
- Category: `Memes.OpenTelemetry.Common.EventService`
- Message: `upload {memes.meme.name} {memes.meme.type} {memes.meme.size} {event.name} {event.domain}`

**Example:**

```text
Timestamp: 2023-05-14 17:46:03.0587704 +0000 UTC
SeverityText: Information
SeverityNumber: Info(9)
Body: Str(upload {memes.meme.name} {memes.meme.type} {memes.meme.size} {event.name} {event.domain})
Attributes:
     -> dotnet.ilogger.category: Str(Memes.OpenTelemetry.Common.EventService)
     -> Id: Int(2)
     -> memes.meme.name: Str(this is fine)
     -> memes.meme.type: Str(png)
     -> memes.meme.size: Int(126864)
     -> event.name: Str(upload_meme)
     -> event.domain: Str(memes)
Trace ID: b654cd06b91d04700e4c425daa47a2c3
Span ID: fdf7f1facc2778fa
Flags: 0
```

**Make sure to document new events in this table.**

## Traces

All spans related to meme operations are expected to have `memes.meme_name` attribute. It's recorded with [`MemeNameEnrichingProcessor`](https://github.com/PacktPublishing/Modern-Distributed-Tracing-in-.NET/blob/main/chapter14/Memes.OpenTelemetry.Common/MemeNameEnrichingProcessor.cs) processor and is enabled by default when configuring collection with `Memes.OpenTelemetry.Common.OpenTelemetryExtensions.ConfigureTelemetry` method.

## Metrics

Make sure to document new metrics related to memes in this document. 

>Note: Meme name (`memes.meme.name`) and size (`memes.meme.size`) have high cardinality and cannot be used on metrics. 

