using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications
{
    public class UserSpecification : BaseSpecification<User>
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public UserSpecification(UserSpecParams userSpecParams, int pageIndex, int pageSize)
            : base(e =>
                // Search with terms
                string.IsNullOrEmpty(userSpecParams.Search) ||
                (
                    // Email
                    (!string.IsNullOrEmpty(e.Email) && e.Email.Contains(userSpecParams.Search)) ||
                    // Phone
                    (!string.IsNullOrEmpty(e.Phone) && e.Phone.Contains(userSpecParams.Search)) ||
                    // Address
                    (!string.IsNullOrEmpty(e.Address) && e.Address.Contains(userSpecParams.Search)) ||
                    // Individual FirstName and LastName search
                    (!string.IsNullOrEmpty(e.FirstName) && e.FirstName.Contains(userSpecParams.Search)) ||
                    (!string.IsNullOrEmpty(e.LastName) && e.LastName.Contains(userSpecParams.Search)) ||
                    // Full Name search
                    (!string.IsNullOrEmpty(e.FirstName) &&
                     !string.IsNullOrEmpty(e.LastName) &&
                     (e.FirstName + " " + e.LastName).Contains(userSpecParams.Search))
                ))
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            // Enable split query
            EnableSplitQuery();
            // Include role 
            ApplyInclude(q => q
                .Include(e => e.Role));
            // Default order by first name
            AddOrderBy(e => e.FirstName);
            if (!string.IsNullOrEmpty(userSpecParams.FirstName)) // With first name
            {
                AddFilter(x => x.FirstName == userSpecParams.FirstName);
            }

            if (!string.IsNullOrEmpty(userSpecParams.LastName)) // With last name
            {
                AddFilter(x => x.LastName == userSpecParams.LastName);
            }

            if (!string.IsNullOrEmpty(userSpecParams.Gender)) // With gender
            {
                AddFilter(x => x.Gender == userSpecParams.Gender);
            }

            if (userSpecParams.IsActive != null) // With status
            {
                AddFilter(x => x.IsActive == userSpecParams.IsActive);
            }

            if (userSpecParams.IsDeleted != null) // Is deleted
            {
                AddFilter(x => x.IsDeleted == userSpecParams.IsDeleted);
            }

            if (userSpecParams.DobRange != null
                && userSpecParams.DobRange.Count > 1) // With range of dob
            {
                if (userSpecParams.DobRange[0] is null && userSpecParams.DobRange[1].HasValue)
                {
                    AddFilter(x => x.Dob <= userSpecParams.DobRange[1]);
                }
                else if (userSpecParams.DobRange[0].HasValue && userSpecParams.DobRange[1] is null)
                {
                    AddFilter(x => x.Dob >= userSpecParams.DobRange[0]);
                }
                else
                {
                    AddFilter(x => x.Dob.HasValue &&
                                   x.Dob.Value.Date >= userSpecParams.DobRange[0].Value.Date
                                   && x.Dob.Value.Date <= userSpecParams.DobRange[1].Value.Date);
                }
            }

            // With range of create date like dob
            if (userSpecParams.CreateDateRange != null
                && userSpecParams.CreateDateRange.Count > 1)
            {
                if (userSpecParams.CreateDateRange[0] is null && userSpecParams.CreateDateRange[1].HasValue)
                {
                    AddFilter(x => x.CreateDate <= userSpecParams.CreateDateRange[1]);
                }
                else if (userSpecParams.CreateDateRange[0].HasValue && userSpecParams.CreateDateRange[1] is null)
                {
                    AddFilter(x => x.CreateDate >= userSpecParams.CreateDateRange[0]);
                }
                else
                {
                    AddFilter(x =>
                        x.CreateDate.Date >= userSpecParams.CreateDateRange[0].Value.Date
                        && x.CreateDate.Date <= userSpecParams.CreateDateRange[1].Value.Date);
                }
            }

            // With range of modified date like dob
            if (userSpecParams.ModifiedDateRange != null
                && userSpecParams.ModifiedDateRange.Count > 1)
            {
                if (userSpecParams.ModifiedDateRange[0] is null && userSpecParams.ModifiedDateRange[1].HasValue)
                {
                    AddFilter(x => x.ModifiedDate <= userSpecParams.ModifiedDateRange[1]);
                }
                else if (userSpecParams.ModifiedDateRange[0].HasValue && userSpecParams.ModifiedDateRange[1] is null)
                {
                    AddFilter(x => x.ModifiedDate >= userSpecParams.ModifiedDateRange[0]);
                }
                else
                {
                    AddFilter(x => x.Dob.HasValue &&
                                   x.ModifiedDate!.Value.Date >= userSpecParams.ModifiedDateRange[0].Value.Date
                                   && x.ModifiedDate!.Value.Date <= userSpecParams.ModifiedDateRange[1].Value.Date);
                }
            }


            // Apply Sorting
            if (!string.IsNullOrEmpty(userSpecParams.Sort))
            {
                var sortBy = userSpecParams.Sort.Trim();
                var isDescending = sortBy.StartsWith("-");
                var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

                ApplySorting(propertyName, isDescending);
            }

            AddFilter(x => x.Role.EnglishName != nameof(Role.Administration));
        }

        private void ApplySorting(string propertyName, bool isDescending)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            // Use Reflection to dynamically apply sorting
            var parameter = Expression.Parameter(typeof(User), "x");
            var property = Expression.Property(parameter, propertyName);
            var sortExpression =
                Expression.Lambda<Func<User, object>>(Expression.Convert(property, typeof(object)), parameter);

            if (isDescending)
            {
                AddOrderByDescending(sortExpression);
            }
            else
            {
                AddOrderBy(sortExpression);
            }
        }
    }
}