using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(e => e.Id).HasName("Pk_RefreshToken_Id");

            builder.ToTable("Refresh_Token");

            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.ExpiryDate)
                .HasColumnType("datetime")
                .HasColumnName("expiry_date");
            builder.Property(e => e.RefreshTokenId)
	            .HasColumnType("nvarchar(100)")
	            .HasColumnName("refresh_token_id");
            builder.Property(e => e.UserId).HasColumnName("user_id");

            builder.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_RefreshToken_UserId");

			#region Update at: 23-11-2024 by Le Xuan Phuoc
			builder.Property(e => e.EmployeeId).HasColumnName("employee_id");
			builder.HasOne(d => d.Employee).WithMany(p => p.RefreshTokens)
				.HasForeignKey(d => d.EmployeeId)
				.HasConstraintName("FK_RefreshToken_EmployeeId");
			
            builder.ToTable(b => b.HasCheckConstraint("CK_RefreshToken_UserOrEmployee", 
               "(user_id IS NOT NULL AND employee_id IS NULL) OR " +
			   "(user_id IS NULL AND employee_id IS NOT NULL)"));
			#endregion
			
			#region Update at: 26-11-2024 by Le Xuan Phuoc
			builder.Property(e => e.TokenId)
				.HasColumnType("nvarchar(36)")
				.HasColumnName("token_id");
			#endregion
        }
	}
}
