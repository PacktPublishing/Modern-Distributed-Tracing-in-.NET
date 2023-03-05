using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace server;

public class GrpcMetricsInterceptor : Interceptor
{
    private static readonly Meter Meter = new ("Server.Grpc");
    private static readonly Histogram<int> ServerDuration = Meter.CreateHistogram<int>("rpc.server.duration", "ms", "Duration of inbound RPC");
    private static readonly Histogram<int> RequestCounter = Meter.CreateHistogram<int>("rpc.server.requests_per_rpc", "count", "Number of requests received per RPC");
    private static readonly Histogram<int> ResponseCounter = Meter.CreateHistogram<int>("rpc.server.responses_per_rpc", "count", "Number of responses sent per RPC");

    private static bool AnyMetricEnabled()
    {
        return ServerDuration.Enabled || RequestCounter.Enabled || ResponseCounter.Enabled;
    }
    public override async Task<Res> UnaryServerHandler<Req, Res>(Req request, ServerCallContext context, UnaryServerMethod<Req, Res> continuation)
    {
        if (!AnyMetricEnabled())
        {
            return await continuation(request, context);
        }

        var duration = Stopwatch.StartNew();

        var statusCode = StatusCode.Unknown;
        try
        {
            var response = await continuation(request, context);
            statusCode = context.Status.StatusCode;
            return response;
        }
        catch (RpcException ex)
        {
            statusCode = ex.StatusCode;
            throw;
        }
        finally
        {
            RecordMetrics(duration, context.Host, context.Method, statusCode, 1, 1);
        }
    }

    private static void RecordMetrics(Stopwatch duration, string host, string method, StatusCode statusCode, int requestsReceived, int responsesSent)
    {
        TagList metricsTags = GetMetricsAttributes(host, method, statusCode);
        RequestCounter.Record(requestsReceived, metricsTags);
        ResponseCounter.Record(responsesSent, metricsTags);
        ServerDuration.Record((int)duration.ElapsedMilliseconds, metricsTags);
    }

    public override async Task DuplexStreamingServerHandler<Req, Res>(IAsyncStreamReader<Req> requestStream, IServerStreamWriter<Res> responseStream, ServerCallContext context, DuplexStreamingServerMethod<Req, Res> continuation)
        where Req : class
        where Res : class
    {
        if (!AnyMetricEnabled())
        {
            await continuation(requestStream, responseStream, context);
        }

        var duration = Stopwatch.StartNew();

        var requestCounter = new RequestCountingStream<Req>(requestStream);
        var responseCounter = new ResponseCountingStream<Res>(responseStream);

        var statusCode = StatusCode.Unknown;
        try
        {
            await continuation(requestCounter, responseCounter,  context);
            statusCode = context.Status.StatusCode;
        }
        catch (RpcException ex)
        {
            statusCode = ex.StatusCode;
            throw;
        }
        finally
        {
            TagList metricsTags = GetMetricsAttributes(context.Host, context.Method, statusCode);

            ServerDuration.Record((int)duration.ElapsedMilliseconds, metricsTags);
            RequestCounter.Record(requestCounter.RequestsReceived, metricsTags);
            ResponseCounter.Record(responseCounter.ResponsesSent, metricsTags);
        }
    }

    private static TagList GetMetricsAttributes(string authority, string fullServiceName, StatusCode statusCode)
    {
        GetHostAndPort(authority, out var host, out var port);
        GetServiceAndMethod(fullServiceName, out var service, out var method);

        TagList tags = new ()
        {
            { "rpc.system", "grpc" },
            { "rpc.service", service },
            { "rpc.method", method },
            { "net.host.name", host },
            { "rpc.grpc.status_code", (int)statusCode }
        };

        if (port != 80 && port != 443)
        {
            tags.Add("net.host.port", port);
        }

        return tags;
    }

    private static void GetServiceAndMethod(string fullName, out string? service, out string method)
    {
        // could be a good idea to cache results
        int lastSlash = fullName.LastIndexOf('/');

        if (lastSlash == -1)
        {
            service = null;
            method = fullName;
        }
        else
        {
            service = fullName[..lastSlash];
            method = fullName[(lastSlash + 1)..];
        }
    }

    private static void GetHostAndPort(string authority, out string host, out int port)
    {
        // could be a good idea to cache results
        int colon = authority.IndexOf(':');
        if (colon == -1)
        {
            host = authority;
            port = 443;
        }
        else
        {
            host = authority[..colon];
            port = 443;
            var portStr = authority[(colon + 1)..];
            if (int.TryParse(portStr, out var p))
            {
                port = p;
            }
        }
    }


    private class ResponseCountingStream<T> : IServerStreamWriter<T>
    {
        private readonly IServerStreamWriter<T> _inner;
        public int ResponsesSent => _responsesSent;
        private int _responsesSent;

        public ResponseCountingStream(IServerStreamWriter<T> inner)
        {
            _inner = inner;
            _responsesSent = 0;
        }

        public WriteOptions? WriteOptions { get => _inner.WriteOptions; set => _inner.WriteOptions = value; }

        public async Task WriteAsync(T message)
        {
            await _inner.WriteAsync(message);
            Interlocked.Increment(ref _responsesSent);
        }
    }

    private class RequestCountingStream<T> : IAsyncStreamReader<T>
    {
        private readonly IAsyncStreamReader<T> _inner;
        public int RequestsReceived { get; private set; }
        private int _requestsReceived;

        public RequestCountingStream(IAsyncStreamReader<T> inner)
        {
            _inner = inner;
            _requestsReceived = 0;
        }

        public T Current => _inner.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (await _inner.MoveNext(cancellationToken))
            {
                Interlocked.Increment(ref _requestsReceived);
                return true;
            }
            return false;
        }
    }
}
