using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Soluvion.API.Data;
using Soluvion.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(string username, string password)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                return BadRequest("Ez a felhasználónév már foglalt!");
            }

            CreatePasswordHash(password, out string passwordHash);

            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                CompanyId = 1, // alapertelmezett ceg
                Role = "Admin"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Sikeres regisztráció!");
        }

        private void CreatePasswordHash(string password, out string passwordHash)
        {
            string secretKey = _configuration.GetSection("AppSettings:Token").Value;

            if (string.IsNullOrEmpty(secretKey)) throw new Exception("Nincs beállítva a titkos kulcs!");

            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                passwordHash = Convert.ToBase64String(hashBytes);
            }
        }
    }
}
