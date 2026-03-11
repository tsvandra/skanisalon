using Soluvion.API.DTOs;
using Soluvion.API.DTOs.AppointmentDtos;
using Soluvion.Domain.Models;

namespace Soluvion.API.Interfaces
{
    public interface IAppointmentService
    {
        Task<Appointment> CreateAppointmentAsync(CreateAppointmentDto dto, string username);
        Task<List<AppointmentResponseDto>> GetAppointmentsAsync(DateTime start, DateTime end, string username, int? employeeId = null);
        Task<Appointment> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto dto, string username);
        Task<bool> DeleteAppointmentAsync(int appointmentId, string username);
    }
}