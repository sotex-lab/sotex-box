using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using DotNext;

namespace backend.Services.Aws;

public interface IGetOrCreateBucketService
{
    Task<Result<S3Bucket, GetOrCreateBucketError>> GetProcessed();
    Task<Result<S3Bucket, GetOrCreateBucketError>> GetNonProcessed();
    Task<Result<S3Bucket, GetOrCreateBucketError>> GetSchedule();
}

public enum GetOrCreateBucketError
{
    Unknown = 1,
    FailedToCreateBucket,
    FailedToListBuckets,
    NotFound,
}

public class GetOrCreateBucketServiceImpl : IGetOrCreateBucketService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<GetOrCreateBucketServiceImpl> _logger;

    private static string nonProcessed = Environment.GetEnvironmentVariable(
        "NON_PROCESSED_BUCKET_NAME"
    )!;
#pragma warning disable CS0414 // Add readonly modifier
    private static string processed = Environment.GetEnvironmentVariable("PROCESSED_BUCKET_NAME")!;
#pragma warning restore CS0414 // Add readonly modifier
    private static string schedule = Environment.GetEnvironmentVariable("SCHEDULE_BUCKET_NAME")!;

    public GetOrCreateBucketServiceImpl(IAmazonS3 s3, ILogger<GetOrCreateBucketServiceImpl> logger)
    {
        _s3Client = s3;
        _logger = logger;
    }

    //TODO: should be changed to processed once we implement the way of transfering from
    //      one bucket to another. Also should remove the queting of warning above
    public async Task<Result<S3Bucket, GetOrCreateBucketError>> GetProcessed() =>
        await EnsureCreated(nonProcessed);

    public async Task<Result<S3Bucket, GetOrCreateBucketError>> GetNonProcessed() =>
        await EnsureCreated(nonProcessed);

    public async Task<Result<S3Bucket, GetOrCreateBucketError>> GetSchedule() =>
        await EnsureCreated(schedule);

    private async Task<Result<S3Bucket, GetOrCreateBucketError>> EnsureCreated(string bucketName)
    {
        try
        {
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName))
            {
                var putBucketRequest = new PutBucketRequest { BucketName = bucketName };

                var bucketResponse = await _s3Client.PutBucketAsync(putBucketRequest);
                if (
                    bucketResponse.HttpStatusCode != System.Net.HttpStatusCode.OK
                    && bucketResponse.HttpStatusCode != System.Net.HttpStatusCode.Created
                )
                {
                    _logger.LogWarning(
                        "Unexpected status code: {0}",
                        bucketResponse.HttpStatusCode
                    );
                    return new Result<S3Bucket, GetOrCreateBucketError>(
                        GetOrCreateBucketError.FailedToCreateBucket
                    );
                }
            }

            var bucketsResponse = await _s3Client.ListBucketsAsync();
            if (bucketsResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogWarning("Unexpected status code: {0}", bucketsResponse.HttpStatusCode);
                return new Result<S3Bucket, GetOrCreateBucketError>(
                    GetOrCreateBucketError.FailedToListBuckets
                );
            }

            var bucket = bucketsResponse.Buckets.FirstOrDefault(x => x.BucketName == bucketName);

            return bucket == null
                ? new Result<S3Bucket, GetOrCreateBucketError>(GetOrCreateBucketError.NotFound)
                : new Result<S3Bucket, GetOrCreateBucketError>(bucket);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return new Result<S3Bucket, GetOrCreateBucketError>(GetOrCreateBucketError.Unknown);
        }
    }
}
