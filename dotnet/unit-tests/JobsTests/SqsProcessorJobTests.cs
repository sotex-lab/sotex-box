using System.Text.Json.Nodes;
using Amazon.SQS;
using Amazon.SQS.Model;
using backend.Hangfire;
using DotNext;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using model.Core;
using Moq;
using Newtonsoft.Json;
using persistence.Repository;
using persistence.Repository.Base;
using Shouldly;

namespace unit_tests;

[TestClass]
public class SqsProcessorJobTests
{
    private void SetEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("AWS_SQS_NONPROCESSED_QUEUE_URL", "/0/queue");
    }

    [TestMethod]
    public async Task Should_HandleBadStatusCode()
    {
        var logger = new Mock<ILogger<SqsProcessorJob>>();

        var sqsClient = new Mock<IAmazonSQS>();
        sqsClient
            .Setup(x =>
                x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), new CancellationToken())
            )
            .ReturnsAsync(
                () =>
                    new ReceiveMessageResponse
                    {
                        HttpStatusCode = System.Net.HttpStatusCode.NoContent
                    }
            );

        var adRepo = new Mock<IAdRepository>();

        SetEnvironmentVariable();

        var job = new SqsProcessorJob(logger.Object, sqsClient.Object, adRepo.Object);

        // Should work without exceptions
        await job.Run().ShouldNotThrowAsync();
    }

    [TestMethod]
    public async Task Should_WorkIfMessagesAreEmpty()
    {
        var logger = new Mock<ILogger<SqsProcessorJob>>();

        var sqsClient = new Mock<IAmazonSQS>();
        sqsClient
            .Setup(x =>
                x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), new CancellationToken())
            )
            .ReturnsAsync(
                () =>
                    new ReceiveMessageResponse
                    {
                        HttpStatusCode = System.Net.HttpStatusCode.OK,
                        Messages = new List<Amazon.SQS.Model.Message>(),
                    }
            );

        var adRepo = new Mock<IAdRepository>();

        SetEnvironmentVariable();

        var job = new SqsProcessorJob(logger.Object, sqsClient.Object, adRepo.Object);

        await job.Run().ShouldNotThrowAsync();
    }

    [TestMethod]
    public async Task Should_WorkIfMessagesAreBadlyFormatted()
    {
        var logger = new Mock<ILogger<SqsProcessorJob>>();

        var sqsClient = new Mock<IAmazonSQS>();
        sqsClient
            .Setup(x =>
                x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), new CancellationToken())
            )
            .ReturnsAsync(
                () =>
                    new ReceiveMessageResponse
                    {
                        HttpStatusCode = System.Net.HttpStatusCode.OK,
                        Messages = new List<Amazon.SQS.Model.Message>
                        {
                            new Amazon.SQS.Model.Message { Body = "Random body" }
                        },
                    }
            );

        var adRepo = new Mock<IAdRepository>();

        SetEnvironmentVariable();

        var job = new SqsProcessorJob(logger.Object, sqsClient.Object, adRepo.Object);

        await job.Run().ShouldNotThrowAsync();
    }

    [TestMethod]
    public async Task Should_WorkForNoRecords()
    {
        var logger = new Mock<ILogger<SqsProcessorJob>>();

        var sqsClient = new Mock<IAmazonSQS>();
        sqsClient
            .Setup(x =>
                x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), new CancellationToken())
            )
            .ReturnsAsync(
                () =>
                    new ReceiveMessageResponse
                    {
                        HttpStatusCode = System.Net.HttpStatusCode.OK,
                        Messages = new List<Amazon.SQS.Model.Message>
                        {
                            new Amazon.SQS.Model.Message
                            {
                                Body = JsonConvert.SerializeObject(
                                    new backend.Hangfire.Message { Records = new List<Record>() }
                                )
                            }
                        },
                    }
            );

        var adRepo = new Mock<IAdRepository>();

        SetEnvironmentVariable();

        var job = new SqsProcessorJob(logger.Object, sqsClient.Object, adRepo.Object);

        await job.Run().ShouldNotThrowAsync();
    }

    [TestMethod]
    public async Task Should_WorkWithRecords()
    {
        var logger = new Mock<ILogger<SqsProcessorJob>>();

        var key = Guid.NewGuid();
        var ad = new Ad
        {
            AdScope = AdScope.Global,
            Id = key,
            ObjectId = "",
            Tags = new List<Tag>()
        };

        var sqsClient = new Mock<IAmazonSQS>();
        sqsClient
            .Setup(x =>
                x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), new CancellationToken())
            )
            .ReturnsAsync(
                () =>
                    new ReceiveMessageResponse
                    {
                        HttpStatusCode = System.Net.HttpStatusCode.OK,
                        Messages = new List<Amazon.SQS.Model.Message>
                        {
                            new Amazon.SQS.Model.Message
                            {
                                Body = JsonConvert.SerializeObject(
                                    new backend.Hangfire.Message
                                    {
                                        Records = new List<Record>
                                        {
                                            new Record
                                            {
                                                S3 = new S3
                                                {
                                                    Bucket = new Bucket { Name = "test" },
                                                    Object = new S3Object
                                                    {
                                                        ContentType = "unknown",
                                                        ETag = "new tag",
                                                        Key = key.ToString(),
                                                        Size = "123123123"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                )
                            }
                        },
                    }
            );

        var adRepo = new Mock<IAdRepository>();
        adRepo
            .Setup(x => x.GetSingle(It.IsAny<Guid>(), default))
            .ReturnsAsync(() => new Result<Ad, RepositoryError>(ad));
        adRepo
            .Setup(x => x.Update(It.IsAny<Ad>(), default))
            .ReturnsAsync(() => new Result<Ad, RepositoryError>(ad));

        SetEnvironmentVariable();

        var job = new SqsProcessorJob(logger.Object, sqsClient.Object, adRepo.Object);

        await job.Run().ShouldNotThrowAsync();
        ad.ObjectId.ShouldBe("new tag");
    }
}
