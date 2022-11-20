using OpenTelemetry.Trace;
using System.Diagnostics;

namespace frontend;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            Activity.Current?.RecordException(ex);
            throw;
        }
    }
}
