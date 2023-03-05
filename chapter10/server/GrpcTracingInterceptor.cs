using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace server;

public class GrpcTracingInterceptor : Interceptor
{
    private static readonly ActivitySource Source = new("Server.Grpc");
    private readonly TextMapPropagator _propagator;

    public GrpcTracingInterceptor(TextMapPropagator propagator) {
        _propagator = propagator;
    }
    
    public override async Task<Res> UnaryServerHandler<Req, Res>(Req request, ServerCallContext ctx, UnaryServerMethod<Req, Res> continuation)
    {
        PropagationContext traceContext = _propagator.Extract(default, ctx.RequestHeaders, static (headers, k) => new[] { headers.GetValue(k) });
        Baggage.Current = traceContext.Baggage;
        using var activity = Source.StartActivity(ctx.Method, ActivityKind.Server, traceContext.ActivityContext);

        if (activity?.IsAllDataRequested != true) return await continuation(request, ctx);

        SetRpcAttributes(activity, ctx.Host, ctx.Method);

        try
        {
            var response = await continuation(request, ctx);
            SetStatus(activity, ctx.Status);
            return response;
        }
        catch (Exception ex)
        {
            SetStatus(activity, ex);
            throw;
        }
    }

    public override async Task DuplexStreamingServerHandler<Req, Res>(IAsyncStreamReader<Req> requestStream, IServerStreamWriter<Res> responseStream, ServerCallContext context, DuplexStreamingServerMethod<Req, Res> continuation)
        where Req : class
        where Res : class
    {
        PropagationContext traceContext = _propagator.Extract(default, context.RequestHeaders, Getter);
        Baggage.Current = traceContext.Baggage;
        using var activity = Source.StartActivity(context.Method, ActivityKind.Server, traceContext.ActivityContext);
        if (activity?.IsAllDataRequested != true)
        {
            await continuation(requestStream, responseStream, context);
            return;
        }

        SetRpcAttributes(activity, context.Host, context.Method);

        var wrappedRequestStream = new RequestStreamWithEvents<Req>(activity, requestStream);
        var wrappedResponseStream = new ResponseStreamWithEvents<Res>(activity, responseStream);
        try
        {
            await continuation(wrappedRequestStream, wrappedResponseStream,  context);
            SetStatus(activity, context.Status);
        }
        catch (Exception ex)
        {
            // if exception happens after first response, it won't be reflected in context.Status - request if over and this is a steram after.
            SetStatus(activity, ex);
            throw;
        }
    }

    private static IEnumerable<string> Getter(Metadata headers, string fieldName) =>
        headers.Where(h => h.Key == fieldName).Select(h => h.Value);


    private static void SetRpcAttributes(Activity activity, string authority, string methodName)
    {
        GetHostAndPort(authority, out var host, out var port);
        GetServiceAndMethod(methodName, out var service, out var method);

        activity.SetTag("rpc.system", "grpc");
        activity.SetTag("rpc.service", service);
        activity.SetTag("rpc.method", method);
        activity.SetTag("net.host.name", host);
        if (port != 80 && port != 443)
        {
            activity.SetTag("net.host.port", port);
        }
    }

    private static void SetStatus(Activity activity, Grpc.Core.Status status)
    {
        activity.SetTag("rpc.grpc.status_code", (int)status.StatusCode);

        ActivityStatusCode activityStatusCode = ActivityStatusCode.Unset;
        if (status.StatusCode != Grpc.Core.StatusCode.OK)
        {
            activityStatusCode = ActivityStatusCode.Error;
        }

        if (status.DebugException != null) { 
            activity.RecordException(status.DebugException);
        }

        activity.SetStatus(activityStatusCode, status.Detail);
    }

    private static void SetStatus(Activity activity, Exception exception)
    {
        activity.SetTag("rpc.grpc.status_code", (int)Grpc.Core.StatusCode.Unknown);

        ActivityStatusCode activityStatusCode = ActivityStatusCode.Error;
        activity.RecordException(exception);

        activity.SetStatus(activityStatusCode, exception.Message);
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


    private class ResponseStreamWithEvents<T> : IServerStreamWriter<T>
    {
        private readonly IServerStreamWriter<T> _inner;
        private readonly Activity _activity;
        public int ResponsesSent => _responsesSent;
        private int _responsesSent;

        public ResponseStreamWithEvents(Activity activity, IServerStreamWriter<T> inner)
        {
            _inner = inner;
            _activity = activity;
            _responsesSent = 0;
        }

        public WriteOptions? WriteOptions { get => _inner.WriteOptions; set => _inner.WriteOptions = value; }

        public async Task WriteAsync(T message)
        {
            await _inner.WriteAsync(message);
            ActivityTagsCollection tags = new()
            {
                { "message.type", "SENT" },
                { "message.id", Interlocked.Increment(ref _responsesSent) },

            };

            _activity.AddEvent(new ActivityEvent("message", tags: tags));
        }
    }

    private class RequestStreamWithEvents<T> : IAsyncStreamReader<T>
    {
        private readonly IAsyncStreamReader<T> _inner;
        private readonly Activity _activity;
        public int RequestsReceived => _requestsReseived;
        private int _requestsReseived;

        public RequestStreamWithEvents(Activity activity, IAsyncStreamReader<T> inner)
        {
            _inner = inner;
            _activity = activity;
            _requestsReseived = 0;
        }

        public T Current => _inner.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (await _inner.MoveNext(cancellationToken))
            {
                ActivityTagsCollection tags = new()
                {
                    { "message.type", "RECEIVED" },
                    { "message.id", Interlocked.Increment(ref _requestsReseived) },

                };

                _activity.AddEvent(new ActivityEvent("message", tags: tags));

                return true;
            }

            return false;
        }
    }
}
