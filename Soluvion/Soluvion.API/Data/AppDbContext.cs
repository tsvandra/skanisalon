using Microsoft.EntityFrameworkCore;
using Soluvion.Domain.Models;
using Soluvion.API.Interfaces;
using System.Reflection;

namespace Soluvion.API.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ITenantContext? _tenantContext;

        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null) : base(options)
        {
            _tenantContext = tenantContext;
        }

        public int CurrentTenantId => _tenantContext?.CurrentCompany?.Id ?? 0;

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyLanguage> CompanyLanguages { get; set; }
        public DbSet<CompanyType> CompanyTypes { get; set; }

        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceVariant> ServiceVariants { get; set; }

        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<GalleryCategory> GalleryCategories { get; set; }

        public DbSet<UiTranslationOverride> UiTranslationOverrides { get; set; }
        public DbSet<CompanyEmployee> CompanyEmployees { get; set; }
        public DbSet<CompanyCustomer> CompanyCustomers { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentItem> AppointmentItems { get; set; }
        public DbSet<CompanyAttribute> CompanyAttributes { get; set; }
        public DbSet<IndustryTemplateAttribute> IndustryTemplateAttributes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. GLOBÁLIS TENANT SZŰRŐK (Query Filters) ---
            modelBuilder.Entity<Company>().HasQueryFilter(c => CurrentTenantId == 0 || c.Id == CurrentTenantId);
            modelBuilder.Entity<CompanyLanguage>().HasQueryFilter(cl => CurrentTenantId == 0 || cl.CompanyId == CurrentTenantId);
            modelBuilder.Entity<Service>().HasQueryFilter(s => CurrentTenantId == 0 || s.CompanyId == CurrentTenantId);
            modelBuilder.Entity<GalleryCategory>().HasQueryFilter(gc => CurrentTenantId == 0 || gc.CompanyId == CurrentTenantId);
            modelBuilder.Entity<UiTranslationOverride>().HasQueryFilter(u => CurrentTenantId == 0 || u.CompanyId == CurrentTenantId);

            modelBuilder.Entity<GalleryImage>().HasQueryFilter(gi => CurrentTenantId == 0 || gi.Category!.CompanyId == CurrentTenantId);
            modelBuilder.Entity<ServiceVariant>().HasQueryFilter(sv => CurrentTenantId == 0 || sv.Service!.CompanyId == CurrentTenantId);

            modelBuilder.Entity<CompanyEmployee>().HasQueryFilter(ce => CurrentTenantId == 0 || ce.CompanyId == CurrentTenantId);
            modelBuilder.Entity<CompanyCustomer>().HasQueryFilter(cc => CurrentTenantId == 0 || cc.CompanyId == CurrentTenantId);
            modelBuilder.Entity<Appointment>().HasQueryFilter(a => CurrentTenantId == 0 || a.CompanyId == CurrentTenantId);
            modelBuilder.Entity<AppointmentItem>().HasQueryFilter(ai => CurrentTenantId == 0 || ai.Appointment!.CompanyId == CurrentTenantId);
            modelBuilder.Entity<CompanyAttribute>().HasQueryFilter(ca => CurrentTenantId == 0 || ca.CompanyId == CurrentTenantId);

            // --- 2. KONFIGURÁCIÓK ALKALMAZÁSA AUTÓMATIKUSAN ---
            // Ez a sor megkeresi az Assembly-ben (a projektben) az összes IEntityTypeConfiguration 
            // interfészt implementáló osztályt (amit az EntityConfigurations.cs-ben írtunk), és be is tölti őket!
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}