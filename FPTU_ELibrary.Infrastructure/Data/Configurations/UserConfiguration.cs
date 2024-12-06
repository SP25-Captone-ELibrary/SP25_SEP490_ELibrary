using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(e => e.UserId).HasName("PK_User_UserId");

            builder.ToTable("User");

            builder.Property(e => e.UserId)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("user_id");
            builder.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            builder.Property(e => e.Avatar)
                .HasMaxLength(2048)
                .IsUnicode(false)
                .HasColumnName("avatar");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.ModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("modified_date");
            builder.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("dob");
            builder.Property(e => e.Gender)
                .HasMaxLength(50)
                .HasColumnName("gender");
            builder.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            builder.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
			builder.Property(e => e.EmailVerificationCode)
				.HasMaxLength(20)
				.HasColumnName("email_verification_code");
			builder.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            builder.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            builder.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            builder.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            builder.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasColumnName("phone");
            builder.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            builder.Property(e => e.PhoneVerificationCode)
                .HasMaxLength(20)
                .HasColumnName("phone_verification_code");
            builder.Property(e => e.PhoneVerificationExpiry)
                .HasColumnType("datetime")
                .HasColumnName("phone_verification_expiry");
            builder.Property(e => e.RoleId).HasColumnName("role_id");
            builder.Property(e => e.TwoFactorBackupCodes).HasColumnName("two_factor_backup_codes");
            builder.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            builder.Property(e => e.TwoFactorSecretKey)
                .HasMaxLength(255)
                .HasColumnName("two_factor_secret_key");
            builder.Property(e => e.UserCode)
                .HasMaxLength(20)
                .HasColumnName("user_code");

            builder.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict) // Restrict delete when SystemRole has associated employees
                .HasConstraintName("FK_SystemRole_RoleId");
        }
    }
}
