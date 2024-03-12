using Amazon.S3;
using Amazon.S3.Model;
using DotNext;

namespace backend.Services.Aws;

public interface IPutObjectService
{
    Task<Result<string, PutObjectError>> Put(S3Bucket bucket, string key, Stream stream);
    Task<Result<string, PutObjectError>> PutEmpty(S3Bucket bucket, string key);
}

public enum PutObjectError
{
    Unknown,
    FailedToCreateObject,
}

public class PutObjectServiceImpl : IPutObjectService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<PutObjectServiceImpl> _logger;

    public PutObjectServiceImpl(IAmazonS3 s3, ILogger<PutObjectServiceImpl> logger)
    {
        _s3Client = s3;
        _logger = logger;
    }

    public async Task<Result<string, PutObjectError>> Put(
        S3Bucket bucket,
        string key,
        Stream stream
    )
    {
        var putObjectRequest = new PutObjectRequest
        {
            BucketName = bucket.BucketName,
            Key = key,
            InputStream = stream
        };

        try
        {
            var response = await _s3Client.PutObjectAsync(putObjectRequest);

            if (
                response.HttpStatusCode != System.Net.HttpStatusCode.OK
                && response.HttpStatusCode != System.Net.HttpStatusCode.Created
            )
            {
                _logger.LogWarning("Unexpected status code: {0}", response.HttpStatusCode);
                return new Result<string, PutObjectError>(PutObjectError.FailedToCreateObject);
            }

            return new Result<string, PutObjectError>(response.ETag);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return new Result<string, PutObjectError>(PutObjectError.Unknown);
        }
    }

    public async Task<Result<string, PutObjectError>> PutEmpty(S3Bucket bucket, string key) =>
        await Put(bucket, key, new MemoryStream());
}
