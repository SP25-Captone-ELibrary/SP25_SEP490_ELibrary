using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Repositories.Base;
using FPTU_ELibrary.Infrastructure.Data.Context;
using FPTU_ELibrary.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FptuElibraryDbContext _context;
        private Hashtable _repositories;

        public UnitOfWork(FptuElibraryDbContext context)
        {
            _context = context;

            // Initialize repo hastable if not exist
            if (_repositories == null) _repositories = new Hashtable();
        }

        public IGenericRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class
        {
			// Retrieves the name of the entity type
			var type = typeof(TEntity).Name;

			// Checks repository for particular entity is created
			if (!_repositories.ContainsKey(type))
			{
				// Defines the type of the generic repository
				var repositoryType = typeof(GenericRepository<TEntity, TKey>);

				// Creates an instance of the generic repository 
				var repositoryInstance = Activator.CreateInstance(repositoryType, _context);

				// Adds the created repository to the Hashtable
				_repositories.Add(type, repositoryInstance);
			}

			return (IGenericRepository<TEntity, TKey>)_repositories[type]!;
		}

        public int SaveChanges() => _context.SaveChanges();

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public int SaveChangesWithTransaction()
        {
            int result = -1;

            // Starts a new database transaction
            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Saves all changes and commits the transaction if successful
                    result = _context.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch (Exception)
                {
                    // If an exception occurs, the transaction is rolled back
                    result = -1;
                    _context.Database.RollbackTransaction();
                }
            }

            return result;
        }

        public async Task<int> SaveChangesWithTransactionAsync()
        {
            int result = -1;

            // Starts a new database transaction
            using (var dbContextTransaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Saves all changes and commits the transaction if successful
                    result = await _context.SaveChangesAsync();
                    await dbContextTransaction.CommitAsync();
                }
                catch (Exception)
                {
                    // If an exception occurs, the transaction is rolled back
                    result = -1;
                    await _context.Database.RollbackTransactionAsync();
                }
            }

            return result;
        }

        public void Dispose() => _context.Dispose();
    }
}
