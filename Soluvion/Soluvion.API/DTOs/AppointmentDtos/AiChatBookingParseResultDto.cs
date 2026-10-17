namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class AiChatBookingParseResultDto
    {
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? Date { get; set; }
        public string? Time { get; set; }
        public List<int> ServiceVariantIds { get; set; } = new();
        public string Channel { get; set; } = "WhatsApp";
        public string Notes { get; set; } = string.Empty;
        public int ConfidencePercent { get; set; } = 90;
        public List<string> MissingFields { get; set; } = new();
        public List<string> ExtractedMessages { get; set; } = new();
    }
}
