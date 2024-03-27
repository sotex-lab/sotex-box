using System.Net;
using Microsoft.AspNetCore.Mvc;
using persistence.Repository;
using persistence.Repository.Base;
using SseHandler;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]/[action]")]
public class EventController(IEventCoordinator eventCoordinator, IDeviceRepository deviceRepository)
    : ControllerBase
{
    [HttpGet]
    public async Task Connect(Guid id, CancellationToken token)
    {
        if (
            Environment.GetEnvironmentVariable("REQUIRE_KNOWN_DEVICES")!.Equals("true")
            && !await CheckIfDeviceExistsAndUpdateIp(id, token)
        )
            return;

        var result = eventCoordinator.Add(id, HttpContext.Response.Body);

        if (!result.IsSuccessful)
        {
            await ErrorOut(result.Error.Stringify(), HttpStatusCode.BadRequest);
            return;
        }

        var ourToken = result.Value.Token;
        HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");
        HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;

        var semaphore = new SemaphoreSlim(0);
        token.Register(() => semaphore.Release());
        ourToken.Register(() => semaphore.Release());
        while (!ourToken.IsCancellationRequested && !token.IsCancellationRequested)
        {
            await semaphore.WaitAsync();
        }

        eventCoordinator.Remove(id);
    }

    [HttpDelete]
    public IActionResult ForceDisconnect(Guid id)
    {
        var result = eventCoordinator.Remove(id);
        return result.IsSuccessful ? Ok("removed\n") : BadRequest(result.Error.Stringify());
    }

    [HttpGet]
    public async Task<IActionResult> WriteData(Guid id, string message)
    {
        var result = await eventCoordinator.SendMessage(id, message);
        return result.IsSuccessful ? Ok("sent\n") : BadRequest(result.Error.Stringify());
    }

    private async Task ErrorOut(string message, HttpStatusCode statusCode)
    {
        HttpContext.Response.StatusCode = (int)statusCode;
        await HttpContext.Response.WriteAsync(message);
    }

    private async Task<bool> CheckIfDeviceExistsAndUpdateIp(Guid id, CancellationToken token)
    {
        var maybeDevice = await deviceRepository.GetSingle(id, token);

        if (!maybeDevice.IsSuccessful)
        {
            await ErrorOut(RepositoryError.NotFound.Stringify(), HttpStatusCode.Forbidden);
            return false;
        }

        var ip = HttpContext.Connection.RemoteIpAddress;
        var device = maybeDevice.Value;
        device.Ip = ip;

        maybeDevice = await deviceRepository.Update(device, token);
        if (!maybeDevice.IsSuccessful)
        {
            await ErrorOut(RepositoryError.General.Stringify(), HttpStatusCode.BadRequest);
            return false;
        }

        return true;
    }
}
