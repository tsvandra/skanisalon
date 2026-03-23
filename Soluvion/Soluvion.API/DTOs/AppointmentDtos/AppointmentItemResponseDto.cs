namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class AppointmentItemResponseDto
    {
        public int Id { get; set; }
        public int ServiceVariantId { get; set; }
        public int CalculatedDurationMinutes { get; set; }
        public decimal Price { get; set; }
    }
}