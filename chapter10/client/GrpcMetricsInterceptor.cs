using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace client;

public class GrpcMetricsInterceptor : Interceptor
{
    private static readonly Meter Meter = new ("Client.Grpc");
    private static readonly Histogram<int> ClientDuration = Meter.CreateHistogram<int>("rpc.client.duration", "ms", "Duration of outbound RPC");
    private static readonly Histogram<int> RequestCounter = Meter.CreateHistogram<int>("rpc.client.requests_per_rpc", "count", "Number of messages sent per RPC");
    private static readonly Histogram<int> ResponseCounter = Meter.CreateHistogram<int>("rpc.client.responses_per_rpc", "count", "Number of messages received per RPC");
    private readonly string _host;
    private readonly int _port;

    public GrpcMetricsInterceptor(Uri endpoint) {
        _host = endpoint.Host ?? throw new ArgumentException("Host is null", nameof(endpoint));
        _port = endpoint.Port;
    }
    
    private static bool AnyMetricEnabled()
    {
        return ClientDuration.Enabled || RequestCounter.Enabled || ResponseCounter.Enabled;
    }

    public override AsyncUnaryCall<Res> AsyncUnaryCall<Req, Res>(Req request, ClientInterceptorContext<Req, Res> context, AsyncUnaryCallContinuation<Req, Res> continuation)
    {
        if (!AnyMetricEnabled()) {
            return continuation(request, context);
        }

        var duration = Stopwatch.StartNew();
        var call = continuation(request, context);

        return new AsyncUnaryCall<Res>(
            HandleResponse(duration, call.ResponseAsync, context, call),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<Res> HandleResponse<Req, Res>(Stopwatch duration, Task<Res> original, ClientInterceptorContext<Req, Res> context, AsyncUnaryCall<Res> call)
        where Req : class where Res : class
    {

        StatusCode statusCode = StatusCode.Unknown;
        try
        {
            var response = await original;
            statusCode = call.GetStatus().StatusCode;
            return response;
        }
        catch (RpcException ex)
        {
            statusCode = ex.StatusCode;
            throw;
        }
        finally
        {
            RecordMetrics(duration, context.Method.ServiceName, context.Method.Name, statusCode, 1, 1);
        }
    }

    private void RecordMetrics(Stopwatch duration, string serviceName, string methodName, StatusCode statusCode, int requestsSent, int responsesReceived)
    {
        TagList metricsTags = GetMetricsAttributes(serviceName, methodName, statusCode);
        RequestCounter.Record(requestsSent, metricsTags);
        ResponseCounter.Record(responsesReceived, metricsTags);
        ClientDuration.Record((int)duration.ElapsedMilliseconds, metricsTags);
    }

    public override AsyncDuplexStreamingCall<Req, Res> AsyncDuplexStreamingCall<Req, Res>(ClientInterceptorContext<Req, Res> context, AsyncDuplexStreamingCallContinuation<Req, Res> continuation)
    {
        if (!AnyMetricEnabled())
        {
            return continuation(context);
        }

        var duration = Stopwatch.StartNew();
        var call = continuation(context);

        var requestCounter = new RequestCountingStream<Req>(call.RequestStream);

        void OnResponseError(int responses, Exception? ex)
        {
            RecordMetrics(duration, context.Method.ServiceName, context.Method.Name, StatusCode.Unknown, requestCounter.RequestsSent, responses);
        };

        var responseCounter = new ResponseCountingStream<Res>(call.ResponseStream, OnResponseError);

        return new AsyncDuplexStreamingCall<Req, Res>(
            requestCounter,
            responseCounter,
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            () =>
            {
                RecordMetrics(duration, context.Method.ServiceName, context.Method.Name, call.GetStatus().StatusCode, requestCounter.RequestsSent, responseCounter.ResponsesReceived);
                call.Dispose();
            });
    }

    private TagList GetMetricsAttributes(string serviceName, string methodName, StatusCode statusCode)
    {
        TagList tags = new ()
        {
            { "rpc.system", "grpc" },
            { "rpc.service", serviceName },
            { "rpc.method", methodName },
            { "net.peer.name", _host },
            { "rpc.grpc.status_code", (int)statusCode }
        };

        if (_port != 80 && _port != 443)
        {
            tags.Add("net.peer.port", _port);
        }

        return tags;
    }

    private class ResponseCountingStream<T> : IAsyncStreamReader<T>
    {
        public int ResponsesReceived  => _responsesReceived;
        private readonly IAsyncStreamReader<T> _inner;
        private readonly Action<int, Exception?> _onError;
        private int _responsesReceived;

        public ResponseCountingStream(IAsyncStreamReader<T> inner, Action<int, Exception?> onError)
        {
            _inner = inner;
            _responsesReceived = 0;
            _onError = onError;
        }

        public T Current => _inner.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            try
            {
                if (await _inner.MoveNext(cancellationToken))
                {
                    Interlocked.Increment(ref _responsesReceived);
                    return true;
                }

                return false;
            } catch (Exception e)
            {
                _onError.Invoke(_responsesReceived, e);
                throw;
            }
        }
    }

    private class RequestCountingStream<T> : IClientStreamWriter<T>
    {
        public int RequestsSent => _requestsSent;
        private readonly IClientStreamWriter<T> _inner;
        private int _requestsSent;

        public RequestCountingStream(IClientStreamWriter<T> inner)
        {
            _inner = inner;
            _requestsSent = 0;
        }

        public WriteOptions? WriteOptions { get => _inner.WriteOptions; set => _inner.WriteOptions = value; }

        public Task CompleteAsync() => _inner.CompleteAsync();

        public async Task WriteAsync(T message)
        {
            await _inner.WriteAsync(message);
            Interlocked.Increment(ref _requestsSent);
        }
    }
}
