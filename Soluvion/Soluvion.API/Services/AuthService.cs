using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Soluvion.API.Data;
using Soluvion.Domain.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User?> RegisterAsync(string username, string password, string companyName)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                return null;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Új cég létrehozása (SaaS alap)
                var company = new Company
                {
                    Name = companyName,
                    SubscriptionPlan = Domain.Models.Enums.SubscriptionPlan.Free,
                    AllowOverlappingAppointments = false,
                    // ... egyéb kötelező alapértékek a Company-n
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync(); // Hogy megkapjuk a company.Id-t

                // 2. Felhasználó létrehozása
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                var user = new User
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    CompanyId = company.Id, // Az alapértelmezett cége
                    Role = "Admin" // Globális szerepkör
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // Hogy megkapjuk a user.Id-t

                // 3. SaaS Jogosultság: CompanyEmployee összekötés 'Owner' role-lal
                var employee = new CompanyEmployee
                {
                    CompanyId = company.Id,
                    UserId = user.Id,
                    Role = Domain.Models.Enums.EmployeeRole.Owner,
                    IsActive = true
                };
                _context.CompanyEmployees.Add(employee);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return user;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // A GlobalExceptionHandler majd elkapja
            }
        }

        public async Task<string?> LoginAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return null;
            }

            // BCrypt ellenőrzés
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null;
            }

            return CreateToken(user);
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("CompanyId", user.CompanyId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}