using Microsoft.AspNetCore.Http;
using Soluvion.API.DTOs.AppointmentDtos;

namespace Soluvion.API.Interfaces
{
    public interface IAiChatBookingService
    {
        Task<AiChatBookingParseResultDto> ParseChatBookingAsync(string? messagesText, IEnumerable<IFormFile>? images, string? channelHint = null);
        Task<UpdatePendingFromChatResultDto> UpdatePendingAppointmentFromChatAsync(int appointmentId, string? messagesText, IEnumerable<IFormFile>? images = null, string? channelHint = null);
    }
}
