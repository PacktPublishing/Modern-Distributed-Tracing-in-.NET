﻿using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace storage;

public class S3Storage : IStorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly AwsS3Options _s3Options;
    public S3Storage(IOptions<CloudStorageOptions> options)
    {
        _s3Options = options.Value.AwsS3 ?? throw new ArgumentNullException(nameof(options.Value.AwsS3));
        _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(_s3Options.BucketRegion));
    }

    public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
    {
        var request = new GetObjectRequest
        {
            BucketName = _s3Options.BucketName,
            Key = name
        };

        var response = await _s3Client.GetObjectAsync(request, cancellationToken);
        return response.ResponseStream;
    }

    public async Task WriteAsync(string name, Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var request = new PutObjectRequest
        {
            BucketName = _s3Options.BucketName,
            Key = name,
            InputStream = memoryStream
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }
}
