using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Security.Claims;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using BookCategory = FPTU_ELibrary.Domain.Entities.BookCategory;
using SystemFeature = FPTU_ELibrary.Domain.Entities.SystemFeature;

namespace FPTU_ELibrary.Infrastructure.Data.Context;

public class FptuElibraryDbContext : DbContext
{
	private readonly IHttpContextAccessor _contextAccessor;
	public FptuElibraryDbContext() { }

    public FptuElibraryDbContext(DbContextOptions<FptuElibraryDbContext> options, IHttpContextAccessor contextAccessor)
	    : base(options)
    {
	    _contextAccessor = contextAccessor;
    }

    public DbSet<AuditTrail> AuditTrails { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<BookCategory> BookCategories { get; set; }
    public DbSet<BookEdition> BookEditions { get; set; }
    public DbSet<BookEditionCopy> BookEditionCopies { get; set; }
    public DbSet<BookEditionInventory> BookEditionInventories { get; set; }
    public DbSet<BookResource> BookResources { get; set; }
    public DbSet<BookReview> BookReviews { get; set; }
    public DbSet<BorrowRecord> BorrowRecords { get; set; }
    public DbSet<BorrowRequest> BorrowRequests { get; set; }
    public DbSet<CopyConditionHistory> CopyConditionHistories { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Fine> Fines { get; set; }
    public DbSet<FinePolicy> FinePolicies { get; set; }
    public DbSet<LibraryFloor> LibraryFloors { get; set; }
    public DbSet<LibraryPath> LibraryPaths { get; set; }
    public DbSet<LibrarySection> LibrarySections { get; set; }
    public DbSet<LibraryShelf> LibraryShelves { get; set; }
    public DbSet<LibraryZone> LibraryZones { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationRecipient> NotificationRecipients { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<SystemRole> SystemRoles { get; set; }
    public DbSet<SystemFeature> SystemFeatures { get; set; }
    public DbSet<SystemPermission> SystemPermissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserFavorite> UserFavorites { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder.UseSqlServer(GetConnectionString(), o
           		    => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

	private string GetConnectionString()
	{
		IConfigurationBuilder builder = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			.AddEnvironmentVariables();

		string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? null!;

		if (!string.IsNullOrEmpty(environment))
		{
			builder.AddJsonFile($"appsettings.{environment}.json");
		}

		IConfiguration configuration = builder.Build();

		return configuration.GetConnectionString("DefaultConnectionStr")!;
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

	// Declare for constant variable
	private const string SystemSource = "system";
	
	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		// Try to retrieve user email from claims
		var userEmail = _contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
		
		// Set auditable properties (createdAt, updatedAt, createdBy, updatedBy)
		SetAuditableProperties(userEmail);

		var auditEntries = HandleAuditingBeforeSaveChanges(userEmail).ToList();
		if (auditEntries.Count > 0)
		{
			await AuditTrails.AddRangeAsync(auditEntries, cancellationToken);
		}
		
		return await base.SaveChangesAsync(cancellationToken);
	}
	
	/// <summary>
	/// Handle create audit logs for specific entities before performing save changes
	/// </summary>
	/// <param name="email"></param>
	/// <returns></returns>
	private List<AuditTrail> HandleAuditingBeforeSaveChanges(string? email)
	{
		var auditableEntries = ChangeTracker.Entries<IAuditableEntity>()
			.Where(e => (e.State == EntityState.Added || // Added action 
			             e.State == EntityState.Modified || // Modified action
			             e.State == EntityState.Deleted) && // Deleted action
			            IsAuditEnabledEntity(e.Entity.GetType())) // Only with specific entity
			.Select(x => CreateTrailEntry(email, x))
			.ToList();

		return auditableEntries;
	}
	
	/// <summary>
	/// Create trail entry
	/// </summary>
	/// <param name="email"></param>
	/// <param name="entry"></param>
	/// <returns></returns>
	private static AuditTrail CreateTrailEntry(string? email, EntityEntry<IAuditableEntity> entry)
    {
	    // Current local datetime
	    var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
		    // Vietnam timezone
		    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
	    
        var trailEntry = new AuditTrail
        {
    	    EntityName = entry.Entity.GetType().Name,
    	    Email = !string.IsNullOrEmpty(email) ? email : SystemSource,
    	    DateUtc = currentLocalDateTime
        };

        SetAuditTrailPropertyValues(entry, trailEntry);
        SetAuditTrailNavigationValues(entry, trailEntry);
        SetAuditTrailReferenceValues(entry, trailEntry);

        return trailEntry;
    }

	/// <summary>
	/// Sets column values to audit trail entity
	/// </summary>
	/// <param name="entry">Current entity entry ef core model</param>
	/// <param name="trailEntry">Audit trail entity</param>
	private static void SetAuditTrailPropertyValues(EntityEntry entry, AuditTrail trailEntry)
	{
		// Skip temp fields (that will be assigned automatically by ef core engine, for example: when inserting an entity)
		foreach (var property in entry.Properties.Where(x => !x.IsTemporary))
		{
			if (property.Metadata.IsPrimaryKey())
			{
				trailEntry.EntityId = property.CurrentValue?.ToString();
				continue;
			}

			// Filter properties that should not appear in the audit list
			// if (property.Metadata.Name.Equals("PasswordHash"))
			// {
			// 	continue;
			// }

			SetAuditTrailPropertyValue(entry, trailEntry, property);
		}
	}

	/// <summary>
	/// Sets a property value to the audit trail entity
	/// </summary>
	/// <param name="entry">Current entity entry ef core model</param>
	/// <param name="trailEntry">Audit trail entity</param>
	/// <param name="property">Entity property ef core model</param>
	private static void SetAuditTrailPropertyValue(EntityEntry entry, AuditTrail trailEntry, PropertyEntry property)
	{
		var propertyName = property.Metadata.Name;

		switch (entry.State)
		{
			case EntityState.Added:
				trailEntry.TrailType = TrailType.Added;
				trailEntry.NewValues[propertyName] = property.CurrentValue;

				break;

			case EntityState.Deleted:
				trailEntry.TrailType = TrailType.Deleted;
				trailEntry.OldValues[propertyName] = property.OriginalValue;

				break;

			case EntityState.Modified:
				if (property.IsModified && (property.OriginalValue is null || !property.OriginalValue.Equals(property.CurrentValue)))
				{
					trailEntry.ChangedColumns.Add(propertyName);
					trailEntry.TrailType = TrailType.Modified;
					trailEntry.OldValues[propertyName] = property.OriginalValue;
					trailEntry.NewValues[propertyName] = property.CurrentValue;
				}

				break;
		}

		if (trailEntry.ChangedColumns.Count > 0)
		{
			trailEntry.TrailType = TrailType.Modified;
		}
	}
	
	/// <summary>
	/// Try to set changed columns for navigations of root entity, do not create detail audit logs for relations
	/// </summary>
	/// <param name="entry"></param>
	/// <param name="trailEntry"></param>
	private static void SetAuditTrailNavigationValues(EntityEntry entry, AuditTrail trailEntry)
	{
		// Check whether navigations from entry, which must be a collection and mark as modified
		foreach (var navigation in entry.Navigations.Where(x => x.Metadata.IsCollection && x.IsModified))
		{
			// Get current value of navigation
			if (navigation.CurrentValue is not IEnumerable<object> enumerable)
			{
				// Skip when is not a collection
				continue;
			}

			// Access collection items
			var collection = enumerable.ToList();
			if (collection.Count == 0)  
			{
				// Skip when collection is empty
				continue;
			}

			// Get first item of collection for getting entity name
			var navigationName = collection.First().GetType().Name;
			// Add to change columns
			trailEntry.ChangedColumns.Add(navigationName);
		}
	}
	
	/// <summary>
	/// Try to set changed columns for referencing navigations
	/// </summary>
	/// <param name="entry"></param>
	/// <param name="trailEntry"></param>
	private static void SetAuditTrailReferenceValues(EntityEntry entry, AuditTrail trailEntry)
	{
		// Get references of entry entity, which is non-collection navigation
		foreach (var reference in entry.References.Where(x => x.IsModified))
		{
			// Get reference entity name
			var referenceName = reference.EntityEntry.Entity.GetType().Name;
			// Add to change columns
			trailEntry.ChangedColumns.Add(referenceName);
		}
	}
	
	/// <summary>
	/// Only process add audit log for Entity: Book, BookEdition, BookResource,
	/// CopyConditionHistory, LearningMaterial, SystemRole, RolePermission
	/// </summary>
	/// <param name="entityType"></param>
	/// <returns></returns>
	private bool IsAuditEnabledEntity(Type entityType)
	{
		var auditEnabledEntityTypes = new HashSet<Type>
		{
			typeof(Book), typeof(BookEdition), typeof(BookResource),
			typeof(CopyConditionHistory), typeof(LearningMaterial),
			typeof(SystemRole), typeof(RolePermission)
		};
		return auditEnabledEntityTypes.Contains(entityType);
	}
	
	/// <summary>
	/// Sets auditable properties for entities that are inherited from <see cref="IAuditableEntity"/>
	/// </summary>
	/// <param name="email"></param>
	private void SetAuditableProperties(string? email)
	{
		// Current local datetime
		var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
			// Vietnam timezone
			TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
		
		foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
		{
			switch (entry.State)
			{
				case EntityState.Added:
					entry.Entity.CreatedAt = currentLocalDateTime;
					entry.Entity.CreatedBy = !string.IsNullOrEmpty(email) ? email : SystemSource;
					break;
				case EntityState.Modified:
					entry.Entity.UpdatedAt = currentLocalDateTime;
					entry.Entity.UpdatedBy = !string.IsNullOrEmpty(email) ? email : SystemSource;
					break;
			}
		}
	}
}
