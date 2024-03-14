using Amazon.S3.Util;
using AutoMapper;
using backend.Services.Aws;
using Microsoft.AspNetCore.Mvc;
using model.Contracts;
using model.Core;
using Newtonsoft.Json;
using persistence.Repository;
using persistence.Repository.Base;
using Tag = model.Core.Tag;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]")]
public class AdsController(
    IAdRepository adRepository,
    ITagRepository tagRepository,
    IMapper mapper,
    IGetOrCreateBucketService getOrCreateBucketService,
    IPreSignObjectService preSignObjectService,
    ILogger<AdsController> logger
) : ControllerBase
{
    [HttpGet]
    public async IAsyncEnumerable<AdContract> Get()
    {
        await foreach (var item in adRepository.Fetch())
        {
            yield return mapper.Map<AdContract>(item);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var maybeResource = await adRepository.GetSingle(id);

        return maybeResource.IsSuccessful
            ? Ok(mapper.Map<AdContract>(maybeResource.Value))
            : BadRequest(maybeResource.Error.Stringify());
    }

    [HttpPost]
    public async Task<IActionResult> Post(AdContract contract)
    {
        var mapped = mapper.Map<Ad>(contract);
        if (mapped == null)
            return BadRequest();

        var foundTags = new List<Tag>();
        await foreach (var tag in tagRepository.FindByNames(contract.Tags))
        {
            foundTags.Add(tag);
        }
        var foundTagNames = foundTags.Select(x => x.Name);
        foreach (var tag in contract.Tags)
        {
            if (foundTagNames.Contains(tag))
                continue;
            foundTags.Add(new Tag { Name = tag.Trim().ToLower() });
        }

        mapped.Tags = foundTags;

        var maybeAd = await adRepository.Add(mapped);
        if (!maybeAd.IsSuccessful)
            BadRequest(maybeAd.Error.Stringify());

        var ad = maybeAd.Value;

        var bucketResponse = await getOrCreateBucketService.GetNonProcessed();
        if (!bucketResponse.IsSuccessful)
            return await RemoveAdAndReturn(ad, "bucket");

        var presignedResponse = await preSignObjectService.Put(
            bucketResponse.Value,
            ad.Id.ToString()
        );
        if (!presignedResponse.IsSuccessful)
            return await RemoveAdAndReturn(ad, "presigning");

        return maybeAd.IsSuccessful
            ? CreatedAtAction(
                nameof(Post),
                new { id = maybeAd.Value.Id },
                new { id = maybeAd.Value.Id, presigned = presignedResponse.Value }
            )
            : await RemoveAdAndReturn(ad, "update");
    }

    private async Task<IActionResult> RemoveAdAndReturn(Ad ad, string stage)
    {
        logger.LogWarning("Error while creating an object in blob storage in stage {0}", stage);

        var result = await adRepository.Delete(ad);
        if (!result.IsSuccessful)
            return BadRequest("Couldn't rollback ad creation. Contact admin");

        return BadRequest("Internal problem, contact admin");
    }

    [HttpPost("callback")]
    public IActionResult Callback([FromBody] string data)
    {
        logger.LogInformation("Callback hit with data: {0}", data);

        return Ok();
    }
}
