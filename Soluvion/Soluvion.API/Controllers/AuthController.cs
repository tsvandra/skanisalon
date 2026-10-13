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
                return BadRequest(new { errorCode = "USERNAME_ALREADY_EXISTS" });
            }

            return Ok(new { success = true });
        }

        [HttpGet("company-types")]
        public async Task<IActionResult> GetCompanyTypes([FromServices] Data.AppDbContext db)
        {
            var types = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                db.CompanyTypes.Select(ct => new { ct.Id, ct.Name }));
            return Ok(types);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            try
            {
                var token = await _authService.LoginAsync(request.Username, request.Password);

                if (token == null)
                {
                    return BadRequest(new { errorCode = "INVALID_CREDENTIALS" });
                }

                return Ok(new { Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new { errorCode = ex.Message });
            }
        }
    }
}