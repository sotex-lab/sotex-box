using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using model.Core;
using persistence.Repository;
using persistence.Repository.Base;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]")]
public class DevicesController(IDeviceRepository deviceRepository, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async IAsyncEnumerable<DeviceContract> Get()
    {
        await foreach (var item in deviceRepository.Fetch())
            yield return mapper.Map<DeviceContract>(item);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token)
    {
        var maybeResource = await deviceRepository.GetSingle(id, token);

        return maybeResource.IsSuccessful
            ? Ok(mapper.Map<DeviceContract>(maybeResource.Value))
            : BadRequest(maybeResource.Error.Stringify());
    }

    [HttpPost]
    public async Task<IActionResult> Post(DeviceContract contract, CancellationToken token)
    {
        var mapped = mapper.Map<Device>(contract);
        if (mapped == null)
            return BadRequest();

        var maybeDevice = await deviceRepository.GetSingle(
            x => mapped.UtilityName!.Equals(x.UtilityName),
            token
        );

        if (maybeDevice.IsSuccessful)
            return BadRequest(RepositoryError.Duplicate.Stringify());

        maybeDevice = await deviceRepository.Add(mapped, token);

        return maybeDevice.IsSuccessful
            ? CreatedAtAction(nameof(Post), mapper.Map<DeviceContract>(maybeDevice.Value))
            : BadRequest(maybeDevice.Error.Stringify());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid id, DeviceContract contract, CancellationToken token)
    {
        if (contract.Ip == null)
            return BadRequest(RepositoryError.ArgumentNull.ToString());

        var maybeDevice = await deviceRepository.GetSingle(id, token);
        if (maybeDevice.IsSuccessful)
            return BadRequest(maybeDevice.Error.Stringify());

        var device = maybeDevice.Value;
        device.Ip = contract.Ip;

        maybeDevice = await deviceRepository.Update(device, token);
        return maybeDevice.IsSuccessful
            ? Ok(mapper.Map<DeviceContract>(maybeDevice.Value))
            : BadRequest(maybeDevice.Error.ToString());
    }
}
