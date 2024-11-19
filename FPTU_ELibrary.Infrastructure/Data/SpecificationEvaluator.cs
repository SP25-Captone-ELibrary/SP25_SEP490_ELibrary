using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Infrastructure.Data
{
    //  Summary:
    //      This class is to specify query conditions, which use when retrieving data 
    public class SpecificationEvaluator<TEntity> 
        where TEntity : class
    {
        public static IQueryable<TEntity> GetQuery(
            IQueryable<TEntity> inputQuery,
            ISpecification<TEntity> spec)
        {
            // Initialize queryable 
            var query = inputQuery.AsQueryable();

            // Query with criteria 
            if(spec.Criteria != null) query = query.Where(spec.Criteria);
            
            // Order 
            if(spec.OrderBy != null) query = query.OrderBy(spec.OrderBy);
            
            // Order by decending
            if(spec.OrderByDescending != null) query = query.OrderByDescending(spec.OrderByDescending);
            
            // Pagination
            if(spec.IsPagingEnabled == true) query = query.Skip(spec.Skip).Take(spec.Take);
            
            // Accumulate queryable allowing to include multiple relation entity
            query = spec.Includes?.Aggregate(query, (current, include) => current.Include(include)) ?? query;  

            return query;
        } 
    }
}
