namespace Soluvion.API.DTOs.CustomerDtos
{
    /// <summary>
    /// Ügyfelek összevonása: a MergedCustomerIds-ben megadott ügyfelek beolvadnak a PrimaryCustomerId ügyfélbe,
    /// amely a FinalData szerinti végleges adatokat kapja.
    /// </summary>
    public class MergeCustomersDto
    {
        public int PrimaryCustomerId { get; set; }
        public List<int> MergedCustomerIds { get; set; } = new();
        public CreateCustomerDto FinalData { get; set; } = new();
    }

    public class MergeCustomersResultDto
    {
        public int PrimaryCustomerId { get; set; }
        public int MergedCustomerCount { get; set; }

        /// <summary>Az átkerülő (nem duplikált) foglalások száma.</summary>
        public int MovedAppointments { get; set; }

        /// <summary>Az azonos időpontú és azonos szolgáltatású duplikált foglalások száma, amelyek csak egyszer maradnak meg.</summary>
        public int RemovedDuplicateAppointments { get; set; }

        /// <summary>Az összevont ügyfél összes foglalásának száma az összevonás után.</summary>
        public int TotalAppointmentsAfterMerge { get; set; }

        public bool DryRun { get; set; }
    }
}

