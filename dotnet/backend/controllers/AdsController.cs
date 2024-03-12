using AutoMapper;
using backend.Services.Aws;
using Microsoft.AspNetCore.Mvc;
using model.Contracts;
using model.Core;
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
    IPutObjectService putObjectService,
    IPreSignObjectService preSignObjectService
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
            return BadRequest("Internal problem, contact admin");

        var objectResponse = await putObjectService.PutEmpty(
            bucketResponse.Value,
            ad.Id.ToString()
        );
        if (!objectResponse.IsSuccessful)
            return BadRequest("Internal problem, contact admin");

        ad.ObjectId = objectResponse.Value;
        maybeAd = await adRepository.Update(ad);

        var presignedResponse = await preSignObjectService.Put(
            bucketResponse.Value,
            ad.Id.ToString()
        );
        if (!objectResponse.IsSuccessful)
            return BadRequest("Internal problem, contact admin");

        return maybeAd.IsSuccessful
            ? CreatedAtAction(
                nameof(Post),
                new { id = maybeAd.Value.Id },
                new { id = maybeAd.Value.Id, presigned = presignedResponse.Value }
            )
            : BadRequest(maybeAd.Error.Stringify());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var maybeResource = await adRepository.GetSingle(id);
        if (!maybeResource.IsSuccessful)
            return BadRequest(maybeResource.Error.Stringify());

        var result = await adRepository.Delete(maybeResource.Value);
        return result.IsSuccessful ? Ok() : BadRequest(result.Error.Stringify());
    }
}
