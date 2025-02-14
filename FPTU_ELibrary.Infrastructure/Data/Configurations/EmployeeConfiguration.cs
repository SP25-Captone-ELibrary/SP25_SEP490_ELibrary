using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.HasKey(e => e.EmployeeId).HasName("PK_Employee_EmployeeId");

            builder.ToTable("Employee");

            builder.Property(e => e.EmployeeId)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("employee_id");
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
            builder.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("dob");
            builder.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            builder.Property(e => e.EmailConfirmed)
                .HasDefaultValue(false)
                .HasColumnName("email_confirmed");
			builder.Property(e => e.EmailVerificationCode)
				.HasMaxLength(20)
				.HasColumnName("email_verification_code");
			builder.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .HasColumnName("employee_code");
            builder.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            builder.Property(e => e.Gender)
                .HasMaxLength(50)
                .HasColumnName("gender");
            builder.Property(e => e.HireDate)
                .HasColumnType("datetime")
                .HasColumnName("hire_date");
            builder.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            builder.Property(e => e.RoleId).HasColumnName("role_id");
            builder.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            builder.Property(e => e.ModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("modified_date");
            builder.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            builder.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasColumnName("phone");
            builder.Property(e => e.PhoneNumberConfirmed)
                .HasDefaultValue(false)
                .HasColumnName("phone_number_confirmed");
            builder.Property(e => e.PhoneVerificationCode)
                .HasMaxLength(20)
                .HasColumnName("phone_verification_code");
            builder.Property(e => e.PhoneVerificationExpiry)
                .HasColumnType("datetime")
                .HasColumnName("phone_verification_expiry");
            builder.Property(e => e.TerminationDate)
                .HasColumnType("datetime")
                .HasColumnName("termination_date");
            builder.Property(e => e.TwoFactorBackupCodes)
                .HasMaxLength(255)
                .HasColumnName("two_factor_backup_codes");
            builder.Property(e => e.TwoFactorEnabled)
                .HasDefaultValue(false)
                .HasColumnName("two_factor_enabled");
            builder.Property(e => e.TwoFactorSecretKey)
                .HasMaxLength(255)
                .HasColumnName("two_factor_secret_key");

            builder.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict) // Restrict delete when SystemRole has associated employees
                .HasConstraintName("FK_Employee_RoleId");

            #region Update at 12/06/2024 by Le Xuan Phuoc
            builder.Property(e => e.ModifiedBy)
                .HasColumnType("nvarchar(100)")
                .HasColumnName("modified_by");
            #endregion
            
            #region Update at 12/09/2024 by Le Xuan Phuoc
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            #endregion
        }
    }
}
