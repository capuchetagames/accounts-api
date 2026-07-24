using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure.Repository.Configuration;

public class AccountConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x=>x.Id).HasColumnType("uuid").ValueGeneratedOnAdd().HasValueGenerator<GuidValueGenerator>();
        builder.HasIndex(x => x.Cpf).IsUnique();
        builder.Property(x=>x.Cpf).HasColumnType("VARCHAR(11)").IsRequired();
        builder.Property(x => x.Name).HasColumnType("VARCHAR(100)").IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x=>x.Email).HasColumnType("VARCHAR(100)").IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnType("VARCHAR(100)").IsRequired();
        builder.Property(x=>x.Role).HasColumnType("VARCHAR(10)").IsRequired();
        builder.Property(x=>x.IsActive).HasColumnType("BOOLEAN").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnType("uuid").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnType("TIMESTAMP").IsRequired();
        
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull); // se o criador for deletado, não deleta em cascata os criados por ele
        
    }
}