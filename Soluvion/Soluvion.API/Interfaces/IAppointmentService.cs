using Soluvion.API.DTOs;
using Soluvion.API.DTOs.AppointmentDtos;
using Soluvion.Domain.Models;

namespace Soluvion.API.Interfaces
{
    public interface IAppointmentService
    {
        Task<Appointment> CreateAppointmentAsync(CreateAppointmentDto dto);
        Task<List<AppointmentResponseDto>> GetAppointmentsAsync(DateTime start, DateTime end, int? employeeId = null);
        Task<List<AppointmentResponseDto>> GetPendingAppointmentsAsync();
        Task<Appointment> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto dto);
        Task<Appointment> UpdateAppointmentStatusAsync(int appointmentId, UpdateAppointmentStatusDto dto);
        Task<bool> DeleteAppointmentAsync(int appointmentId);
    }
}