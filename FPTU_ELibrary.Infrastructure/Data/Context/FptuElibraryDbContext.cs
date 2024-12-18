using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace FPTU_ELibrary.Infrastructure.Data.Context;

public partial class FptuElibraryDbContext : DbContext
{
    public FptuElibraryDbContext() { }

    public FptuElibraryDbContext(DbContextOptions<FptuElibraryDbContext> options)
        : base(options) { }

    public DbSet<Author> Authors { get; set; }
    public DbSet<Book> Books { get; set; }
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
}
