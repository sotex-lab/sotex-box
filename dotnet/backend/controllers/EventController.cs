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
    public async Task Connect(string id, CancellationToken token)
    {
        var result = _eventCoordinator.Add(id, HttpContext.Response.Body);

        if (!result.IsSuccessful)
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await HttpContext.Response.WriteAsync(result.Error.Stringify());
            return;
        }

        var ourToken = result.Value;
        HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");

        while (!ourToken.IsCancellationRequested && !token.IsCancellationRequested) { }

        _eventCoordinator.Remove(id);
    }

    [HttpDelete]
    public IActionResult ForceDisconnect(string id)
    {
        var result = _eventCoordinator.Remove(id);
        return result.IsSuccessful ? Ok("removed\n") : BadRequest(result.Error.Stringify());
    }

    [HttpGet]
    public async Task<IActionResult> WriteData(string id, string message)
    {
        var result = await _eventCoordinator.SendMessage(id, message);
        return result.IsSuccessful ? Ok("sent\n") : BadRequest(result.Error.Stringify());
    }
}
