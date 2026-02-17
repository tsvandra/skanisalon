using Microsoft.EntityFrameworkCore;
using Soluvion.API.Models;

namespace Soluvion.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyLanguage> CompanyLanguages { get; set; }
        public DbSet<CompanyType> CompanyTypes { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceVariant> ServiceVariants { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<GalleryCategory> GalleryCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- JSONB MEZŐK KONFIGURÁLÁSA ---
            // Mivel a Program.cs-ben bekapcsoltuk a .EnableDynamicJson()-t,
            // itt elég csak a típust megadni, nem kell kézi konverzió!

            // 1. Szolgáltatás variáns név
            modelBuilder.Entity<ServiceVariant>()
                .Property(v => v.VariantName)
                .HasColumnType("jsonb");

            // 2. Szolgáltatás mezők
            modelBuilder.Entity<Service>()
                .Property(s => s.Name).HasColumnType("jsonb");
            modelBuilder.Entity<Service>()
                .Property(s => s.Category).HasColumnType("jsonb");
            modelBuilder.Entity<Service>()
                .Property(s => s.Description).HasColumnType("jsonb");

            // 3. Galéria mezők
            modelBuilder.Entity<GalleryImage>()
                .Property(g => g.Title).HasColumnType("jsonb");

            modelBuilder.Entity<GalleryCategory>()
                .Property(c => c.Name).HasColumnType("jsonb");
        }
    }
}