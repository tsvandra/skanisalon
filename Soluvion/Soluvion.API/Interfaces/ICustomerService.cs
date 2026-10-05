using Soluvion.API.DTOs.CustomerDtos;

namespace Soluvion.API.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerResponseDto>> GetCompanyCustomersAsync();
        Task<CustomerResponseDto> GetCustomerByIdAsync(int id);
        Task<List<Soluvion.API.DTOs.AppointmentDtos.AppointmentResponseDto>> GetCustomerAppointmentsAsync(int id);
        Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto dto);
        Task<CustomerResponseDto> UpdateCustomerAsync(int id, CreateCustomerDto dto);
        Task DeleteCustomerAsync(int id);
    }
}