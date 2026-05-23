namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserCreditLedgerConfiguration : IEntityTypeConfiguration<UserCreditLedger>
{
    public void Configure(EntityTypeBuilder<UserCreditLedger> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(10, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(10, 2);
        builder.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("VND").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(0);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasCheckConstraint(
            "CK_UserCreditLedger_Amount_NonNegative",
            "\"Amount\" >= 0");

        builder.ToTable("UserCreditLedgers");
    }
}

