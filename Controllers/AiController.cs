using DotNet8WebAPI.Services.AI;
using Microsoft.AspNetCore.Mvc;

namespace DotNet8WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] string message)
    {
        var response = await _aiService.AskAsync(message);

        return Ok(new
        {
            response
        });
    }
}