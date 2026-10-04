namespace Soluvion.API.DTOs.AppointmentDtos
{
    using System;
    using System.Collections.Generic;

    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool MaterialUsageRecorded { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PrimaryServiceName { get; set; } = string.Empty;
        public List<AppointmentItemResponseDto> Items { get; set; } = new();
    }
}

