using Amazon.S3;
using Amazon.S3.Model;
using DotNext;
using model.Contracts;
using Newtonsoft.Json;

namespace backend.Services.Aws;

public interface IGetObjectService
{
    Task<Result<ScheduleContract, GetObjectError>> GetObjectByKey(S3Bucket bucket, string key);
}

public enum GetObjectError
{
    General = 1,
    KeyNotFound,
    BadFormat
}

public class GetObjectService : IGetObjectService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<GetObjectService> _logger;

    public GetObjectService(ILogger<GetObjectService> logger, IAmazonS3 s3)
    {
        _s3Client = s3;
        _logger = logger;
    }

    public async Task<Result<ScheduleContract, GetObjectError>> GetObjectByKey(
        S3Bucket bucket,
        string key
    )
    {
        var response = await _s3Client.GetObjectAsync(bucket.BucketName, key);
        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            _logger.LogWarning("Unexpected status code: {0}", response.HttpStatusCode);
            return new Result<ScheduleContract, GetObjectError>(GetObjectError.KeyNotFound);
        }

        using var stream = new StreamReader(response.ResponseStream);
        var contents = await stream.ReadToEndAsync();
        try
        {
            var deserialized = JsonConvert.DeserializeObject<ScheduleContract>(contents);
            return new Result<ScheduleContract, GetObjectError>(deserialized);
        }
        catch (Exception e)
        {
            _logger.LogError("Couldn't deserialize into expected contract: {0}", e.Message);
            return new Result<ScheduleContract, GetObjectError>(GetObjectError.BadFormat);
        }
    }
}
