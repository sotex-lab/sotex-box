using System.Text;
using model.Contracts;
using model.Core;
using Newtonsoft.Json;
using Shouldly;

namespace e2e_tester.AdTests;

public class CreateAdTest : E2ETest
{
    public CreateAdTest(E2ECtx c)
        : base(c) { }

    protected override string Description() =>
        "Create a new ad and fully process it with a background job";

    protected override string Name() => "Create ad";

    protected override async Task Run(CancellationToken token)
    {
        var client = GetClient();

        var contract = new AdContract { Scope = AdScope.Global, Tags = ["test", "tags"] };
        var content = new StringContent(
            JsonConvert.SerializeObject(contract),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync("/ads", content, token);

        response.IsSuccessStatusCode.ShouldBeTrue();

        var responseContent = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            await response.Content.ReadAsStringAsync()
        )!;

        responseContent.ShouldContainKey("id");
        responseContent.ShouldContainKey("presigned");

        var adPath = Path.Combine(ResourcesDir(), "test-add.png");

        var request = new HttpRequestMessage(HttpMethod.Put, responseContent["presigned"])
        {
            Content = new ByteArrayContent(await File.ReadAllBytesAsync(adPath, token))
        };
        request.Content.Headers.Add("Content-Type", "image/png");

        var putResponse = await client.SendAsync(request, token);

        putResponse.IsSuccessStatusCode.ShouldBeTrue();

        await Task.Delay(DefaultJobInterval(), token);

        var adRepo = GetRepository<Ad, Guid>();

        var maybeAd = await adRepo.GetSingle(new Guid(responseContent["id"]));
        maybeAd.IsSuccessful.ShouldBeTrue();

        maybeAd.Value.ObjectId.ShouldNotBeNullOrEmpty();
    }
}
