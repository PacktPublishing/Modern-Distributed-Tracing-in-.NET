using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace client;

public class GrpcTracingInterceptor : Interceptor
{
    private static readonly ActivitySource Source = new("Client.Grpc");
    private readonly string _host;
    private readonly int _port;
    private readonly TextMapPropagator _propagator;

    public GrpcTracingInterceptor(Uri endpoint, TextMapPropagator propagator) {
        _host = endpoint.Host ?? throw new ArgumentException("Host is null", nameof(endpoint));
        _port = endpoint.Port;
        _propagator = propagator;
    }
    
    public override AsyncUnaryCall<Res> AsyncUnaryCall<Req, Res>(Req request, ClientInterceptorContext<Req, Res> ctx, AsyncUnaryCallContinuation<Req, Res> continuation)
    {
        var activity = Source.StartActivity(ctx.Method.FullName, ActivityKind.Client);
        ctx = InjectTraceContext(activity, ctx);
        if (activity?.IsAllDataRequested != true)
            return continuation(request, ctx);

        SetRpcAttributes(activity, ctx.Method);

        var call = continuation(request, ctx);
        return new AsyncUnaryCall<Res>(
            HandleResponse(call.ResponseAsync, activity, call),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private static async Task<Res> HandleResponse<Req, Res>(Task<Res> original, Activity act, AsyncUnaryCall<Req> call)
    {
        try
        {
            var response = await original;
            SetStatus(act, call.GetStatus());
            return response;
        } 
        catch (Exception ex)
        {
            SetStatus(act, ex);
            throw;
        }
        finally
        {
            act.Dispose();
        }
    }

    private ClientInterceptorContext<Req, Res> InjectTraceContext<Req, Res>(Activity? act, ClientInterceptorContext<Req, Res> ctx) 
        where Req : class where Res : class
    {
        if (act == null) return ctx;

        if (ctx.Options.Headers == null)
        {
            ctx = new ClientInterceptorContext<Req, Res>(ctx.Method, ctx.Host, ctx.Options.WithHeaders(new Metadata()));
        }

        _propagator.Inject(new PropagationContext(act.Context, Baggage.Current),
            ctx.Options.Headers!, 
            static (headers, k, v) => headers.Add(k, v));

        return ctx;
    }

    private void SetRpcAttributes<Req, Res>(Activity act, Method<Req, Res> method)
    {
        act.SetTag("rpc.system", "grpc");
        act.SetTag("rpc.service", method.ServiceName);
        act.SetTag("rpc.method", method.Name);
        act.SetTag("net.peer.name", _host);
        if (_port != 80 && _port != 443)
        {
            act.SetTag("net.peer.port", _port);
        }
    }

    private static void SetStatus(Activity act, Grpc.Core.Status status)
    {
        act.SetTag("rpc.grpc.status_code", (int)status.StatusCode);

        var activityStatus = status.StatusCode != Grpc.Core.StatusCode.OK ?
            ActivityStatusCode.Error : ActivityStatusCode.Unset;

        act.SetStatus(activityStatus, status.Detail);
    }

    
    public override AsyncDuplexStreamingCall<Req, Res> AsyncDuplexStreamingCall<Req, Res>(ClientInterceptorContext<Req, Res> ctx, AsyncDuplexStreamingCallContinuation<Req, Res> continuation)
    {
        var activity = Source.StartActivity(ctx.Method.FullName, ActivityKind.Client);
        ctx = InjectTraceContext(activity, ctx);
        if (activity == null || !activity.IsAllDataRequested) return continuation(ctx);

        SetRpcAttributes(activity, ctx.Method);
        var call = continuation(ctx);

        var requestStream = new RequestEventStream<Req>(activity, call.RequestStream);
        var responseStream = new ResponseEventStream<Res>(activity, call.ResponseStream);
        return new AsyncDuplexStreamingCall<Req, Res>(
            requestStream,
            responseStream,
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            () => 
            {
                SetStatus(activity, call.GetStatus());
                activity.Dispose();
                call.Dispose();
            });
    }


    private static void SetStatus(Activity activity, Exception exception)
    {
        activity.SetTag("rpc.grpc.status_code", (int)(exception is RpcException rpcEx ? rpcEx.StatusCode : Grpc.Core.StatusCode.Unknown));
        ActivityStatusCode activityStatusCode = ActivityStatusCode.Error;
        activity.RecordException(exception);

        activity.SetStatus(activityStatusCode, exception.Message);
    }

    private class ResponseEventStream<T> : IAsyncStreamReader<T>
    {
        private readonly IAsyncStreamReader<T> _inner;
        private readonly Activity _activity;

        private int _responsesReceived;

        public ResponseEventStream(Activity activity, IAsyncStreamReader<T> inner)
        {
            _inner = inner;
            _activity = activity;
            _responsesReceived = 0;
        }

        public T Current => _inner.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            try
            {
                if (await _inner.MoveNext(cancellationToken))
                {
                    ActivityTagsCollection tags = new()
                {
                    { "message.type", "RECEIVED" },
                    { "message.id", Interlocked.Increment(ref _responsesReceived) },

                };

                    _activity.AddEvent(new ActivityEvent("message", tags: tags));

                    return true;
                }

                return false;
            } 
            catch (Exception ex)
            {
                SetStatus(_activity, ex);
                _activity.Dispose();
                throw;
            }
        }
    }

    private class RequestEventStream<T> : IClientStreamWriter<T>
    {
        private readonly IClientStreamWriter<T> _inner;
        private readonly Activity _activity;
        private int _requestsSent;

        public RequestEventStream(Activity activity, IClientStreamWriter<T> inner)
        {
            _inner = inner;
            _activity = activity;
            _requestsSent = 0;
        }

        public WriteOptions? WriteOptions { get => _inner.WriteOptions; set => _inner.WriteOptions = value; }

        public Task CompleteAsync() => _inner.CompleteAsync();

        public async Task WriteAsync(T message)
        {
            try
            {
                await _inner.WriteAsync(message);
                ActivityTagsCollection tags = new()
                {
                    { "message.type", "SENT" },
                    { "message.id", Interlocked.Increment(ref _requestsSent) },
                };

                _activity.AddEvent(new ActivityEvent("message", tags: tags));

            }
            catch (Exception ex)
            {
                _activity.RecordException(ex);
                throw;
            }
        }
    }
}
