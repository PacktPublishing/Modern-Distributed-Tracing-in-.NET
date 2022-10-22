using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace memefunc
{
    public class Storage
    {
        [FunctionName("storage-upload")]
        public async Task Upload([HttpTrigger(AuthorizationLevel.Function, "put", Route = "memes/{name}")] HttpRequest request,
            [Blob("memes/{name}", FileAccess.Write)] Stream output)
        {
            await request.Body.CopyToAsync(output);
        }

        [FunctionName("storage-download")]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "memes/{name}")] HttpRequest request,
            [Blob("memes/{name}", FileAccess.Read)] Stream input)
        {
            var response = new MemoryStream();
            await input.CopyToAsync(response);
            response.Position = 0;
            return new FileStreamResult(response, "image/png");
        }
    }
}
