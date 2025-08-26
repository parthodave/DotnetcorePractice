using DotNet8WebAPI.Helpers;
using DotNet8WebAPI.Middlewares;
using DotNet8WebAPI.Model;
using DotNet8WebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotNet8WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;
        private ILogger<UsersController> _logger;
     
        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get()
        {
            return Ok(await _userService.GetAll());
        }

        [HttpPost]
       // [Authorize]
        public async Task<IActionResult> Post([FromBody] User userObj)
        {
            userObj.Id = 0;
            return Ok(await _userService.AddAndUpdateUser(userObj));
        }

        // PUT api/<CustomerController>/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(int id, [FromBody] User userObj)
        {
            userObj.Id = id;
            return Ok(await _userService.AddAndUpdateUser(userObj));
        }

        [HttpPost("login")]
        public async Task<IActionResult> login(AuthenticateRequest model)
        {
            _logger.LogInformation("Inside the login method...");
             var ij = 0;
             var jk = 25 / ij;
            var response = await _userService.Authenticate(model);
           
            //if (response == null)
            //    return BadRequest(new { message = "Username or password is incorrect" });
            _logger.LogInformation("Login completed...");
            return Ok(response);
        }

    }
}
