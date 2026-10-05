using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs.CustomerDtos;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;

namespace Soluvion.API.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public CustomerService(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        public async Task<List<CustomerResponseDto>> GetCompanyCustomersAsync()
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var customers = await _context.CompanyCustomers
                .Where(c => c.CompanyId == companyId)
                .ToListAsync();

            return customers.Select(c =>
            {
                string displayName = "Ismeretlen Vendég";

                if (c.Attributes != null)
                {
                    if (c.Attributes.ContainsKey("FullName") && !string.IsNullOrWhiteSpace(c.Attributes["FullName"]))
                        displayName = c.Attributes["FullName"];
                    else if (c.Attributes.ContainsKey("Name") && !string.IsNullOrWhiteSpace(c.Attributes["Name"]))
                        displayName = c.Attributes["Name"];
                    else if (c.Attributes.ContainsKey("Phone") && !string.IsNullOrWhiteSpace(c.Attributes["Phone"]))
                        displayName = c.Attributes["Phone"];
                    else if (c.Attributes.ContainsKey("Email") && !string.IsNullOrWhiteSpace(c.Attributes["Email"]))
                        displayName = c.Attributes["Email"];
                }

                // Kiszűrjük azokat a kulcsokat, amiket fix mezőként kezelünk (Név, Telefon, Email, Notes)
                var dynamicAttributes = c.Attributes?
                    .Where(kvp => kvp.Key != "FullName" && kvp.Key != "Name" && kvp.Key != "Phone" && kvp.Key != "Email" && kvp.Key != "Notes")
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();

                // A megjegyzés prioritása: Ha van fizikai mező, azt használjuk, ha nincs, akkor megnézzük maradt-e a JSON-ben régi adat
                string? notes = c.Notes;
                if (string.IsNullOrWhiteSpace(notes) && c.Attributes != null && c.Attributes.ContainsKey("Notes"))
                {
                    notes = c.Attributes["Notes"];
                }

                return new CustomerResponseDto
                {
                    Id = c.Id,
                    Name = displayName,
                    Phone = c.Attributes != null && c.Attributes.ContainsKey("Phone") ? c.Attributes["Phone"] : null,
                    Email = c.Attributes != null && c.Attributes.ContainsKey("Email") ? c.Attributes["Email"] : null,
                    Notes = notes,
                    Attributes = dynamicAttributes // Csak a tiszta, egyedi jellemzők mennek a frontendnek
                };
            }).OrderBy(c => c.Name).ToList();
        }

        public async Task<CustomerResponseDto> GetCustomerByIdAsync(int id)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var c = await _context.CompanyCustomers
                .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);

            if (c == null) throw new KeyNotFoundException("Az ügyfél nem található.");

            string displayName = "Ismeretlen Vendég";

            if (c.Attributes != null)
            {
                if (c.Attributes.ContainsKey("FullName") && !string.IsNullOrWhiteSpace(c.Attributes["FullName"]))
                    displayName = c.Attributes["FullName"];
                else if (c.Attributes.ContainsKey("Name") && !string.IsNullOrWhiteSpace(c.Attributes["Name"]))
                    displayName = c.Attributes["Name"];
                else if (c.Attributes.ContainsKey("Phone") && !string.IsNullOrWhiteSpace(c.Attributes["Phone"]))
                    displayName = c.Attributes["Phone"];
                else if (c.Attributes.ContainsKey("Email") && !string.IsNullOrWhiteSpace(c.Attributes["Email"]))
                    displayName = c.Attributes["Email"];
            }

            var dynamicAttributes = c.Attributes?
                .Where(kvp => kvp.Key != "FullName" && kvp.Key != "Name" && kvp.Key != "Phone" && kvp.Key != "Email" && kvp.Key != "Notes")
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();

            string? notes = c.Notes;
            if (string.IsNullOrWhiteSpace(notes) && c.Attributes != null && c.Attributes.ContainsKey("Notes"))
            {
                notes = c.Attributes["Notes"];
            }

            return new CustomerResponseDto
            {
                Id = c.Id,
                Name = displayName,
                Phone = c.Attributes != null && c.Attributes.ContainsKey("Phone") ? c.Attributes["Phone"] : null,
                Email = c.Attributes != null && c.Attributes.ContainsKey("Email") ? c.Attributes["Email"] : null,
                Notes = notes,
                Attributes = dynamicAttributes
            };
        }

        public async Task<List<Soluvion.API.DTOs.AppointmentDtos.AppointmentResponseDto>> GetCustomerAppointmentsAsync(int id)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var appointments = await _context.Appointments
                .Include(a => a.Items)
                    .ThenInclude(i => i.ServiceVariant)
                    .ThenInclude(sv => sv!.Service)
                .Include(a => a.Employee)
                    .ThenInclude(e => e!.User)
                .Where(a => a.CustomerId == id && a.CompanyId == companyId)
                .OrderByDescending(a => a.StartDateTime)
                .ToListAsync();

            return appointments.Select(a => new Soluvion.API.DTOs.AppointmentDtos.AppointmentResponseDto
            {
                Id = a.Id,
                CustomerId = a.CustomerId,
                CustomerName = "", 
                EmployeeId = a.EmployeeId,
                EmployeeName = a.Employee?.User?.Username ?? "Ismeretlen dolgozó",
                StartDateTime = a.StartDateTime,
                EndDateTime = a.EndDateTime,
                Status = a.Status.ToString(),
                TotalPrice = a.TotalPrice,
                CustomerNotes = a.CustomerNotes,
                Items = a.Items.Select(i => new Soluvion.API.DTOs.AppointmentDtos.AppointmentItemResponseDto
                {
                    ServiceId = i.ServiceVariant?.ServiceId ?? 0,
                    ServiceName = (i.ServiceVariant?.Service?.Name != null && i.ServiceVariant.Service.Name.ContainsKey("hu")) ? i.ServiceVariant.Service.Name["hu"] : "Ismeretlen szolgáltatás",
                    Price = i.Price
                }).ToList(),
                MaterialUsageRecorded = a.MaterialUsageRecorded
            }).ToList();
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            // A frontendről érkező egyedi jellemzőkből indulunk ki (vagy üres lista)
            var attributes = dto.Attributes != null
                ? new Dictionary<string, string>(dto.Attributes)
                : new Dictionary<string, string>();

            // Hozzáadjuk a fix alapadatokat a JSONB-hez
            if (!string.IsNullOrWhiteSpace(dto.FullName)) attributes["FullName"] = dto.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone)) attributes["Phone"] = dto.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Email)) attributes["Email"] = dto.Email.Trim();

            // A Notes szigorúan a fizikai oszlopba megy, kivesszük a JSON-ből, ha véletlenül bekerült volna
            if (attributes.ContainsKey("Notes")) attributes.Remove("Notes");

            var customer = new CompanyCustomer
            {
                CompanyId = companyId,
                UserId = null,
                Attributes = attributes,
                Notes = dto.Notes?.Trim() // Fizikai oszlopba mentjük
            };

            _context.CompanyCustomers.Add(customer);
            await _context.SaveChangesAsync();

            string displayName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName :
                                 (!string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone : dto.Email ?? "Névtelen Vendég");

            return new CustomerResponseDto
            {
                Id = customer.Id,
                Name = displayName,
                Phone = dto.Phone,
                Email = dto.Email,
                Notes = customer.Notes,
                Attributes = dto.Attributes ?? new Dictionary<string, string>()
            };
        }

        public async Task<CustomerResponseDto> UpdateCustomerAsync(int id, CreateCustomerDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var customer = await _context.CompanyCustomers
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (customer == null) throw new KeyNotFoundException("Az ügyfél nem található.");

            // A frontendről érkező egyedi jellemzőkből indulunk ki
            var attributes = dto.Attributes != null
                ? new Dictionary<string, string>(dto.Attributes)
                : new Dictionary<string, string>();

            // Frissítjük a fix alapadatokat
            if (!string.IsNullOrWhiteSpace(dto.FullName)) attributes["FullName"] = dto.FullName.Trim();
            else attributes.Remove("FullName");

            if (!string.IsNullOrWhiteSpace(dto.Phone)) attributes["Phone"] = dto.Phone.Trim();
            else attributes.Remove("Phone");

            if (!string.IsNullOrWhiteSpace(dto.Email)) attributes["Email"] = dto.Email.Trim();
            else attributes.Remove("Email");

            // A Notes szigorúan a fizikai oszlopba megy, tisztítjuk a JSONB-t a régi adatoktól
            if (attributes.ContainsKey("Notes")) attributes.Remove("Notes");

            customer.Attributes = attributes;
            customer.Notes = dto.Notes?.Trim(); // Fizikai oszlop frissítése

            _context.CompanyCustomers.Update(customer);
            await _context.SaveChangesAsync();

            string displayName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName :
                                 (!string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone : dto.Email ?? "Névtelen Vendég");

            return new CustomerResponseDto
            {
                Id = customer.Id,
                Name = displayName,
                Phone = dto.Phone,
                Email = dto.Email,
                Notes = customer.Notes,
                Attributes = dto.Attributes ?? new Dictionary<string, string>()
            };
        }

        public async Task DeleteCustomerAsync(int id)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var customer = await _context.CompanyCustomers
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (customer == null) throw new KeyNotFoundException("Az ügyfél nem található.");

            bool hasAppointments = await _context.Appointments.AnyAsync(a => a.CustomerId == id && a.CompanyId == companyId);
            if (hasAppointments)
            {
                throw new InvalidOperationException("Ezt az ügyfelet nem lehet törölni, mert már tartozik hozzá foglalás.");
            }

            _context.CompanyCustomers.Remove(customer);
            await _context.SaveChangesAsync();
        }

        public async Task<MergeCustomersResultDto> MergeCustomersAsync(MergeCustomersDto dto, bool dryRun)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var mergedIds = (dto.MergedCustomerIds ?? new List<int>())
                .Where(i => i != dto.PrimaryCustomerId)
                .Distinct()
                .ToList();

            if (mergedIds.Count == 0)
                throw new ArgumentException("Összevonáshoz legalább két különböző ügyfelet kell megadni.");

            var allIds = mergedIds.Append(dto.PrimaryCustomerId).ToList();

            var customers = await _context.CompanyCustomers
                .Where(c => allIds.Contains(c.Id) && c.CompanyId == companyId)
                .ToListAsync();

            if (customers.Count != allIds.Count)
                throw new KeyNotFoundException("Valamelyik kiválasztott ügyfél nem található.");

            var primary = customers.First(c => c.Id == dto.PrimaryCustomerId);
            var duplicates = customers.Where(c => c.Id != primary.Id).ToList();

            var appointments = await _context.Appointments
                .Include(a => a.Items)
                .Where(a => a.CompanyId == companyId && allIds.Contains(a.CustomerId))
                .ToListAsync();

            // Azok a foglalások, amikhez készletmozgás tartozik, nem törölhetők
            var appointmentIds = appointments.Select(a => a.Id).ToList();
            var appointmentsWithDocuments = (await _context.InventoryDocuments
                .Where(d => d.AppointmentId != null && appointmentIds.Contains(d.AppointmentId.Value))
                .Select(d => d.AppointmentId!.Value)
                .ToListAsync()).ToHashSet();

            bool IsProtected(Appointment a) => a.MaterialUsageRecorded || appointmentsWithDocuments.Contains(a.Id);

            // Duplikátum = ugyanaz a dolgozó, ugyanaz az időpont és pontosan ugyanazok a szolgáltatások
            var groups = appointments.GroupBy(a => (
                a.EmployeeId,
                a.StartDateTime,
                Services: string.Join(",", a.Items.Select(i => i.ServiceVariantId).OrderBy(x => x))));

            var toRemove = new List<(Appointment Duplicate, Appointment Keeper)>();

            foreach (var group in groups)
            {
                if (group.Count() < 2) continue;

                // A megtartandó: zárolt/készletes > előrehaladottabb státusz > régebbi
                var ordered = group
                    .OrderByDescending(a => IsProtected(a))
                    .ThenByDescending(a => GetStatusRank(a.Status))
                    .ThenBy(a => a.Id)
                    .ToList();

                var keeper = ordered[0];
                foreach (var other in ordered.Skip(1))
                {
                    // Készletmozgással rendelkező foglalást nem törlünk, az inkább átkerül az ügyfélhez
                    if (IsProtected(other)) continue;
                    toRemove.Add((other, keeper));
                }
            }

            var removedSet = toRemove.Select(r => r.Duplicate).ToHashSet();
            var toMove = appointments.Where(a => !removedSet.Contains(a) && a.CustomerId != primary.Id).ToList();

            var result = new MergeCustomersResultDto
            {
                PrimaryCustomerId = primary.Id,
                MergedCustomerCount = duplicates.Count,
                MovedAppointments = toMove.Count,
                RemovedDuplicateAppointments = toRemove.Count,
                TotalAppointmentsAfterMerge = appointments.Count - toRemove.Count,
                DryRun = dryRun
            };

            if (dryRun) return result;

            var final = dto.FinalData ?? new CreateCustomerDto();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 1) Duplikált foglalások egyesítése: a megjegyzések átmentése, majd törlés
            foreach (var (duplicate, keeper) in toRemove)
            {
                keeper.AdminNotes = AppendNote(keeper.AdminNotes, duplicate.AdminNotes);
                keeper.CustomerNotes = AppendNote(keeper.CustomerNotes, duplicate.CustomerNotes);
                if (string.IsNullOrWhiteSpace(keeper.ExtraMaterials)) keeper.ExtraMaterials = duplicate.ExtraMaterials;

                _context.Appointments.Remove(duplicate);
            }

            // 2) A többi foglalás átkötése a megmaradó ügyfélre
            foreach (var appointment in toMove)
            {
                appointment.CustomerId = primary.Id;
            }

            // 3) Felhasználói fiók hivatkozás átvétele (ha a megmaradó ügyfélnek még nincs)
            int? userId = primary.UserId ?? duplicates.FirstOrDefault(d => d.UserId != null)?.UserId;
            foreach (var d in duplicates) d.UserId = null;
            await _context.SaveChangesAsync();

            // 4) Végleges adatok a megmaradó ügyfélen
            var attributes = final.Attributes != null
                ? new Dictionary<string, string>(final.Attributes)
                : new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(final.FullName)) attributes["FullName"] = final.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(final.Phone)) attributes["Phone"] = final.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(final.Email)) attributes["Email"] = final.Email.Trim();
            attributes.Remove("Notes");

            // A rendszer által kezelt receptlista (nem látszik az űrlapon) ne vesszen el: egyesítjük
            if (!attributes.ContainsKey("FormulaList"))
            {
                var mergedFormula = MergeFormulaLists(new[] { primary }.Concat(duplicates));
                if (mergedFormula != null) attributes["FormulaList"] = mergedFormula;
            }

            primary.Attributes = attributes;
            primary.Notes = final.Notes?.Trim();
            primary.UserId = userId;

            // 5) A beolvadt ügyfelek törlése
            _context.CompanyCustomers.RemoveRange(duplicates);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return result;
        }

        private static int GetStatusRank(Soluvion.Domain.Models.Enums.AppointmentStatus status) => status switch
        {
            Soluvion.Domain.Models.Enums.AppointmentStatus.Completed => 5,
            Soluvion.Domain.Models.Enums.AppointmentStatus.Confirmed => 4,
            Soluvion.Domain.Models.Enums.AppointmentStatus.NoShow => 3,
            Soluvion.Domain.Models.Enums.AppointmentStatus.Pending => 2,
            Soluvion.Domain.Models.Enums.AppointmentStatus.Rescheduled => 1,
            _ => 0
        };

        private static string? AppendNote(string? existing, string? incoming)
        {
            if (string.IsNullOrWhiteSpace(incoming)) return existing;
            if (string.IsNullOrWhiteSpace(existing)) return incoming;
            if (existing.Contains(incoming.Trim())) return existing;
            return $"{existing}\n{incoming}";
        }

        /// <summary>
        /// A "FormulaList" (alapértelmezett anyagok) JSON tömbjeinek egyesítése productId alapján;
        /// az első (megmaradó) ügyfél értékei élveznek elsőbbséget.
        /// </summary>
        private static string? MergeFormulaLists(IEnumerable<CompanyCustomer> customers)
        {
            var merged = new List<System.Text.Json.Nodes.JsonNode>();
            var seenProductIds = new HashSet<string>();

            foreach (var customer in customers)
            {
                if (customer.Attributes == null || !customer.Attributes.TryGetValue("FormulaList", out var json) || string.IsNullOrWhiteSpace(json))
                    continue;

                try
                {
                    if (System.Text.Json.Nodes.JsonNode.Parse(json) is not System.Text.Json.Nodes.JsonArray array) continue;
                    foreach (var item in array)
                    {
                        if (item == null) continue;
                        var productId = item["productId"]?.ToString() ?? string.Empty;
                        if (seenProductIds.Add(productId))
                            merged.Add(item.DeepClone());
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Hibás JSON-t kihagyjuk
                }
            }

            return merged.Count == 0 ? null : new System.Text.Json.Nodes.JsonArray(merged.ToArray()).ToJsonString();
        }
    }
}
