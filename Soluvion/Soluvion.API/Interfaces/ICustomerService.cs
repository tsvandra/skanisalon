using Soluvion.API.DTOs.CustomerDtos;

namespace Soluvion.API.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerResponseDto>> GetCompanyCustomersAsync();
        Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto dto);
        Task<CustomerResponseDto> UpdateCustomerAsync(int id, CreateCustomerDto dto);
        Task DeleteCustomerAsync(int id);
    }
}