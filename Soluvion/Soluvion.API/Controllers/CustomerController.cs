using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs.CustomerDtos;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Csak bejelentkezett dolgozók férhetnek hozzá!
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var result = await _customerService.GetCompanyCustomersAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            var result = await _customerService.CreateCustomerAsync(dto);
            return CreatedAtAction(nameof(GetCustomers), new { id = result.Id }, result);
        }
    }
}