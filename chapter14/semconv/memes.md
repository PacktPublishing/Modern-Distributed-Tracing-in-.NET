# Memes semantic conventions

This document describes semantic conventions for memes application. It covers attributes that can be used on different telemetry signals.

## Attributes

When recording information about meme, it is required to use following attributes. They are available in [`SemanticAttributes](TODO) class in `Memes.OpenTelemetry.Common` package. 

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

Use [`EventService`](TODO) to record meme events. Following events are available:

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
- Message: "download {memes.meme.name} {memes.meme.type} {memes.meme.size}"
- 
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
- Message: "upload {memes.meme.name} {memes.meme.type} {memes.meme.size}"

**Make sure to document new events in this table.**

## Traces

All spans related to meme operations are expected to have `memes.meme_name` attribute. It's recorded with [`MemeNameEnrichingProcessor`](TODO) processor and is enabled by default when configuring collection with `Memes.OpenTelemetry.Common.OpenTelemetryExtensions.ConfigureTelemetry` method.

## Metrics

Make sure to document new metrics related to memes in this document. 

>Note: Meme name (`memes.meme.name`) and size (`memes.meme.size`) have high cardinality and cannot be used on metrics. 

