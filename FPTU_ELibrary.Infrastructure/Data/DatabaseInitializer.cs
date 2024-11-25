using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using BookCategoryEnum = FPTU_ELibrary.Domain.Common.Enums.BookCategory;
using BookCategoryEntity = FPTU_ELibrary.Domain.Entities.BookCategory;
using System.ComponentModel;
using System.Reflection;

namespace FPTU_ELibrary.Infrastructure.Data
{
	//	Summary:
	//		This class is to initialize database and seeding default data for the application
	public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly FptuElibraryDbContext _context;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(ILogger<DatabaseInitializer> logger,
            FptuElibraryDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        //  Summary:
        //      Try to initialize database and its table if not exist
        public async Task InitializeAsync()
        {
            try
            {
                // Check whether the database exists and can be connected to
                if (!await _context.Database.CanConnectAsync())
                {
                    // Check for applied migrations
                    var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
                    if (appliedMigrations.Any())
                    {
                        _logger.LogInformation("Migrations have been applied.");
                        return;
                    }

                    // Perform migration if necessary
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Database initialized successfully.");
                }
                else
                {
                    _logger.LogInformation("Database cannot be connected to.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }

        //  Summary:
        //      Progress seeding data
        public async Task SeedAsync()
        {
            try
            {
                await TrySeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        //  Summary:
        //      Perform seeding data
        public async Task TrySeedAsync()
        {
            try
            {
				// [System Roles]
				if (!await _context.SystemRoles.AnyAsync()) await SeedSystemRoleAsync();
				else _logger.LogInformation("Already seed data for table {0}", "System_Role");

				// [Job Roles]
				if (!await _context.JobRoles.AnyAsync()) await SeedJobRoleAsync();
				else _logger.LogInformation("Already seed data for table {0}", "Job_Role");

				// [Book Categories]
				if (!await _context.BookCategories.AnyAsync()) await SeedBookCategory();
				else _logger.LogInformation("Already seed data for table {0}", "Book_Category");

				// [Employees]
				if (!await _context.Employees.AnyAsync()) await SeedEmployeeAsync();
				else _logger.LogInformation("Already seed data for table {0}", "Employee");

				// [Books]
				if (!await _context.Books.AnyAsync()) await SeedBookAsync();
				else _logger.LogInformation("Already seed data for table {0}", "Book");
			}
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while performing seed data.");
                throw;
            }
        }

		//  Summary:
		//      Seeding System role
		private async Task SeedSystemRoleAsync()
		{
			// Initialize system role
			List<Domain.Entities.SystemRole> roles = new()
			{
				new()
				{
					EnglishName = nameof(Domain.Common.Enums.Role.Administration).ToString(),
					VietnameseName = Domain.Common.Enums.Role.Administration.GetDescription()
				},
				new()
				{
					EnglishName = nameof(Domain.Common.Enums.Role.Teacher).ToString(),
					VietnameseName = Domain.Common.Enums.Role.Teacher.GetDescription()
				},
				new()
				{
					EnglishName = nameof(Domain.Common.Enums.Role.Student).ToString(),
					VietnameseName = Domain.Common.Enums.Role.Student.GetDescription()
				},
				new()
				{
					EnglishName = nameof(Domain.Common.Enums.Role.GeneralMember).ToString(),
					VietnameseName = Domain.Common.Enums.Role.GeneralMember.GetDescription()
				}
			};
		
			// Add range
			await _context.SystemRoles.AddRangeAsync(roles);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.LogInformation("Seed system role successfully.");
		}

		//  Summary:
		//      Seeding Job role
		private async Task SeedJobRoleAsync()
		{
			// Intialize job role 
			List<JobRole> jobRoles = new()
			{
				new()
				{
					EnglishName = nameof(JobTitle.HeadLibrarian).ToString(),
					VietnameseName = JobTitle.HeadLibrarian.GetDescription()
				},
				new()
				{
					EnglishName = nameof(JobTitle.LibraryManager).ToString(),
					VietnameseName = JobTitle.LibraryManager.GetDescription()
				},
				new()
				{
					EnglishName = nameof(JobTitle.Librarian).ToString(),
					VietnameseName = JobTitle.Librarian.GetDescription()
				},
				new()
				{
					EnglishName = nameof(JobTitle.LibraryAssistant).ToString(),
					VietnameseName = JobTitle.LibraryAssistant.GetDescription()
				},
				new()
				{
					EnglishName = nameof(JobTitle.TemporaryWorker).ToString(),
					VietnameseName = JobTitle.TemporaryWorker.GetDescription()
				}
			};

			// Add range
			await _context.JobRoles.AddRangeAsync(jobRoles);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.LogInformation("Seed job role successfully.");
		}

		//  Summary:
		//      Seeding Book Category
		private async Task SeedBookCategory()
        {
			// Initialize book category entities
            List<BookCategoryEntity> bookCategories = new()
            {
                new()
                {
                    EnglishName = nameof(BookCategoryEnum.Mystery).ToString(),
                    VietnameseName = BookCategoryEnum.Mystery.GetDescription()
                },
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Romance).ToString(),
					VietnameseName = BookCategoryEnum.Romance.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.FantasyAndScienceFiction).ToString(),
					VietnameseName = BookCategoryEnum.FantasyAndScienceFiction.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.ThrillerAndHorror).ToString(),
					VietnameseName = BookCategoryEnum.ThrillerAndHorror.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.ShortStories).ToString(),
					VietnameseName = BookCategoryEnum.ShortStories.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Biography).ToString(),
					VietnameseName = BookCategoryEnum.Biography.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.CookBooks).ToString(),
					VietnameseName = BookCategoryEnum.CookBooks.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Essays).ToString(),
					VietnameseName = BookCategoryEnum.Essays.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.SelfHelp).ToString(),
					VietnameseName = BookCategoryEnum.SelfHelp.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.History).ToString(),
					VietnameseName = BookCategoryEnum.History.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Poetry).ToString(),
					VietnameseName = BookCategoryEnum.Poetry.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Children).ToString(),
					VietnameseName = BookCategoryEnum.Children.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.BusinessAndInvesting).ToString(),
					VietnameseName = BookCategoryEnum.BusinessAndInvesting.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Education).ToString(),
					VietnameseName = BookCategoryEnum.Education.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Politics).ToString(),
					VietnameseName = BookCategoryEnum.Politics.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.ReligionAndSpirituality).ToString(),
					VietnameseName = BookCategoryEnum.ReligionAndSpirituality.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.LifeSkills).ToString(),
					VietnameseName = BookCategoryEnum.LifeSkills.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.HealthAndWellness).ToString(),
					VietnameseName = BookCategoryEnum.HealthAndWellness.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.ScienceAndTechnology).ToString(),
					VietnameseName = BookCategoryEnum.ScienceAndTechnology.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Novels).ToString(),
					VietnameseName = BookCategoryEnum.Novels.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.TravelAndGeography).ToString(),
					VietnameseName = BookCategoryEnum.TravelAndGeography.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Humor).ToString(),
					VietnameseName = BookCategoryEnum.Humor.GetDescription()
				}
			};
        
			// Add range
			await _context.BookCategories.AddRangeAsync(bookCategories);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.LogInformation("Seed book category success.");
		}

        //  Summary:
        //      Seeding Book
        private async Task SeedBookAsync()
        {
			// Get librian
			var librian = await _context.Employees
				.Include(x => x.JobRole)
				.FirstOrDefaultAsync(e => e.JobRole.EnglishName == JobTitle.Librarian.ToString());

			// Get book categories
			var categories = await _context.BookCategories.ToListAsync();

			if(librian == null || !categories.Any())
			{
				_logger.LogError("Not found any librian or book category to process seeding book");
				return;
			}

			// Random 
			var rnd = new Random();
			
			// Initialize books
            List<Book> books = new()
			{
				new Book
				{
					Title = "Harry Potter and the Sorcerer's Stone",
					Summary = "A young wizard's journey begins.",
					CanBorrow = true,
					IsDeleted = false,
					IsDraft = false,
					CategoryId = categories.First(x => 
						x.EnglishName == BookCategoryEnum.FantasyAndScienceFiction.ToString()).CategoryId,
					CreateDate = DateTime.Now,
					CreateBy = librian.EmployeeId,
					UpdatedDate = null,
					UpdatedBy = null,
					BookEditions = new List<BookEdition>
					{
						new BookEdition { EditionTitle = "First Edition", EditionNumber = 1, PublicationYear = 1997, PageCount = 309, Language = "English", Isbn = "9780747532699", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId },
						new BookEdition { EditionTitle = "Second Edition", EditionNumber = 2, PublicationYear = 1998, PageCount = 320, Language = "English", Isbn = "9780747538493", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId },
						new BookEdition { EditionTitle = "Third Edition", EditionNumber = 3, PublicationYear = 1999, PageCount = 340, Language = "English", Isbn = "9780747546290", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId },
						new BookEdition { EditionTitle = "Fourth Edition", EditionNumber = 4, PublicationYear = 2000, PageCount = 350, Language = "English", Isbn = "9780747549505", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId },
						new BookEdition { EditionTitle = "Illustrated Edition", EditionNumber = 5, PublicationYear = 2015, PageCount = 256, Language = "English", Isbn = "9780545790352", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId }
					}
				},
				new Book
				{
					Title = "The Hobbit",
					Summary = "A hobbit's adventurous journey to reclaim a lost kingdom.",
					CanBorrow = true,
					IsDeleted = false,
					IsDraft = false,
					CategoryId = categories.First(x =>
						x.EnglishName == BookCategoryEnum.Novels.ToString()).CategoryId,
					CreateDate = DateTime.Now,
					CreateBy = librian.EmployeeId,
					BookEditions = new List<BookEdition>
					{
						new BookEdition { EditionTitle = "First Edition", EditionNumber = 1, PublicationYear = 1937, PageCount = 310, Language = "English", Isbn = "9780547928227", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId },
						new BookEdition { EditionTitle = "Second Edition", EditionNumber = 2, PublicationYear = 1951, PageCount = 320, Language = "English", Isbn = "9780261102217", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId },
						new BookEdition { EditionTitle = "Third Edition", EditionNumber = 3, PublicationYear = 1966, PageCount = 330, Language = "English", Isbn = "9780395071229", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId },
						new BookEdition { EditionTitle = "Illustrated Edition", EditionNumber = 4, PublicationYear = 1976, PageCount = 340, Language = "English", Isbn = "9780395177112", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId },
						new BookEdition { EditionTitle = "Anniversary Edition", EditionNumber = 5, PublicationYear = 2007, PageCount = 370, Language = "English", Isbn = "9780007262306", CreateDate = DateTime.Now, CreateBy = librian.EmployeeId }
					}
				},
			};

			// Add Range
			await _context.AddRangeAsync(books);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.LogInformation("Seed books successfully.");
		}
    
		//	Summary:
		//		Seeding Employee
		private async Task SeedEmployeeAsync()
		{
			// Get librian job role
			var librianJobRole = await _context.JobRoles.FirstOrDefaultAsync(x => 
				x.EnglishName == JobTitle.Librarian.ToString());

			// Check for role existence
			if(librianJobRole == null)
			{
				_logger.LogError("Not found any librian role to seed Employee");
				return;
			}

			// Initialize employees
			List<Employee> employees = new()
			{
				new()
				{
					EmployeeCode = "EM270925",
					Email = "librian@gmail.com",
					PasswordHash = "$2y$10$b53oQweICAgJnyIKawNmV.x7LKLdsWSd5/ZuSy8l4Za6jt1rnHJrS",
					FirstName = "Nguyen Van",
					LastName = "A",
					Dob = new DateTime(1995, 02, 10),
					Phone = "0777155790",
					Gender = Gender.Male.ToString(),
					HireDate = new DateTime(2023, 10, 10),
					IsActive = true,
					CreateDate = DateTime.UtcNow,
					TwoFactorEnabled = false,
					PhoneNumberConfirmed = false,
					EmailConfirmed = false,
					JobRoleId = librianJobRole.JobRoleId
				}
			};

			// Add Range
			await _context.Employees.AddRangeAsync(employees);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.LogInformation("Seed employees successfully.");
		}

	}

	//	Summary:
	//		Extensions procedures for DatabaseInitializer
	public static class DatabaseInitializerExtensions
	{
		// Enum extensions
		public static string GetDescription(this System.Enum value)
		{
			var field = value.GetType().GetField(value.ToString());
			var attribute = field?.GetCustomAttribute<DescriptionAttribute>();

			return attribute?.Description ?? value.ToString();
		}
	}
}
