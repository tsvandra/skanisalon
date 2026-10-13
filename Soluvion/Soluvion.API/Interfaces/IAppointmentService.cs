using Soluvion.API.DTOs;
using Soluvion.API.DTOs.AppointmentDtos;
using Soluvion.Domain.Models;

namespace Soluvion.API.Interfaces
{
    public interface IAppointmentService
    {
        Task<Appointment> CreateAppointmentAsync(CreateAppointmentDto dto);
        Task<List<AppointmentResponseDto>> GetAppointmentsAsync(DateTime start, DateTime end, int? employeeId = null);
        Task<Appointment> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto dto);
        Task<bool> DeleteAppointmentAsync(int appointmentId);
    }
}