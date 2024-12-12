using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications
{
    public class UserSpecification : BaseSpecification<User>
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public UserSpecification(UserSpecParams userSpecParams,int pageIndex, int pageSize)
            : base(BuildCriteria(userSpecParams))
        {
            PageIndex = pageIndex;
            PageSize = pageSize;

            ApplyPaging(pageSize, (pageIndex - 1) * pageSize);

            // Apply Sorting
            if (!string.IsNullOrEmpty(userSpecParams.Sort))
            {
                var sortBy = userSpecParams.Sort.Trim();
                var isDescending = sortBy.StartsWith("-");
                var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

                ApplySorting(propertyName, isDescending);
            }
        }

        private static Expression<Func<User, bool>> BuildCriteria(UserSpecParams userSpecParams)
        {
            // If all filter params are null, return true to fetch all records
            if (string.IsNullOrEmpty(userSpecParams.Search) &&
                string.IsNullOrEmpty(userSpecParams.UserCode) &&
                string.IsNullOrEmpty(userSpecParams.Email) &&
                string.IsNullOrEmpty(userSpecParams.FirstName) &&
                string.IsNullOrEmpty(userSpecParams.LastName) &&
                string.IsNullOrEmpty(userSpecParams.Phone) &&
                string.IsNullOrEmpty(userSpecParams.Address))
            {
                return x => true; // Fetch all records
            }

            // Build filter logic
            return x =>
                (string.IsNullOrEmpty(userSpecParams.Search) ||
                    x.UserCode.Contains(userSpecParams.Search) ||
                    x.Email.Contains(userSpecParams.Search) ||
                    x.FirstName.Contains(userSpecParams.Search) ||
                    x.LastName.Contains(userSpecParams.Search)) &&
                (string.IsNullOrEmpty(userSpecParams.UserCode) || x.UserCode.Contains(userSpecParams.UserCode)) &&
                (string.IsNullOrEmpty(userSpecParams.Email) || x.Email.Contains(userSpecParams.Email)) &&
                (string.IsNullOrEmpty(userSpecParams.FirstName) || x.FirstName.Contains(userSpecParams.FirstName)) &&
                (string.IsNullOrEmpty(userSpecParams.LastName) || x.LastName.Contains(userSpecParams.LastName)) &&
                (string.IsNullOrEmpty(userSpecParams.Phone) || x.Phone.Contains(userSpecParams.Phone)) &&
                (string.IsNullOrEmpty(userSpecParams.Address) || x.Address.Contains(userSpecParams.Address));
        }

        private void ApplySorting(string propertyName, bool isDescending)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            // Use Reflection to dynamically apply sorting
            var parameter = Expression.Parameter(typeof(User), "x");
            var property = Expression.Property(parameter, propertyName);
            var sortExpression = Expression.Lambda<Func<User, object>>(Expression.Convert(property, typeof(object)), parameter);

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
