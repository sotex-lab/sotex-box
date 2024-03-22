using System.Net;
using Microsoft.AspNetCore.Mvc;
using SseHandler;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]/[action]")]
public class EventController : ControllerBase
{
    private readonly IEventCoordinator _eventCoordinator;

    public EventController(IEventCoordinator eventCoordinator)
    {
        _eventCoordinator = eventCoordinator;
    }

    [HttpGet]
    public async Task Connect(Guid id, CancellationToken token)
    {
        var result = _eventCoordinator.Add(id, HttpContext.Response.Body);

        if (!result.IsSuccessful)
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await HttpContext.Response.WriteAsync(result.Error.Stringify());
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

        _eventCoordinator.Remove(id);
    }

    [HttpDelete]
    public IActionResult ForceDisconnect(Guid id)
    {
        var result = _eventCoordinator.Remove(id);
        return result.IsSuccessful ? Ok("removed\n") : BadRequest(result.Error.Stringify());
    }

    [HttpGet]
    public async Task<IActionResult> WriteData(Guid id, string message)
    {
        var result = await _eventCoordinator.SendMessage(id, message);
        return result.IsSuccessful ? Ok("sent\n") : BadRequest(result.Error.Stringify());
    }
}
