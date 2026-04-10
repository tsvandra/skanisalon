namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class AppointmentItemRequestDto
    {
        public int ServiceVariantId { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
    }
}