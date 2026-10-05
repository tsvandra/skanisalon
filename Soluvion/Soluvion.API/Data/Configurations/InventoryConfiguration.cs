using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Soluvion.Domain.Models;

namespace Soluvion.API.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasOne(p => p.Company)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class InventoryDocumentConfiguration : IEntityTypeConfiguration<InventoryDocument>
    {
        public void Configure(EntityTypeBuilder<InventoryDocument> builder)
        {
            builder.HasOne(d => d.Company)
                .WithMany(c => c.InventoryDocuments)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class InventoryDocumentItemConfiguration : IEntityTypeConfiguration<InventoryDocumentItem>
    {
        public void Configure(EntityTypeBuilder<InventoryDocumentItem> builder)
        {
            builder.HasOne(i => i.InventoryDocument)
                .WithMany(d => d.Items)
                .HasForeignKey(i => i.InventoryDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.Product)
                .WithMany(p => p.InventoryMovements)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ServiceVariantProductConfiguration : IEntityTypeConfiguration<ServiceVariantProduct>
    {
        public void Configure(EntityTypeBuilder<ServiceVariantProduct> builder)
        {
            builder.HasOne(svp => svp.ServiceVariant)
                .WithMany(sv => sv.DefaultProducts)
                .HasForeignKey(svp => svp.ServiceVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(svp => svp.Product)
                .WithMany(p => p.ServiceVariantLinks)
                .HasForeignKey(svp => svp.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class AppointmentUsedProductConfiguration : IEntityTypeConfiguration<AppointmentUsedProduct>
    {
        public void Configure(EntityTypeBuilder<AppointmentUsedProduct> builder)
        {
            builder.HasOne(aup => aup.AppointmentItem)
                .WithMany(ai => ai.UsedProducts)
                .HasForeignKey(aup => aup.AppointmentItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(aup => aup.Product)
                .WithMany() // A Product oldalon nem vezetünk listát az összes felhasznált Appointment-ről, mert túl nagy lehet
                .HasForeignKey(aup => aup.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

