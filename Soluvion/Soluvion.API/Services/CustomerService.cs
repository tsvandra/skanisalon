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

                return new CustomerResponseDto
                {
                    Id = c.Id,
                    Name = displayName,
                    Phone = c.Attributes != null && c.Attributes.ContainsKey("Phone") ? c.Attributes["Phone"] : null,
                    Email = c.Attributes != null && c.Attributes.ContainsKey("Email") ? c.Attributes["Email"] : null,
                    Notes = c.Attributes != null && c.Attributes.ContainsKey("Notes") ? c.Attributes["Notes"] : null
                };
            }).OrderBy(c => c.Name).ToList();
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var attributes = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(dto.FullName)) attributes.Add("FullName", dto.FullName.Trim());
            if (!string.IsNullOrWhiteSpace(dto.Phone)) attributes.Add("Phone", dto.Phone.Trim());
            if (!string.IsNullOrWhiteSpace(dto.Email)) attributes.Add("Email", dto.Email.Trim());
            if (!string.IsNullOrWhiteSpace(dto.Notes)) attributes.Add("Notes", dto.Notes.Trim());

            var customer = new CompanyCustomer
            {
                CompanyId = companyId,
                UserId = null,
                Attributes = attributes
            };

            _context.CompanyCustomers.Add(customer);
            await _context.SaveChangesAsync();

            string displayName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName :
                                 (!string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone : dto.Email ?? "Névtelen Vendég");

            return new CustomerResponseDto { Id = customer.Id, Name = displayName };
        }

        public async Task<CustomerResponseDto> UpdateCustomerAsync(int id, CreateCustomerDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var customer = await _context.CompanyCustomers
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (customer == null) throw new KeyNotFoundException("Az ügyfél nem található.");

            // Újraépítjük az attribútumokat
            var attributes = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(dto.FullName)) attributes.Add("FullName", dto.FullName.Trim());
            if (!string.IsNullOrWhiteSpace(dto.Phone)) attributes.Add("Phone", dto.Phone.Trim());
            if (!string.IsNullOrWhiteSpace(dto.Email)) attributes.Add("Email", dto.Email.Trim());
            if (!string.IsNullOrWhiteSpace(dto.Notes)) attributes.Add("Notes", dto.Notes.Trim());

            customer.Attributes = attributes;

            _context.CompanyCustomers.Update(customer);
            await _context.SaveChangesAsync();

            string displayName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName :
                                 (!string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone : dto.Email ?? "Névtelen Vendég");

            return new CustomerResponseDto { Id = customer.Id, Name = displayName };
        }

        public async Task DeleteCustomerAsync(int id)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var customer = await _context.CompanyCustomers
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (customer == null) throw new KeyNotFoundException("Az ügyfél nem található.");

            // VÉDELEM: Ellenőrizzük, hogy van-e foglalása
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