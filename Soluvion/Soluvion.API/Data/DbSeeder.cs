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
        }
    }
}