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

        public DbSet<UiTranslationOverride> UiTranslationOverrides { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. ÖSSZETETT KULCSOK KONFIGURÁLÁSA (HIÁNYZÓ RÉSZEK) ---

            // CompanyLanguage: Egy cégnél egy nyelv csak egyszer szerepelhet
            modelBuilder.Entity<CompanyLanguage>()
                .HasKey(cl => new { cl.CompanyId, cl.LanguageCode });

            // UiTranslationOverride: Egy cégnél, egy nyelven, egy kulcsnak csak egy fordítása lehet
            // EZT HIÁNYOLTA MOST A RENDSZER:
            modelBuilder.Entity<UiTranslationOverride>()
                .HasKey(t => new { t.CompanyId, t.LanguageCode, t.TranslationKey });


            // --- 2. JSONB MEZŐK KONFIGURÁLÁSA ---
            // A Program.cs-ben lévő .EnableDynamicJson() miatt itt elég a típust megadni.

            // Szolgáltatás variáns név
            modelBuilder.Entity<ServiceVariant>()
                .Property(v => v.VariantName).HasColumnType("jsonb");

            // Szolgáltatás mezők
            modelBuilder.Entity<Service>()
                .Property(s => s.Name).HasColumnType("jsonb");
            modelBuilder.Entity<Service>()
                .Property(s => s.Category).HasColumnType("jsonb");
            modelBuilder.Entity<Service>()
                .Property(s => s.Description).HasColumnType("jsonb");

            // Galéria mezők
            modelBuilder.Entity<GalleryImage>()
                .Property(g => g.Title).HasColumnType("jsonb");

            modelBuilder.Entity<GalleryCategory>()
                .Property(c => c.Name).HasColumnType("jsonb");
        }
    }
}