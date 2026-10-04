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
    public async Task<IActionResult> Ask(
        [FromBody] string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(new { error = "The message cannot be empty." });
        }

        var correlationId = Request.Headers["X-Correlation-ID"].ToString();
        try
        {
            var response = await _aiService.AskAsync(
                message,
                cancellationToken,
                correlationId);

            return Ok(new
            {
                response
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}