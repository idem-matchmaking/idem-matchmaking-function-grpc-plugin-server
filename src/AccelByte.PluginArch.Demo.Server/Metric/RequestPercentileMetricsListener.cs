using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation;

using HdrHistogram;
using OpenTelemetry.Metrics;
using OpenTelemetry.Internal;
using OpenTelemetry.Instrumentation.AspNetCore;
using System.Reflection;

namespace AccelByte.PluginArch.Demo.Server.Metric
{
    public class RequestPercentileMetricsListener
    {
        public const string OTEL_INSTRUMENT_METER_NAME = "OpenTelemetry.Instrumentation.AspNetCore";

        public const string HTTP_DURATION_INSTRUMENT_NAME = "http.server.duration";

        public const string HTTP_LATENCY_INSTRUMENT_NAME = "http.server.latency";


        private Meter _TheMeter;

        private LongHistogram _ComputeHistogram;

        private ObservableGauge<double> _P99_Gauge;

        private ObservableGauge<double> _P95_Gauge;

        public RequestPercentileMetricsListener(string meterName, string meterVersion)
        {
            _TheMeter = new Meter(meterName, meterVersion);

            _ComputeHistogram = new LongHistogram(TimeStamp.Hours(1), 3);

            _P99_Gauge = _TheMeter.CreateObservableGauge<double>(HTTP_LATENCY_INSTRUMENT_NAME + ".p99", () =>
            {
                return (double)_ComputeHistogram.GetValueAtPercentile(99) / 1000;
            }, "ms", "compute the p99 latency of HTTP requests");

            _P95_Gauge = _TheMeter.CreateObservableGauge<double>(HTTP_LATENCY_INSTRUMENT_NAME + ".p95", () =>
            {
                return (double)_ComputeHistogram.GetValueAtPercentile(95) / 1000;
            }, "ms", "compute the p95 latency of HTTP requests");

            MeterListener listener = new MeterListener()
            {
                InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == OTEL_INSTRUMENT_METER_NAME)
                        meterListener.EnableMeasurementEvents(instrument, null);
                }
            };

            //Activity.Current.Duration.TotalMilliseconds is double, make sure use the exact same type for this event callback
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                long adjValue = (long)Math.Round(measurement * 1000, 0);
                _ComputeHistogram.RecordValue(adjValue);                
            });

            listener.Start();
        }
    }

    public static class RequestPercentileMetricsListener_Extensions
    {
        public static MeterProviderBuilder AddRequestLatencyMetric(
            this MeterProviderBuilder builder)
        {
            AssemblyName forMeterId = typeof(RequestPercentileMetricsListener).Assembly.GetName();
            string meterName = forMeterId.Name!;

            new RequestPercentileMetricsListener(meterName, forMeterId.Version!.ToString());

            return builder.AddMeter(meterName);
        }
    }
}
