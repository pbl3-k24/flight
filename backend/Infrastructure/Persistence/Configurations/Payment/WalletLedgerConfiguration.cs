using Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Payment;

public class WalletLedgerConfiguration : IEntityTypeConfiguration<WalletLedger>
{
    public void Configure(EntityTypeBuilder<WalletLedger> builder)
    {
        builder.ToTable("wallet_ledger");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.PaymentId).HasColumnName("payment_id");
        builder.Property(l => l.RefundId).HasColumnName("refund_id");
        builder.Property(l => l.UserId).HasColumnName("user_id");
        builder.Property(l => l.EntryType).HasColumnName("entry_type").HasConversion<string>().HasMaxLength(16);
        builder.Property(l => l.Amount).HasColumnName("amount").HasPrecision(18, 0);
        builder.Property(l => l.Currency).HasColumnName("currency").HasMaxLength(8).HasDefaultValue("VND");
        builder.Property(l => l.Description).HasColumnName("description").HasMaxLength(512);
        builder.Property(l => l.Reference).HasColumnName("reference").HasMaxLength(128);
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");
        builder.Property(l => l.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(l => l.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(l => l.UserId).HasDatabaseName("ix_wallet_ledger_user_id");
        builder.HasIndex(l => l.PaymentId).HasDatabaseName("ix_wallet_ledger_payment_id");
        builder.HasIndex(l => l.CreatedAt).HasDatabaseName("ix_wallet_ledger_created_at");
    }
}
