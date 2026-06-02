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

        public async Task<User?> RegisterAsync(string username, string password, string companyName, int companyTypeId)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                return null;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Új cég létrehozása (SaaS alap) + CompanyTypeId bekötése
                var company = new Company
                {
                    Name = companyName,
                    CompanyTypeId = companyTypeId,
                    SubscriptionPlan = Domain.Models.Enums.SubscriptionPlan.Free,
                    AllowOverlappingAppointments = false,
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync(); // Hogy megkapjuk a company.Id-t

                // 2. Felhasználó létrehozása
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                var user = new User
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    CompanyId = company.Id,
                    Role = "Admin"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

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

                // 4. ONBOARDING VARÁZSLAT: Iparági sablonok másolása
                var templates = await _context.IndustryTemplateAttributes
                    .Where(t => t.CompanyTypeId == companyTypeId && t.IsActive)
                    .ToListAsync();

                if (templates.Any())
                {
                    var companyAttributes = templates.Select(t => new CompanyAttribute
                    {
                        CompanyId = company.Id,
                        Key = t.Key,
                        Label = t.Label,
                        DataType = t.DataType,
                        Options = t.Options, // A JSONB konverzió miatt ez simán másolható
                        IsRequired = t.IsRequired,
                        ShowOnPublicBooking = t.ShowOnPublicBooking,
                        IsActive = t.IsActive
                    }).ToList();

                    _context.CompanyAttributes.AddRange(companyAttributes);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return user;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string?> LoginAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return null;
            }

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
                    expires: DateTime.Now.AddDays(14),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}