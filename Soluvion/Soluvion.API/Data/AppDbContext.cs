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

        // --- Raktár DbSets ---
        public DbSet<Product> Products { get; set; }
        public DbSet<InventoryDocument> InventoryDocuments { get; set; }
        public DbSet<InventoryDocumentItem> InventoryDocumentItems { get; set; }
        public DbSet<ServiceVariantProduct> ServiceVariantProducts { get; set; }
        public DbSet<AppointmentUsedProduct> AppointmentUsedProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. GLOBÁLIS TENANT SZŰRŐK (Query Filters) ---
            // Szigorú Multi-Tenant izoláció: kizárólag az aktuálisan feloldott tenant adatai kérhetők le.
            // Ha nincs feloldott tenant (CurrentTenantId == 0), a szűrő 0 rekordot ad vissza, nem szivárogtat adatot!
            modelBuilder.Entity<Company>().HasQueryFilter(c => c.Id == CurrentTenantId);
            modelBuilder.Entity<CompanyLanguage>().HasQueryFilter(cl => cl.CompanyId == CurrentTenantId);
            modelBuilder.Entity<Service>().HasQueryFilter(s => s.CompanyId == CurrentTenantId);
            modelBuilder.Entity<GalleryCategory>().HasQueryFilter(gc => gc.CompanyId == CurrentTenantId);
            modelBuilder.Entity<UiTranslationOverride>().HasQueryFilter(u => u.CompanyId == CurrentTenantId);

            modelBuilder.Entity<GalleryImage>().HasQueryFilter(gi => gi.Category!.CompanyId == CurrentTenantId);
            modelBuilder.Entity<ServiceVariant>().HasQueryFilter(sv => sv.Service!.CompanyId == CurrentTenantId);

            modelBuilder.Entity<CompanyEmployee>().HasQueryFilter(ce => ce.CompanyId == CurrentTenantId);
            modelBuilder.Entity<CompanyCustomer>().HasQueryFilter(cc => cc.CompanyId == CurrentTenantId);
            modelBuilder.Entity<Appointment>().HasQueryFilter(a => a.CompanyId == CurrentTenantId);
            modelBuilder.Entity<AppointmentItem>().HasQueryFilter(ai => ai.Appointment!.CompanyId == CurrentTenantId);
            modelBuilder.Entity<CompanyAttribute>().HasQueryFilter(ca => ca.CompanyId == CurrentTenantId);

            // Raktár Query Filterek
            modelBuilder.Entity<Product>().HasQueryFilter(p => p.CompanyId == CurrentTenantId);
            modelBuilder.Entity<InventoryDocument>().HasQueryFilter(id => id.CompanyId == CurrentTenantId);
            modelBuilder.Entity<InventoryDocumentItem>().HasQueryFilter(idi => idi.InventoryDocument!.CompanyId == CurrentTenantId);
            modelBuilder.Entity<ServiceVariantProduct>().HasQueryFilter(svp => svp.Product!.CompanyId == CurrentTenantId);
            modelBuilder.Entity<AppointmentUsedProduct>().HasQueryFilter(aup => aup.Product!.CompanyId == CurrentTenantId);

            // --- 2. KONFIGURÁCIÓK ALKALMAZÁSA AUTÓMATIKUSAN ---
            // Ez a sor megkeresi az Assembly-ben (a projektben) az összes IEntityTypeConfiguration 
            // interfészt implementáló osztályt (amit az EntityConfigurations.cs-ben írtunk), és be is tölti őket!
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}