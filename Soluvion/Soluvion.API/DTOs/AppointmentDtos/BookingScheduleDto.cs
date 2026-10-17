using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class BookingTimeIntervalDto
    {
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(10, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Start { get; set; } = "08:00";

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(10, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string End { get; set; } = "16:00";
    }

    public class BookingDayScheduleDto
    {
        public bool IsOpen { get; set; }
        public List<BookingTimeIntervalDto> Intervals { get; set; } = new();
    }

    public class BookingScheduleConfigDto
    {
        /// <summary>
        /// "custom_days" = Csak az egyedileg megnyitott napok/idősávok foglalhatók (pl. meghirdetett napok)
        /// "weekly_template" = Fix heti munkarend (1=Hétfő..7=Vasárnap) + egyedi napi felülbírálatok
        /// </summary>
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(30, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Mode { get; set; } = "weekly_template";

        /// <summary>
        /// Kulcs: "1" (Hétfő) .. "7" (Vasárnap)
        /// </summary>
        public Dictionary<string, BookingDayScheduleDto> WeeklyHours { get; set; } = new();

        /// <summary>
        /// Kulcs: "YYYY-MM-DD" (bármely jövőbeli dátumra megadható egyedi nyitvatartás vagy zárás)
        /// </summary>
        public Dictionary<string, BookingDayScheduleDto> DateOverrides { get; set; } = new();
    }

    public class TimeIntervalMinutesDto
    {
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
        public int StartMinutes { get; set; }
        public int EndMinutes { get; set; }
        public int DurationMinutes => Math.Max(0, EndMinutes - StartMinutes);
    }

    public class DayAvailabilityDto
    {
        public string Date { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public bool HasEnoughFreeTime { get; set; }
        public bool HasCustomOverride { get; set; }
        public int LoadPercentage { get; set; }
        public List<TimeIntervalMinutesDto> OpenIntervals { get; set; } = new();
        public List<TimeIntervalMinutesDto> BusyIntervals { get; set; } = new();
        public List<TimeIntervalMinutesDto> FreeIntervals { get; set; } = new();
    }

    public class UpdateDayOverrideDto
    {
        public bool ClearOverride { get; set; }
        public bool IsOpen { get; set; }
        public List<BookingTimeIntervalDto> Intervals { get; set; } = new();
    }

    public class PublicEmployeeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PublicAvailabilityResponseDto
    {
        public string Mode { get; set; } = "weekly_template";
        public List<PublicEmployeeDto> Employees { get; set; } = new();
        public List<DayAvailabilityDto> Days { get; set; } = new();
    }
}

