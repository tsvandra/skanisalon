using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking; // FONTOS: Ez kell az összehasonlításhoz
using Soluvion.Domain.Models;
using System.Text.Json;
using Soluvion.API.Interfaces; // FONTOS: Ez kell a JSON kezeléshez

namespace Soluvion.API.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ITenantContext? _tenantContext;

        // FONTOS: Injektáljuk a TenantContext-et (opcionálisként, hogy a migrációk hibátlanul lefussanak)
        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null) : base(options)
        {
            _tenantContext = tenantContext;
        }

        // Segédtulajdonság az EF Core dinamikus lekérdezéseihez
        // Ha nincs tenant (pl. háttérfolyamat vagy migráció), akkor 0-t ad vissza.
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>().HasQueryFilter(c => CurrentTenantId == 0 || c.Id == CurrentTenantId);
            modelBuilder.Entity<CompanyLanguage>().HasQueryFilter(cl => CurrentTenantId == 0 || cl.CompanyId == CurrentTenantId);
            modelBuilder.Entity<Service>().HasQueryFilter(s => CurrentTenantId == 0 || s.CompanyId == CurrentTenantId);
            modelBuilder.Entity<GalleryCategory>().HasQueryFilter(gc => CurrentTenantId == 0 || gc.CompanyId == CurrentTenantId);
            modelBuilder.Entity<UiTranslationOverride>().HasQueryFilter(u => CurrentTenantId == 0 || u.CompanyId == CurrentTenantId);

            modelBuilder.Entity<GalleryImage>().HasQueryFilter(gi => CurrentTenantId == 0 || gi.Category!.CompanyId == CurrentTenantId);
            modelBuilder.Entity<ServiceVariant>().HasQueryFilter(sv => CurrentTenantId == 0 || sv.Service!.CompanyId == CurrentTenantId);

            // --- 1. ÖSSZETETT KULCSOK ---
            modelBuilder.Entity<CompanyLanguage>()
                .HasKey(cl => new { cl.CompanyId, cl.LanguageCode });

            modelBuilder.Entity<UiTranslationOverride>()
                .HasKey(t => new { t.CompanyId, t.LanguageCode, t.TranslationKey });


            // --- 2. JSONB KONVERZIÓK (MANUÁLIS) ---
            // Ez oldja meg a "Reading as Dictionary is not supported" hibát!
            var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToDictionary(entry => entry.Key, entry => entry.Value));

            // --- ServiceVariant ---
            modelBuilder.Entity<ServiceVariant>()
                .Property(v => v.VariantName)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            // --- Service ---
            modelBuilder.Entity<Service>()
                .Property(s => s.Name)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            modelBuilder.Entity<Service>()
                .Property(s => s.Category)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            modelBuilder.Entity<Service>()
                .Property(s => s.Description)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            // --- GalleryImage ---
            modelBuilder.Entity<GalleryImage>()
                .Property(g => g.Title)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            // --- GalleryCategory ---
            modelBuilder.Entity<GalleryCategory>()
                .Property(c => c.Name)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            modelBuilder.Entity<Company>()
                .Property(c => c.OpeningHoursTitle)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            modelBuilder.Entity<Company>()
                .Property(c => c.OpeningHoursDescription)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            modelBuilder.Entity<Company>()
                .Property(c => c.OpeningTimeSlots)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);

            modelBuilder.Entity<Company>()
                .Property(c => c.OpeningExtraInfo)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                    dictionaryComparer);
        }
    }
}