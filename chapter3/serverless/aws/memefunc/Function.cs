using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3.Model;
using System.Net;
using OpenTelemetry.Trace;
using OpenTelemetry;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry.Instrumentation.AWSLambda;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace memefunc;

public class Function
{
    private const string BucketName = "memestorage2";
    private static readonly AmazonS3Client Client;
    private static readonly TracerProvider TracerProvider;

    static Function()
    {
        Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());
        TracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddAWSLambdaConfigurations()
            .AddOtlpExporter()
            .AddAWSInstrumentation(opt => opt.SuppressDownstreamInstrumentation = false)
            .AddHttpClientInstrumentation()
            .Build()!;
        Client = new AmazonS3Client(Amazon.RegionEndpoint.USWest2);
    }

    public async Task<APIGatewayProxyResponse> TracingHandler(
        APIGatewayHttpApiV2ProxyRequest req, ILambdaContext ctx) =>
        await AWSLambdaWrapper.TraceAsync(TracerProvider, MemeHandler, req, ctx);

    public async Task<APIGatewayProxyResponse> MemeHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        string name = request.QueryStringParameters["name"];
       
        if (request.RequestContext.Http.Method == "GET")
        {
            return await Download(name);
        } 
        else if (request.RequestContext.Http.Method == "PUT")
        {
            return await Upload(name, request.Body);
        }

        return new APIGatewayProxyResponse()
        {
            StatusCode = (int)HttpStatusCode.MethodNotAllowed
        };
    }

    private static async Task<APIGatewayProxyResponse> Download(string name)
    {
        var downloadRequest = new GetObjectRequest
        {
            BucketName = BucketName,
            Key = name
        };

        var memeResponse = await Client.GetObjectAsync(downloadRequest);

        using var ms = new MemoryStream();
        await memeResponse.ResponseStream.CopyToAsync(ms);
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = Convert.ToBase64String(ms.ToArray()),
            IsBase64Encoded = true,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "image/png" }
        };
    }


    private static async Task<APIGatewayProxyResponse> Upload(string name, string content)
    {
        using var memoryStream = new MemoryStream(Convert.FromBase64String(content));

        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = name,
            InputStream = memoryStream
        };

        await Client.PutObjectAsync(request);

        return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.OK };
    }
}
