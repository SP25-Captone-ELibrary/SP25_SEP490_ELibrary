using FPTU_ELibrary.Domain.Specifications.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Domain.Specifications
{
    //  Summary:
    //      This class is to handle query with conditions, filter, order, pagination data 
    public class BaseSpecification<TEntity> : ISpecification<TEntity> where TEntity :class
    {
        // Default constructor
        public BaseSpecification() { }

        // Constructor with specific criteria
        public BaseSpecification(Expression<Func<TEntity, bool>> criteria) => Criteria = criteria;


        #region Query, Filtering, Order Data
        // Allow query with conditions
        public Expression<Func<TEntity, bool>> Criteria { get; } = null!;

        // Include relation tables
        public List<Expression<Func<TEntity, object>>> Includes { get; } = new();

        // Order 
        public Expression<Func<TEntity, object>> OrderBy { get; private set; } = null!;

        // Order by decending 
        public Expression<Func<TEntity, object>> OrderByDescending { get; private set; } = null!;
        #endregion

        #region Pagination
        public int Take { get; private set; }

        public int Skip { get; private set; }

        public bool IsPagingEnabled { get; private set; }

        public bool AsSplitQuery { get; private set; } = false;

        #endregion

        #region Add specification properties
        public void AddInclude(Expression<Func<TEntity, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        public void AddOrderBy(Expression<Func<TEntity, object>> OrderByexpression)
        {
            OrderBy = OrderByexpression;
        }

        public void AddOrderByDecending(Expression<Func<TEntity, object>> OrderByDecending)
        {
            OrderByDescending = OrderByDecending;
        }

        public void ApplyPagging(int take, int skip)
        {
            Take = take;
            Skip = skip;
            IsPagingEnabled = true;
        }
        #endregion

        #region Split Query

        public void AddAsSplitQuery()
        {
            AsSplitQuery = true;
        }        
        #endregion
    }
}
