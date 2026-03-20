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
                    // Okos név/azonosító választó: Megpróbáljuk a legjobb azonosítót megtalálni
                    if (c.Attributes.ContainsKey("FullName") && !string.IsNullOrWhiteSpace(c.Attributes["FullName"]))
                        displayName = c.Attributes["FullName"];
                    else if (c.Attributes.ContainsKey("Name") && !string.IsNullOrWhiteSpace(c.Attributes["Name"]))
                        displayName = c.Attributes["Name"]; // Kompatibilitás "Jozso"-val
                    else if (c.Attributes.ContainsKey("Phone") && !string.IsNullOrWhiteSpace(c.Attributes["Phone"]))
                        displayName = c.Attributes["Phone"];
                    else if (c.Attributes.ContainsKey("Email") && !string.IsNullOrWhiteSpace(c.Attributes["Email"]))
                        displayName = c.Attributes["Email"];
                }

                return new CustomerResponseDto
                {
                    Id = c.Id,
                    Name = displayName
                };
            }).OrderBy(c => c.Name).ToList();
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var attributes = new Dictionary<string, string>();

            // Csak azt mentjük a JSONB-be, amit ténylegesen megadtak!
            if (!string.IsNullOrWhiteSpace(dto.FullName)) attributes.Add("FullName", dto.FullName.Trim());
            if (!string.IsNullOrWhiteSpace(dto.Phone)) attributes.Add("Phone", dto.Phone.Trim());
            if (!string.IsNullOrWhiteSpace(dto.Email)) attributes.Add("Email", dto.Email.Trim());

            var customer = new CompanyCustomer
            {
                CompanyId = companyId,
                UserId = null,
                Attributes = attributes
            };

            _context.CompanyCustomers.Add(customer);
            await _context.SaveChangesAsync();

            // Visszaadjuk a legördülőnek a legjobb megjeleníthető nevet
            string displayName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName :
                                 (!string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone : dto.Email ?? "Névtelen Vendég");

            return new CustomerResponseDto
            {
                Id = customer.Id,
                Name = displayName
            };
        }
    }
}