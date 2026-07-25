using DotNet8WebAPI.Helpers;
using DotNet8WebAPI.Model;
using DotNet8WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights;

namespace DotNet8WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OurHeroController : ControllerBase
    {
        private readonly IOurHeroService _heroService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<OurHeroController> _logger;

        public OurHeroController(IOurHeroService heroService, TelemetryClient telemetryClient, ILogger<OurHeroController> logger)
        {
            _heroService = heroService;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool? isActive = null)
        {
            _logger.LogInformation("OurHeroController: GetAllHeros operation initiated with isActive={IsActive}", isActive);
            _telemetryClient.TrackEvent("Heroes.GetAll.Started", new Dictionary<string, string>
            {
                { "IsActiveFilter", isActive?.ToString() ?? "null" }
            });

            try
            {
                var heros = await _heroService.GetAllHeros(isActive);

                _telemetryClient.TrackEvent("Heroes.GetAll.Success", new Dictionary<string, string>
                {
                    { "HeroCount", heros?.Count().ToString() ?? "0" },
                    { "IsActiveFilter", isActive?.ToString() ?? "null" }
                });

                _logger.LogInformation("Successfully retrieved {Count} heroes", heros?.Count() ?? 0);
                return Ok(heros);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackEvent("Heroes.GetAll.Failed", new Dictionary<string, string>
                {
                    { "Error", ex.Message }
                });
                throw;
            }
        }

        [HttpGet("{id}")]
        //[Route("{id}")] // /api/OurHero/:id
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation("OurHeroController: Get hero by ID {HeroId}", id);
            _telemetryClient.TrackEvent("Heroes.GetById.Started", new Dictionary<string, string>
            {
                { "HeroId", id.ToString() }
            });

            var hero = await _heroService.GetHerosByID(id);
            if (hero == null)
            {
                _telemetryClient.TrackEvent("Heroes.GetById.NotFound", new Dictionary<string, string>
                {
                    { "HeroId", id.ToString() }
                });
                _logger.LogWarning("Hero with ID {HeroId} not found", id);
                return NotFound();
            }

            _telemetryClient.TrackEvent("Heroes.GetById.Success", new Dictionary<string, string>
            {
                { "HeroId", id.ToString() },
                { "HeroName", hero.FirstName ?? "N/A" }
            });

            return Ok(hero);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AddUpdateOurHero heroObject)
        {
            _logger.LogInformation("OurHeroController: AddOurHero operation initiated for '{Name}'", heroObject?.FirstName);
            _telemetryClient.TrackEvent("Heroes.Add.Started", new Dictionary<string, string>
            {
                { "HeroName", heroObject?.FirstName ?? "N/A" }
            });

            var hero = await _heroService.AddOurHero(heroObject);

            if (hero == null)
            {
                _telemetryClient.TrackEvent("Heroes.Add.Failed", new Dictionary<string, string>
                {
                    { "HeroName", heroObject?.FirstName ?? "N/A" }
                });
                _logger.LogError("Failed to add hero '{Name}'", heroObject?.FirstName);
                return BadRequest();
            }

            _telemetryClient.TrackEvent("Heroes.Add.Success", new Dictionary<string, string>
            {
                { "HeroId", hero!.Id.ToString() },
                { "HeroName", hero.FirstName ?? "N/A" }
            });
            _logger.LogInformation("Successfully created hero with ID {HeroId}", hero.Id);

            return Ok(new
            {
                message = "Super Hero Created Successfully!!!",
                id = hero!.Id
            });
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] AddUpdateOurHero heroObject)
        {
            _logger.LogInformation("OurHeroController: UpdateOurHero operation initiated for ID {HeroId}", id);
            _telemetryClient.TrackEvent("Heroes.Update.Started", new Dictionary<string, string>
            {
                { "HeroId", id.ToString() },
                { "HeroName", heroObject?.FirstName ?? "N/A" }
            });

            var hero = await _heroService.UpdateOurHero(id, heroObject);
            if (hero == null)
            {
                _telemetryClient.TrackEvent("Heroes.Update.NotFound", new Dictionary<string, string>
                {
                    { "HeroId", id.ToString() }
                });
                _logger.LogWarning("Hero with ID {HeroId} not found for update", id);
                return NotFound();
            }

            _telemetryClient.TrackEvent("Heroes.Update.Success", new Dictionary<string, string>
            {
                { "HeroId", hero!.Id.ToString() },
                { "HeroName", hero.FirstName ?? "N/A" }
            });
            _logger.LogInformation("Successfully updated hero with ID {HeroId}", hero.Id);

            return Ok(new
            {
                message = "Super Hero Updated Successfully!!!",
                id = hero!.Id
            });
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            if (!await _heroService.DeleteHerosByID(id))
            {
                return NotFound();
            }

            return Ok(new
            {
                message = "Super Hero Deleted Successfully!!!",
                id = id
            });
        }
    }
}
