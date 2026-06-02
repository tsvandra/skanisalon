using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Soluvion.Domain.Models;

namespace Soluvion.API.Data.Configurations
{
    // --- 1. ÖSSZETETT KULCSOK ---
    public class CompanyLanguageConfiguration : IEntityTypeConfiguration<CompanyLanguage>
    {
        public void Configure(EntityTypeBuilder<CompanyLanguage> builder) =>
            builder.HasKey(cl => new { cl.CompanyId, cl.LanguageCode });
    }

    public class UiTranslationOverrideConfiguration : IEntityTypeConfiguration<UiTranslationOverride>
    {
        public void Configure(EntityTypeBuilder<UiTranslationOverride> builder) =>
            builder.HasKey(t => new { t.CompanyId, t.LanguageCode, t.TranslationKey });
    }

    // --- 2. RELÁCIÓK ÉS JSONB MEZŐK ENTITÁSONKÉNT ---
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.Property(c => c.OpeningHoursTitle).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
            builder.Property(c => c.OpeningHoursDescription).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
            builder.Property(c => c.OpeningTimeSlots).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
            builder.Property(c => c.OpeningExtraInfo).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
        }
    }

    public class CompanyEmployeeConfiguration : IEntityTypeConfiguration<CompanyEmployee>
    {
        public void Configure(EntityTypeBuilder<CompanyEmployee> builder)
        {
            builder.HasOne(ce => ce.Company).WithMany(c => c.Employees).HasForeignKey(ce => ce.CompanyId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(ce => ce.User).WithMany().HasForeignKey(ce => ce.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class CompanyCustomerConfiguration : IEntityTypeConfiguration<CompanyCustomer>
    {
        public void Configure(EntityTypeBuilder<CompanyCustomer> builder)
        {
            builder.HasOne(cc => cc.Company).WithMany(c => c.Customers).HasForeignKey(cc => cc.CompanyId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(cc => cc.User).WithMany().HasForeignKey(cc => cc.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(c => c.Attributes).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
        }
    }

    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.HasOne(a => a.Company).WithMany(c => c.Appointments).HasForeignKey(a => a.CompanyId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class AppointmentItemConfiguration : IEntityTypeConfiguration<AppointmentItem>
    {
        public void Configure(EntityTypeBuilder<AppointmentItem> builder)
        {
            builder.HasOne(ai => ai.Appointment).WithMany(a => a.Items).HasForeignKey(ai => ai.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(ai => ai.ServiceVariant).WithMany().HasForeignKey(ai => ai.ServiceVariantId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class CompanyAttributeConfiguration : IEntityTypeConfiguration<CompanyAttribute>
    {
        public void Configure(EntityTypeBuilder<CompanyAttribute> builder)
        {
            builder.HasOne(ca => ca.Company).WithMany().HasForeignKey(ca => ca.CompanyId).OnDelete(DeleteBehavior.Cascade);
            builder.Property(ca => ca.Options).HasColumnType("jsonb").HasConversion(JsonConversionHelper.ListStringConverter, JsonConversionHelper.ListStringComparer);
        }
    }

    public class IndustryTemplateAttributeConfiguration : IEntityTypeConfiguration<IndustryTemplateAttribute>
    {
        public void Configure(EntityTypeBuilder<IndustryTemplateAttribute> builder)
        {
            builder.HasOne(ita => ita.CompanyType).WithMany(ct => ct.TemplateAttributes).HasForeignKey(ita => ita.CompanyTypeId).OnDelete(DeleteBehavior.Cascade);
            builder.Property(ita => ita.Options).HasColumnType("jsonb").HasConversion(JsonConversionHelper.ListStringConverter, JsonConversionHelper.ListStringComparer);
        }
    }

    public class ServiceVariantConfiguration : IEntityTypeConfiguration<ServiceVariant>
    {
        public void Configure(EntityTypeBuilder<ServiceVariant> builder)
        {
            builder.Property(v => v.VariantName).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
            builder.Property(v => v.ProfileModifiers).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
        }
    }

    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.Property(s => s.Name).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
            builder.Property(s => s.Category).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
            builder.Property(s => s.Description).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
        }
    }

    public class GalleryImageConfiguration : IEntityTypeConfiguration<GalleryImage>
    {
        public void Configure(EntityTypeBuilder<GalleryImage> builder) =>
            builder.Property(g => g.Title).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
    }

    public class GalleryCategoryConfiguration : IEntityTypeConfiguration<GalleryCategory>
    {
        public void Configure(EntityTypeBuilder<GalleryCategory> builder) =>
            builder.Property(c => c.Name).HasColumnType("jsonb").HasConversion(JsonConversionHelper.DictionaryConverter, JsonConversionHelper.DictionaryComparer);
    }
}