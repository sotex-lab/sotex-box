using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using AutoMapper;
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
    IAmazonS3 s3,
    IMapper mapper
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

        var preprocessedBucket = "non-processed";
        try
        {
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, preprocessedBucket))
            {
                var putBucketRequest = new PutBucketRequest { BucketName = preprocessedBucket };

                var bucketResponse = await s3.PutBucketAsync(putBucketRequest);
                if (
                    bucketResponse.HttpStatusCode != System.Net.HttpStatusCode.OK
                    && bucketResponse.HttpStatusCode != System.Net.HttpStatusCode.Created
                )
                {
                    return BadRequest(
                        string.Format("Couldn't create bucket: {0}", bucketResponse.HttpStatusCode)
                    );
                }
            }
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        var putObjectRequest = new PutObjectRequest
        {
            BucketName = preprocessedBucket,
            Key = ad.Id.ToString(),
            InputStream = new MemoryStream()
        };

        var response = await s3.PutObjectAsync(putObjectRequest);

        if (
            response.HttpStatusCode != System.Net.HttpStatusCode.OK
            && response.HttpStatusCode != System.Net.HttpStatusCode.Created
        )
        {
            return BadRequest(
                string.Format("Received status code from s3: {}", response.HttpStatusCode)
            );
        }

        ad.ObjectId = response.ETag;
        maybeAd = await adRepository.Update(ad);

        var request = new GetPreSignedUrlRequest()
        {
            BucketName = preprocessedBucket,
            Key = ad.ObjectId,
            Expires = DateTime.UtcNow.AddMinutes(30),
            Protocol = Environment.GetEnvironmentVariable("AWS_PROTOCOL")! switch
            {
                "http" => Protocol.HTTP,
                "https" => Protocol.HTTPS,
                _ => throw new Exception("Unsupported protocol")
            },
        };
        var presigned = await s3.GetPreSignedURLAsync(request);

        return maybeAd.IsSuccessful
            ? CreatedAtAction(
                nameof(Post),
                new { id = maybeAd.Value.Id },
                new { id = maybeAd.Value.Id, presigned }
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
