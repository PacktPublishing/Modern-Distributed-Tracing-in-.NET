using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

internal class ExplicitConfiguration
{
    public static MeterProvider BuildMetricsPipeline()
    {
        return Sdk.CreateMeterProviderBuilder()
            .AddOtlpExporter((exporterOptions, readerOptions) => readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000)
            .AddView("processor.lag", new ExplicitBucketHistogramConfiguration()
            {
                Boundaries = new double[] { 0, 500, 1000, 2500, 5000, 7500, 10000, 25000, 50000 },
                RecordMinMax = false
            })
            .AddMeter("queue.*")
            .ConfigureResource(resource => resource.AddService("metrics"))
            .Build()!;
    }
}
