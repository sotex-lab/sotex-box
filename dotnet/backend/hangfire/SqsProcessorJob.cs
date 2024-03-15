using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using persistence.Repository;
using persistence.Repository.Base;

namespace backend.Hangfire;

public class SqsProcessorJob : GenericCronJob<SqsProcessorJob>, IGenericCronJob
{
    public static string EnvironmentVariableName => "SQS_INTERVAL";
    private readonly IAmazonSQS _sqsClient;
    private readonly IAdRepository _adRepository;

    public SqsProcessorJob(
        ILogger<SqsProcessorJob> logger,
        IAmazonSQS sqsClient,
        IAdRepository adRepository
    )
        : base(logger)
    {
        _sqsClient = sqsClient;
        _adRepository = adRepository;
    }

    public override async Task Run()
    {
        _logger.LogDebug("Polling sqs for new events");

        var request = new ReceiveMessageRequest()
        {
            QueueUrl = Environment.GetEnvironmentVariable("AWS_SQS_NONPROCESSED_QUEUE_URL")!,
            MaxNumberOfMessages = 5,
            WaitTimeSeconds = 2,
        };

        var response = await _sqsClient.ReceiveMessageAsync(request);
        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            _logger.LogDebug("Received an unexpected status code: {0}", response.HttpStatusCode);
            return;
        }

        foreach (var message in response.Messages)
        {
            Message mapped;
            try
            {
                mapped = JsonConvert.DeserializeObject<Message>(message.Body);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Couldn't map body to object 'Message', error: {0}, body: {1}",
                    e.Message,
                    message.Body
                );
                continue;
            }

            foreach (var record in mapped.Records!)
            {
                var key = record.S3!.Object!.Key!;
                var eTag = record.S3.Object.ETag!;
                _logger.LogDebug(
                    "Received record in bucket '{0}' and for key '{1}'",
                    record.S3!.Bucket!.Name,
                    key
                );

                var maybeAd = await _adRepository.GetSingle(new Guid(key));
                if (!maybeAd.IsSuccessful)
                {
                    _logger.LogWarning("Couldn't find ad with id '{0}', skipping", key);
                    continue;
                }

                var ad = maybeAd.Value;
                ad.ObjectId = eTag;

                var updateResult = await _adRepository.Update(ad);
                if (!updateResult.IsSuccessful)
                {
                    _logger.LogError(
                        "Failed to update ad with id '{0}', due to reasons: '{1}'",
                        key,
                        updateResult.Error.Stringify()
                    );
                }
            }
        }

        _logger.LogDebug("Polling sqs finished");
    }
}

internal class Message
{
    [JsonProperty("Records")]
    public List<Record>? Records { get; set; }
}

internal class Record
{
    [JsonProperty("s3")]
    public S3? S3 { get; set; }
}

internal class S3
{
    [JsonProperty("bucket")]
    public Bucket? Bucket { get; set; }

    [JsonProperty("object")]
    public S3Object? Object { get; set; }
}

internal class Bucket
{
    [JsonProperty("name")]
    public string? Name { get; set; }
}

internal class S3Object
{
    [JsonProperty("key")]
    public string? Key { get; set; }

    [JsonProperty("size")]
    public string? Size { get; set; }

    [JsonProperty("eTag")]
    public string? ETag { get; set; }

    [JsonProperty("contentType")]
    public string? ContentType { get; set; }
}
