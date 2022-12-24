using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace memefunc;

public class Storage
{
    [Function("storage-upload")]
    [BlobOutput("memes/{name}")]
    public static async Task<byte[]> Upload([HttpTrigger(AuthorizationLevel.Function, "put", Route = "memes/{name}")] HttpRequestData request)
    {
        using var output = new MemoryStream();
        await request.Body.CopyToAsync(output);

        output.Position = 0;
        return output.ToArray();
    }

    [Function("storage-download")]
    public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "memes/{name}")] HttpRequestData request,
        [BlobInput("memes/{name}")] byte[] input)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Body = new MemoryStream(input);
        response.Headers.Add("Content-Type", "image/png");

        return response;
    }
}
