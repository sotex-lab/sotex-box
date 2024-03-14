using Amazon.S3;
using Amazon.S3.Model;
using DotNext;

namespace backend.Services.Aws;

public interface IPreSignObjectService
{
    Task<Result<string, PreSignErr>> Put(S3Bucket bucket, string key, DateTime? expires = null);
    Task<Result<string, PreSignErr>> Get(S3Bucket bucket, string key, DateTime? expires = null);
}

public enum PreSignErr
{
    Unknown,
    Failed
}

public class PreSignObjectServiceImpl : IPreSignObjectService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<PreSignObjectServiceImpl> _logger;
    private readonly string _protocol;

    public PreSignObjectServiceImpl(
        IAmazonS3 s3,
        ILogger<PreSignObjectServiceImpl> logger,
        string protocol
    )
    {
        _s3Client = s3;
        _logger = logger;
        _protocol = protocol;
    }

    public async Task<Result<string, PreSignErr>> Put(
        S3Bucket bucket,
        string key,
        DateTime? expires = null
    ) => await SignUrl(bucket, key, HttpVerb.PUT, expires);

    public async Task<Result<string, PreSignErr>> Get(
        S3Bucket bucket,
        string key,
        DateTime? expires = null
    ) => await SignUrl(bucket, key, HttpVerb.GET, expires);

    private async Task<Result<string, PreSignErr>> SignUrl(
        S3Bucket bucket,
        string key,
        HttpVerb verb,
        DateTime? expires = null
    )
    {
        var request = new GetPreSignedUrlRequest()
        {
            BucketName = bucket.BucketName,
            Key = key,
            Expires = expires ?? DateTime.UtcNow.AddMinutes(30),
            Protocol = _protocol switch
            {
                "http" => Protocol.HTTP,
                "https" => Protocol.HTTPS,
                _ => throw new Exception("Unsupported protocol")
            },
            Verb = verb,
        };

        try
        {
            var presigned = await _s3Client.GetPreSignedURLAsync(request);
            return presigned == null
                ? new Result<string, PreSignErr>(PreSignErr.Failed)
                : new Result<string, PreSignErr>(presigned);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return new Result<string, PreSignErr>(PreSignErr.Unknown);
        }
    }
}
