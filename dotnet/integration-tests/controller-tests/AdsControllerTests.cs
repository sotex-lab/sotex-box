using System.Text;
using model.Contracts;
using model.Core;
using Newtonsoft.Json;
using persistence.Repository;
using Shouldly;

namespace IntegrationTests.ControllerTests;

public class AdsControllerTests : IClassFixture<ConfigurableBackendFactory<Program>>
{
    private readonly ConfigurableBackendFactory<Program> _factory;
    private readonly IAdRepository _adRepository;
    private readonly ITagRepository _tagRepository;
    private readonly HttpClient httpClient;

    public AdsControllerTests(ConfigurableBackendFactory<Program> factory)
    {
        _factory = factory;
        httpClient = _factory.CreateClient();
        var serviceProvider = _factory.Services.CreateScope().ServiceProvider;
        _adRepository = serviceProvider.GetService<IAdRepository>()!;
        _tagRepository = serviceProvider.GetService<ITagRepository>()!;
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

        (await _adRepository.Fetch().CountAsync()).ShouldBe(1);
        (await _tagRepository.Fetch().CountAsync()).ShouldBe(2);
    }
}
