using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Soluvion.API.Models;
using System.Text.Json;

namespace Soluvion.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceVariant> ServiceVariants { get; set; }
        public DbSet<GalleryCategory> GalleryCategories { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // JSON szerializációs opciók (pl. ékezetes karakterek helyes kezelése)
            var jsonOptions = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            // ValueComparer a Dictionary típushoz, hogy az EF Core észrevegye a belső változásokat is
            var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToDictionary(entry => entry.Key, entry => entry.Value));

            // --- Service konfiguráció ---
            modelBuilder.Entity<Service>(entity =>
            {
                // Name -> JSONB
                entity.Property(e => e.Name)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
                    .Metadata.SetValueComparer(dictionaryComparer);

                // Category -> JSONB
                entity.Property(e => e.Category)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
                    .Metadata.SetValueComparer(dictionaryComparer);

                // Description -> JSONB
                entity.Property(e => e.Description)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
                    .Metadata.SetValueComparer(dictionaryComparer);
            });

            // --- GalleryCategory konfiguráció ---
            modelBuilder.Entity<GalleryCategory>(entity =>
            {
                // Name -> JSONB
                entity.Property(e => e.Name)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
                    .Metadata.SetValueComparer(dictionaryComparer);
            });
        }
    }
}