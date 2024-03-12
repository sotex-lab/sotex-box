using System.Text;
using Amazon.S3;
using model.Contracts;
using model.Core;
using Newtonsoft.Json;
using persistence.Repository;
using Shouldly;

namespace IntegrationTests.ControllerTests;

[Collection(ConfigurableBackendFactory.IntegrationCollection)]
public class AdsControllerTests
{
    private readonly ConfigurableBackendFactory _factory;
    private readonly IAdRepository _adRepository;
    private readonly ITagRepository _tagRepository;
    private readonly HttpClient httpClient;
    private readonly IAmazonS3 _s3Client;

    public AdsControllerTests(ConfigurableBackendFactory factory)
    {
        _factory = factory;
        httpClient = _factory.CreateClient();
        var serviceProvider = _factory.Services.CreateScope().ServiceProvider;
        _adRepository = serviceProvider.GetService<IAdRepository>()!;
        _tagRepository = serviceProvider.GetService<ITagRepository>()!;
        _s3Client = serviceProvider.GetService<IAmazonS3>()!;

        _factory.ResetStorages();
    }

    [Fact]
    public async Task Should_Post()
    {
        var contract = new AdContract { Scope = AdScope.Global, Tags = ["test", "tags"] };

        var content = new StringContent(
            JsonConvert.SerializeObject(contract),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/ads", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);

        var responseContent = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            await response.Content.ReadAsStringAsync()
        )!;

        responseContent.ShouldContainKey("id");
        responseContent.ShouldContainKey("presigned");

        (await _adRepository.Fetch().CountAsync()).ShouldBe(1);
        (await _tagRepository.Fetch().CountAsync()).ShouldBe(2);

        var bucketsResponse = await _s3Client.ListBucketsAsync();
        bucketsResponse.Buckets.Count.ShouldBe(1);

        bucketsResponse.Buckets.First().BucketName.ShouldBe("non-processed");
    }

    [Fact]
    public async Task Should_Post_NoNewTags()
    {
        await _tagRepository.Add(new Tag { Id = Guid.NewGuid(), Name = "test" });

        var contract = new AdContract { Scope = AdScope.Global, Tags = ["test"] };

        var content = new StringContent(
            JsonConvert.SerializeObject(contract),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/ads", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);

        var responseContent = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            await response.Content.ReadAsStringAsync()
        )!;

        (await _adRepository.Fetch().CountAsync()).ShouldBe(1);
        (await _tagRepository.Fetch().CountAsync()).ShouldBe(1);

        var bucketsResponse = await _s3Client.ListBucketsAsync();
        bucketsResponse.Buckets.Count.ShouldBe(1);

        bucketsResponse.Buckets.First().BucketName.ShouldBe("non-processed");
    }
}
