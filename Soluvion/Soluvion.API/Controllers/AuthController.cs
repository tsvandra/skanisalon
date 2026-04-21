using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            var user = await _authService.RegisterAsync(
                request.Username,
                request.Password,
                request.CompanyName,
                request.CompanyTypeId
            );

            if (user == null)
            {
                return BadRequest("Ez a felhasználónév már foglalt, vagy a regisztráció sikertelen!");
            }

            return Ok(new { Message = "Sikeres regisztráció!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var token = await _authService.LoginAsync(request.Username, request.Password);

            if (token == null)
            {
                return BadRequest("Hibás felhasználónév vagy jelszó.");
            }

            return Ok(new { Token = token });
        }
    }
}