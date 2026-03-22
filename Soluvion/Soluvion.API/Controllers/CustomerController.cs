using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs.CustomerDtos;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    // A frontendben "api/customers"-t használunk, ezért kisbetűvel érdemes hagyni az útvonalat
    [Route("api/customers")]
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto dto)
        {
            try
            {
                var result = await _customerService.UpdateCustomerAsync(id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
                return NoContent(); // 204 No Content - sikeres törlés, nincs visszatérő adat
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Ha van foglalása, ezt a hibaüzenetet küldjük vissza (400 Bad Request)
                return BadRequest(ex.Message);
            }
        }
    }
}