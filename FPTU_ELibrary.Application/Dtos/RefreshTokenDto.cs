using FPTU_ELibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Dtos
{
	public class RefreshTokenDto
	{
		// Key
		public int Id { get; set; }

		// Refresh token ID
		public string RefreshTokenId { get; set; } = null!;
		
		// Token ID
		public string TokenId { get; set; } = null!;

		// Creation and expiration datetime
		public DateTime CreateDate { get; set; }
		public DateTime ExpiryDate { get; set; }

		// For specific user
		public Guid? UserId { get; set; }

		// For specific Employee
		public Guid? EmployeeId { get; set; }

		// Refresh Count
		public int RefreshCount { get; set; }
	}
}
