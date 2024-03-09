using System.Reflection.Metadata.Ecma335;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using model.Contracts;
using model.Core;
using persistence.Repository;
using persistence.Repository.Base;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]/[action]")]
public class AdsController(IAdRepository adRepository, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public IAsyncEnumerable<Ad> Get() => adRepository.Fetch();

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var maybeResource = await adRepository.GetSingle(id);

        return maybeResource.IsSuccessful
            ? Ok(maybeResource.Value)
            : BadRequest(maybeResource.Error.Stringify());
    }

    [HttpPost]
    public async Task<IActionResult> Post(AdContract contract)
    {
        var mapped = mapper.Map<Ad>(contract);
        if (mapped == null)
            return BadRequest();

        var maybeAd = await adRepository.Add(mapped);

        return maybeAd.IsSuccessful ? Created() : BadRequest(maybeAd.Error.Stringify());
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
