using Microsoft.EntityFrameworkCore;
using Soluvion.Domain.Models;

namespace Soluvion.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // 1. Alap Iparágak (CompanyTypes) létrehozása, ha még nincs
            if (!await context.CompanyTypes.AnyAsync())
            {
                context.CompanyTypes.AddRange(
                    new CompanyType { Name = "Fodrászat" },
                    new CompanyType { Name = "Kozmetika" },
                    new CompanyType { Name = "Masszázs" }
                );
                await context.SaveChangesAsync();
            }

            // 2. Iparági Sablonok feltöltése (pl. Fodrászat)
            var fodraszat = await context.CompanyTypes.FirstOrDefaultAsync(c => c.Name == "Fodrászat");

            if (fodraszat != null && !await context.IndustryTemplateAttributes.AnyAsync(t => t.CompanyTypeId == fodraszat.Id))
            {
                var templates = new List<IndustryTemplateAttribute>
                {
                    new IndustryTemplateAttribute
                    {
                        CompanyTypeId = fodraszat.Id,
                        Key = "hair_length",
                        Label = "Hajhossz",
                        DataType = "select",
                        Options = new List<string> { "Rövid", "Félhosszú", "Hosszú" },
                        IsRequired = true,
                        ShowOnPublicBooking = true,
                        IsActive = true
                    },
                    new IndustryTemplateAttribute
                    {
                        CompanyTypeId = fodraszat.Id,
                        Key = "hair_density",
                        Label = "Hajsűrűség",
                        DataType = "select",
                        Options = new List<string> { "Ritka", "Normál", "Sűrű" },
                        IsRequired = false,
                        ShowOnPublicBooking = true,
                        IsActive = true
                    }
                };

                context.CompanyTypes.Update(fodraszat);
                context.IndustryTemplateAttributes.AddRange(templates);
                await context.SaveChangesAsync();
            }

            // 3. Idempotens SuperAdmin (testadmin, Id: 3) beállítás és felesleges admin (Id: 1) / duplikált dolgozó tisztítás
            // Ez biztosítja, hogy az éles (Production) adatbázison is automatikusan és biztonságosan lefusson deploy-kor!
            await NormalizeUsersAndEmployeesAsync(context);
        }

        private static async Task NormalizeUsersAndEmployeesAsync(AppDbContext context)
        {
            // A) Company 7 (Szkáni Szalon): ha van külön CompanyEmployee a testadminnak (UserId=3) és a valódi tulajdonosnak (Kingus, UserId=6),
            // akkor a testadmin CompanyEmployee rekordján lévő foglalásokat átmozgatjuk Kingus CompanyEmployee rekordjára.
            var kingusEmp = await context.CompanyEmployees
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.CompanyId == 7 && e.UserId == 6);

            var testAdminEmps = await context.CompanyEmployees
                .IgnoreQueryFilters()
                .Where(e => e.UserId == 3 || e.UserId == 1)
                .ToListAsync();

            if (kingusEmp != null && testAdminEmps.Count > 0)
            {
                var oldEmpIds = testAdminEmps.Where(e => e.CompanyId == 7).Select(e => e.Id).ToList();
                if (oldEmpIds.Count > 0)
                {
                    var appointmentsToMove = await context.Appointments
                        .IgnoreQueryFilters()
                        .Where(a => a.CompanyId == 7 && oldEmpIds.Contains(a.EmployeeId))
                        .ToListAsync();

                    if (appointmentsToMove.Count > 0)
                    {
                        foreach (var app in appointmentsToMove)
                        {
                            app.EmployeeId = kingusEmp.Id;
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }

            // B) Töröljük a testadminhoz (UserId=3) vagy a régi adminhoz (UserId=1) tartozó CompanyEmployee sorokat,
            // amennyiben nincs már rajtuk foglalás
            if (testAdminEmps.Count > 0)
            {
                foreach (var emp in testAdminEmps)
                {
                    bool hasAnyAppointments = await context.Appointments
                        .IgnoreQueryFilters()
                        .AnyAsync(a => a.EmployeeId == emp.Id);

                    if (!hasAnyAppointments)
                    {
                        context.CompanyEmployees.Remove(emp);
                    }
                }
                await context.SaveChangesAsync();
            }

            // C) Töröljük a felesleges 'admin' (Id: 1) felhasználót
            var legacyAdminUser = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == 1 && u.Username == "admin");

            if (legacyAdminUser != null)
            {
                bool hasEmp = await context.CompanyEmployees
                    .IgnoreQueryFilters()
                    .AnyAsync(e => e.UserId == legacyAdminUser.Id);

                if (!hasEmp)
                {
                    context.Users.Remove(legacyAdminUser);
                    await context.SaveChangesAsync();
                }
            }

            // D) A 'testadmin' (Id: 3) szerepkörét SuperAdmin-ra állítjuk
            var superAdminUser = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == 3 && u.Username == "testadmin");

            if (superAdminUser != null && superAdminUser.Role != "SuperAdmin")
            {
                superAdminUser.Role = "SuperAdmin";
                await context.SaveChangesAsync();
            }

            // E) Minden olyan normál (nem SuperAdmin) Admin felhasználónak, akinek a cégénél még nincs CompanyEmployee rekordja,
            // létrehozzuk az Owner CompanyEmployee összekötést
            var normalUsers = await context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Role != "SuperAdmin" && u.Id != 3 && u.Id != 1 && u.CompanyId > 0)
                .ToListAsync();

            bool addedAnyEmployee = false;
            foreach (var u in normalUsers)
            {
                bool companyExists = await context.Companies
                    .IgnoreQueryFilters()
                    .AnyAsync(c => c.Id == u.CompanyId && !c.IsDeleted);

                if (!companyExists) continue;

                bool empExists = await context.CompanyEmployees
                    .IgnoreQueryFilters()
                    .AnyAsync(e => e.CompanyId == u.CompanyId && e.UserId == u.Id);

                if (!empExists)
                {
                    context.CompanyEmployees.Add(new CompanyEmployee
                    {
                        CompanyId = u.CompanyId,
                        UserId = u.Id,
                        Role = Domain.Models.Enums.EmployeeRole.Owner,
                        IsActive = true
                    });
                    addedAnyEmployee = true;
                }
            }

            if (addedAnyEmployee)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}