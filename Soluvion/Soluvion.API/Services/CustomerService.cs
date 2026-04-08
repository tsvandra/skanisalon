using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs.CustomerDtos;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;

namespace Soluvion.API.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public CustomerService(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        public async Task<List<CustomerResponseDto>> GetCompanyCustomersAsync()
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var customers = await _context.CompanyCustomers
                .Where(c => c.CompanyId == companyId)
                .ToListAsync();

            return customers.Select(c =>
            {
                string displayName = "Ismeretlen Vendég";

                if (c.Attributes != null)
                {
                    if (c.Attributes.ContainsKey("FullName") && !string.IsNullOrWhiteSpace(c.Attributes["FullName"]))
                        displayName = c.Attributes["FullName"];
                    else if (c.Attributes.ContainsKey("Name") && !string.IsNullOrWhiteSpace(c.Attributes["Name"]))
                        displayName = c.Attributes["Name"];
                    else if (c.Attributes.ContainsKey("Phone") && !string.IsNullOrWhiteSpace(c.Attributes["Phone"]))
                        displayName = c.Attributes["Phone"];
                    else if (c.Attributes.ContainsKey("Email") && !string.IsNullOrWhiteSpace(c.Attributes["Email"]))
                        displayName = c.Attributes["Email"];
                }

                // Kiszűrjük azokat a kulcsokat, amiket fix mezőként kezelünk (Név, Telefon, Email, Notes)
                var dynamicAttributes = c.Attributes?
                    .Where(kvp => kvp.Key != "FullName" && kvp.Key != "Name" && kvp.Key != "Phone" && kvp.Key != "Email" && kvp.Key != "Notes")
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();

                // A megjegyzés prioritása: Ha van fizikai mező, azt használjuk, ha nincs, akkor megnézzük maradt-e a JSON-ben régi adat
                string? notes = c.Notes;
                if (string.IsNullOrWhiteSpace(notes) && c.Attributes != null && c.Attributes.ContainsKey("Notes"))
                {
                    notes = c.Attributes["Notes"];
                }

                return new CustomerResponseDto
                {
                    Id = c.Id,
                    Name = displayName,
                    Phone = c.Attributes != null && c.Attributes.ContainsKey("Phone") ? c.Attributes["Phone"] : null,
                    Email = c.Attributes != null && c.Attributes.ContainsKey("Email") ? c.Attributes["Email"] : null,
                    Notes = notes,
                    Attributes = dynamicAttributes // Csak a tiszta, egyedi jellemzők mennek a frontendnek
                };
            }).OrderBy(c => c.Name).ToList();
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            // A frontendről érkező egyedi jellemzőkből indulunk ki (vagy üres lista)
            var attributes = dto.Attributes != null
                ? new Dictionary<string, string>(dto.Attributes)
                : new Dictionary<string, string>();

            // Hozzáadjuk a fix alapadatokat a JSONB-hez
            if (!string.IsNullOrWhiteSpace(dto.FullName)) attributes["FullName"] = dto.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone)) attributes["Phone"] = dto.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Email)) attributes["Email"] = dto.Email.Trim();

            // A Notes szigorúan a fizikai oszlopba megy, kivesszük a JSON-ből, ha véletlenül bekerült volna
            if (attributes.ContainsKey("Notes")) attributes.Remove("Notes");

            var customer = new CompanyCustomer
            {
                CompanyId = companyId,
                UserId = null,
                Attributes = attributes,
                Notes = dto.Notes?.Trim() // Fizikai oszlopba mentjük
            };

            _context.CompanyCustomers.Add(customer);
            await _context.SaveChangesAsync();

            string displayName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName :
                                 (!string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone : dto.Email ?? "Névtelen Vendég");

            return new CustomerResponseDto
            {
                Id = customer.Id,
                Name = displayName,
                Phone = dto.Phone,
                Email = dto.Email,
                Notes = customer.Notes,
                Attributes = dto.Attributes ?? new Dictionary<string, string>()
            };
        }

        public async Task<CustomerResponseDto> UpdateCustomerAsync(int id, CreateCustomerDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var customer = await _context.CompanyCustomers
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (customer == null) throw new KeyNotFoundException("Az ügyfél nem található.");

            // A frontendről érkező egyedi jellemzőkből indulunk ki
            var attributes = dto.Attributes != null
                ? new Dictionary<string, string>(dto.Attributes)
                : new Dictionary<string, string>();

            // Frissítjük a fix alapadatokat
            if (!string.IsNullOrWhiteSpace(dto.FullName)) attributes["FullName"] = dto.FullName.Trim();
            else attributes.Remove("FullName");

            if (!string.IsNullOrWhiteSpace(dto.Phone)) attributes["Phone"] = dto.Phone.Trim();
            else attributes.Remove("Phone");

            if (!string.IsNullOrWhiteSpace(dto.Email)) attributes["Email"] = dto.Email.Trim();
            else attributes.Remove("Email");

            // A Notes szigorúan a fizikai oszlopba megy, tisztítjuk a JSONB-t a régi adatoktól
            if (attributes.ContainsKey("Notes")) attributes.Remove("Notes");

            customer.Attributes = attributes;
            customer.Notes = dto.Notes?.Trim(); // Fizikai oszlop frissítése

            _context.CompanyCustomers.Update(customer);
            await _context.SaveChangesAsync();

            string displayName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName :
                                 (!string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone : dto.Email ?? "Névtelen Vendég");

            return new CustomerResponseDto
            {
                Id = customer.Id,
                Name = displayName,
                Phone = dto.Phone,
                Email = dto.Email,
                Notes = customer.Notes,
                Attributes = dto.Attributes ?? new Dictionary<string, string>()
            };
        }

        public async Task DeleteCustomerAsync(int id)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var customer = await _context.CompanyCustomers
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (customer == null) throw new KeyNotFoundException("Az ügyfél nem található.");

            bool hasAppointments = await _context.Appointments.AnyAsync(a => a.CustomerId == id && a.CompanyId == companyId);
            if (hasAppointments)
            {
                throw new InvalidOperationException("Ezt az ügyfelet nem lehet törölni, mert már tartozik hozzá foglalás.");
            }

            _context.CompanyCustomers.Remove(customer);
            await _context.SaveChangesAsync();
        }
    }
}