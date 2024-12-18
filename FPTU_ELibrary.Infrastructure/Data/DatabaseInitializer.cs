using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualBasic.CompilerServices;
using Serilog;

using BookCategory = FPTU_ELibrary.Domain.Entities.BookCategory;
using BookCategoryEnum = FPTU_ELibrary.Domain.Common.Enums.BookCategory;
using SystemFeature = FPTU_ELibrary.Domain.Entities.SystemFeature;
using SystemFeatureEnum = FPTU_ELibrary.Domain.Common.Enums.SystemFeature;

namespace FPTU_ELibrary.Infrastructure.Data
{
	//	Summary:
	//		This class is to initialize database and seeding default data for the application
	public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly FptuElibraryDbContext _context;
        private readonly ILogger _logger;

        public DatabaseInitializer(ILogger logger,
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
                        _logger.Information("Migrations have been applied.");
                        return;
                    }

                    // Perform migration if necessary
                    await _context.Database.MigrateAsync();
                    _logger.Information("Database initialized successfully.");
                }
                else
                {
                    _logger.Information("Database cannot be connected to.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while initializing the database.");
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
                _logger.Error(ex, "An error occurred while seeding the database.");
            }
        }

        //  Summary:
        //      Perform seeding data
        public async Task TrySeedAsync()
        {
            try
            {
	            if (!await _context.Users.AnyAsync()) await SeedUserAsync();
				// [System Roles]
				if (!await _context.SystemRoles.AnyAsync()) await SeedSystemRoleAsync();
				else _logger.Information("Already seed data for table {0}", "System_Role");
		
				// [System Features]
				if (!await _context.SystemFeatures.AnyAsync()) await SeedSystemFeatureAsync();
				else _logger.Information("Already seed data for table {0}", "System_Feature");
				
				// [System Permissions]
				if (!await _context.SystemPermissions.AnyAsync()) await SeedSystemPermissionAsync();
				else _logger.Information("Already seed data for table {0}", "System_Permission");
				
				// [Role Permissions]
				if (!await _context.RolePermissions.AnyAsync()) await SeedRolePermissionAsync();
				else _logger.Information("Already seed data for table {0}", "Role_Permission");
				
				// [Book Categories]
				if (!await _context.BookCategories.AnyAsync()) await SeedBookCategoryAsync();
				else _logger.Information("Already seed data for table {0}", "Book_Category");

				// [Employees]
				if (!await _context.Employees.AnyAsync()) await SeedEmployeeAsync();
				else _logger.Information("Already seed data for table {0}", "Employee");

				// [Books]
				if (!await _context.Books.AnyAsync()) await SeedBookAsync();
				else _logger.Information("Already seed data for table {0}", "Book");
				
				// [Authors]
				if (!await _context.Authors.AnyAsync()) await SeedAuthorAsync();
				else _logger.Information("Already seed data for table {0}", "Author");
			}
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while performing seed data.");
            }
        }

        //  Summary:
		//      Seeding System role
		private async Task SeedSystemRoleAsync()
		{
			// Initialize user roles
			List<SystemRole> userRoles = new()
			{
				new()
				{
					EnglishName = nameof(Role.Administration),
					VietnameseName = Role.Administration.GetDescription(),
					RoleType = nameof(RoleType.User)
				},
				new()
				{
					EnglishName = nameof(Role.Teacher),
					VietnameseName = Role.Teacher.GetDescription(),
					RoleType = nameof(RoleType.User)
				},
				new()
				{
					EnglishName = nameof(Role.Student),
					VietnameseName = Role.Student.GetDescription(),
					RoleType = nameof(RoleType.User)
				},
				new()
				{
					EnglishName = nameof(Role.GeneralMember),
					VietnameseName = Role.GeneralMember.GetDescription(),
					RoleType = nameof(RoleType.User)
				}
			};
		
			// Initialize employee roles
			List<SystemRole> employeeRoles = new()
			{
				new()
				{
					EnglishName = nameof(Role.HeadLibrarian),
					VietnameseName = Role.HeadLibrarian.GetDescription(),
					RoleType = nameof(RoleType.Employee)
				},
				new()
				{
					EnglishName = nameof(Role.LibraryManager),
					VietnameseName = Role.LibraryManager.GetDescription(),
					RoleType = nameof(RoleType.Employee)
				},
				new()
				{
					EnglishName = nameof(Role.Librarian),
					VietnameseName = Role.Librarian.GetDescription(),
					RoleType = nameof(RoleType.Employee)
				},
				new()
				{
					EnglishName = nameof(Role.LibraryAssistant),
					VietnameseName = Role.LibraryAssistant.GetDescription(),
					RoleType = nameof(RoleType.Employee)
				},
				new()
				{
					EnglishName = nameof(Role.TemporaryWorker),
					VietnameseName = Role.TemporaryWorker.GetDescription(),
					RoleType = nameof(RoleType.Employee)
				}
			};
			
			// Add range user roles
			await _context.SystemRoles.AddRangeAsync(userRoles);
			// Add range employee roles
			await _context.SystemRoles.AddRangeAsync(employeeRoles);	
			
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed system role successfully.");
		}
		//  Summary:
		//      Seeding User 
		private async Task SeedUserAsync()
		{
			await Task.CompletedTask;
		}

		//	Summary:
		//		Seeding System Permission
		private async Task SeedSystemPermissionAsync()
		{
			List<SystemPermission> systemPermissions = new()
			{
				new()
				{
					EnglishName = nameof(Permission.FullAccess),
					VietnameseName = Permission.FullAccess.GetDescription(),
					PermissionLevel = (int) PermissionLevel.FullAccess
				},
				new()
				{
					EnglishName = nameof(Permission.View),
					VietnameseName = Permission.View.GetDescription(),
					PermissionLevel = (int) PermissionLevel.View
				},
				new()
				{
					EnglishName = nameof(Permission.Create),
					VietnameseName = Permission.Create.GetDescription(),
					PermissionLevel = (int) PermissionLevel.Create
				},
				new()
				{
					EnglishName = nameof(Permission.Modify),
					VietnameseName = Permission.Modify.GetDescription(),
					PermissionLevel = (int) PermissionLevel.Modify
				},
				new()
				{
					EnglishName = nameof(Permission.AccessDenied),
					VietnameseName = Permission.AccessDenied.GetDescription(),
					PermissionLevel = (int) PermissionLevel.AccessDenied
				}
			};
			
			// Add range employee roles
			await _context.SystemPermissions.AddRangeAsync(systemPermissions);	
			
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed system permission successfully.");
		}
		
		//	Summary:
		//		Seeding System Feature
		private async Task SeedSystemFeatureAsync()
		{
			List<SystemFeature> systemFeatures = new()
			{
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.UserManagement),
					VietnameseName = "Quản lí người dùng"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.EmployeeManagement),
					VietnameseName = "Quản lí nhân viên"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.RoleManagement),
					VietnameseName = "Quản lí role"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.FineManagement),
					VietnameseName = "Quản lí phí"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.BookManagement),
					VietnameseName = "Quản lí sách"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.BorrowManagement),
					VietnameseName = "Quản lí mượn trả sách"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.TransactionManagement),
					VietnameseName = "Quản lí thanh toán"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.SystemConfigurationManagement),
					VietnameseName = "Quản lí cấu hình hệ thống"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.SystemHealthManagement),
					VietnameseName = "Quản lí tình trạng hệ thống"
				}
			};
			
			// Add range employee roles
			await _context.SystemFeatures.AddRangeAsync(systemFeatures);	
			
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed system feature successfully.");
		}
		
	    //	Summary:
	    //		Seeding Role Permission
	    private async Task SeedRolePermissionAsync()
	    {
		    // Get all system features
		    var features = await _context.SystemFeatures.ToListAsync();
		    // Get system roles (Admin & Employee only)
		    var roles = await _context.SystemRoles.Where(sr => 
			    sr.RoleType == nameof(RoleType.Employee) || // All employee roles
			    sr.EnglishName.Equals(nameof(Role.Administration))) // Admin role
			.ToListAsync();
		    
		    // Get [FULL_ACCESS] permission
		    var fullAccessPermission = await _context.SystemPermissions.
				    FirstOrDefaultAsync(p => p.EnglishName == nameof(Permission.FullAccess));
			// Get [ACCESS_DENIED] permission
			var accessDeniedPermission = await _context.SystemPermissions.
				FirstOrDefaultAsync(p => p.EnglishName == nameof(Permission.AccessDenied));
		    
		    // Initialize list of permission
		    List<RolePermission> rolePermissions = new();
		    for (int i = 0; i < features.Count; ++i)
		    {
			    var feature = features[i];
			    for (int j = 0; j < roles.Count; ++j)
			    {
				    var role = roles[j];

				    // Set default [ACCESS_DENIED] for all roles except Admin
				    if (feature.EnglishName.Equals(nameof(SystemFeatureEnum.RoleManagement)))
				    {
					    // Is not Admin
					    if(!role.EnglishName.Equals(nameof(Role.Administration)))
					    {
						    // Add role permission
						    rolePermissions.Add(new()
						    {
							    PermissionId = accessDeniedPermission!.PermissionId, // Access Denied
							    FeatureId = feature.FeatureId,
							    RoleId = role.RoleId
						    });
						    
						    // Mark as continue
						    continue;
					    }
				    }
				    
				    // Add role permission
				    rolePermissions.Add(new()
				    {
					    PermissionId = fullAccessPermission!.PermissionId, // Full Access 
					    FeatureId = feature.FeatureId,
					    RoleId = role.RoleId
				    });
			    }
		    }
		    
		    // Add range employee roles
		    await _context.RolePermissions.AddRangeAsync(rolePermissions);	
			
		    var saveSucc = await _context.SaveChangesAsync() > 0;

		    if (saveSucc) _logger.Information("Seed role permissions successfully.");
	    }
		
		//  Summary:
		//      Seeding Book Category
		private async Task SeedBookCategoryAsync()
        {
			// Initialize book category entities
            List<BookCategory> bookCategories = new()
            {
                new()
                {
                    EnglishName = nameof(BookCategoryEnum.Mystery),
                    VietnameseName = BookCategoryEnum.Mystery.GetDescription()
                },
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Romance),
					VietnameseName = BookCategoryEnum.Romance.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.FantasyAndScienceFiction),
					VietnameseName = BookCategoryEnum.FantasyAndScienceFiction.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.ThrillerAndHorror),
					VietnameseName = BookCategoryEnum.ThrillerAndHorror.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.ShortStories),
					VietnameseName = BookCategoryEnum.ShortStories.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Biography),
					VietnameseName = BookCategoryEnum.Biography.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.CookBooks),
					VietnameseName = BookCategoryEnum.CookBooks.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Essays),
					VietnameseName = BookCategoryEnum.Essays.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.SelfHelp),
					VietnameseName = BookCategoryEnum.SelfHelp.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.History),
					VietnameseName = BookCategoryEnum.History.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Poetry),
					VietnameseName = BookCategoryEnum.Poetry.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Children),
					VietnameseName = BookCategoryEnum.Children.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.BusinessAndInvesting),
					VietnameseName = BookCategoryEnum.BusinessAndInvesting.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Education),
					VietnameseName = BookCategoryEnum.Education.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Politics),
					VietnameseName = BookCategoryEnum.Politics.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.ReligionAndSpirituality),
					VietnameseName = BookCategoryEnum.ReligionAndSpirituality.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.LifeSkills),
					VietnameseName = BookCategoryEnum.LifeSkills.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.HealthAndWellness),
					VietnameseName = BookCategoryEnum.HealthAndWellness.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.ScienceAndTechnology),
					VietnameseName = BookCategoryEnum.ScienceAndTechnology.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Novels),
					VietnameseName = BookCategoryEnum.Novels.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.TravelAndGeography),
					VietnameseName = BookCategoryEnum.TravelAndGeography.GetDescription()
				},
				new()
				{
					EnglishName = nameof(BookCategoryEnum.Humor),
					VietnameseName = BookCategoryEnum.Humor.GetDescription()
				}
			};
        
			// Add range
			await _context.BookCategories.AddRangeAsync(bookCategories);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed book category success.");
		}

        //  Summary:
        //      Seeding Book
        private async Task SeedBookAsync()
        {
			// Get librarian
			var librarian = await _context.Employees
				.Include(x => x.Role)
				.FirstOrDefaultAsync(e => e.Role.EnglishName == Role.Librarian.ToString());

			// Get book categories
			var categories = await _context.BookCategories.ToListAsync();

			if(librarian == null || !categories.Any())
			{
				_logger.Error("Not found any librian or book category to process seeding book");
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
					CreateBy = librarian.EmployeeId,
					UpdatedDate = null,
					UpdatedBy = null,
					BookEditions = new List<BookEdition>
					{
						new() { EditionTitle = "First Edition", EditionNumber = 1, PublicationYear = 1997, PageCount = 309, Language = "English", Isbn = "9780747532699", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId },
						new() { EditionTitle = "Second Edition", EditionNumber = 2, PublicationYear = 1998, PageCount = 320, Language = "English", Isbn = "9780747538493", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId },
						new() { EditionTitle = "Third Edition", EditionNumber = 3, PublicationYear = 1999, PageCount = 340, Language = "English", Isbn = "9780747546290", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId },
						new() { EditionTitle = "Fourth Edition", EditionNumber = 4, PublicationYear = 2000, PageCount = 350, Language = "English", Isbn = "9780747549505", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId },
						new() { EditionTitle = "Illustrated Edition", EditionNumber = 5, PublicationYear = 2015, PageCount = 256, Language = "English", Isbn = "9780545790352", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId }
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
					CreateBy = librarian.EmployeeId,
					BookEditions = new List<BookEdition>
					{
						new() { EditionTitle = "First Edition", EditionNumber = 1, PublicationYear = 1937, PageCount = 310, Language = "English", Isbn = "9780547928227", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId },
						new() { EditionTitle = "Second Edition", EditionNumber = 2, PublicationYear = 1951, PageCount = 320, Language = "English", Isbn = "9780261102217", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId },
						new() { EditionTitle = "Third Edition", EditionNumber = 3, PublicationYear = 1966, PageCount = 330, Language = "English", Isbn = "9780395071229", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId },
						new() { EditionTitle = "Illustrated Edition", EditionNumber = 4, PublicationYear = 1976, PageCount = 340, Language = "English", Isbn = "9780395177112", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId },
						new() { EditionTitle = "Anniversary Edition", EditionNumber = 5, PublicationYear = 2007, PageCount = 370, Language = "English", Isbn = "9780007262306", CreateDate = DateTime.Now, CreateBy = librarian.EmployeeId }
					}
				}
			};

			// Add Range
			await _context.Books.AddRangeAsync(books);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed books successfully.");
		}
    
        //	Summary:
        //		Seeding Author
        private async Task SeedAuthorAsync()
        {
	        List<Author> authors = new()
			{
			    new()
			    {
			        AuthorCode = "AUTH001",
			        AuthorImage = "image1.jpg",
			        FullName = "Jane Doe",
			        Biography = "<p>Jane Doe is a celebrated author known for her thrilling novels.</p>",
			        Dob = new DateTime(1975, 3, 10),
			        Nationality = "American",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = true
			    },
			    new()
			    {
			        AuthorCode = "AUTH002",
			        AuthorImage = "image2.jpg",
			        FullName = "John Smith",
			        Biography = "<p>John Smith has written numerous science fiction classics.</p>",
			        Dob = new DateTime(1980, 5, 15),
			        Nationality = "British",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH003",
			        AuthorImage = "image3.jpg",
			        FullName = "Emily Jones",
			        Biography = "<p>Emily Jones is a poet and novelist with a global following.</p>",
			        Dob = new DateTime(1990, 1, 20),
			        Nationality = "Canadian",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH004",
			        AuthorImage = "image4.jpg",
			        FullName = "Robert Brown",
			        Biography = "<p>Robert Brown specializes in historical fiction.</p>",
			        Dob = new DateTime(1965, 11, 30),
			        Nationality = "Australian",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = true
			    },
			    new()
			    {
			        AuthorCode = "AUTH005",
			        AuthorImage = "image5.jpg",
			        FullName = "Lisa Wilson",
			        Biography = "<p>Lisa Wilson is an award-winning children's book author.</p>",
			        Dob = new DateTime(1988, 7, 5),
			        Nationality = "American",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH006",
			        AuthorImage = "image6.jpg",
			        FullName = "Michael Green",
			        Biography = "<p>Michael Green is known for his compelling mystery novels.</p>",
			        Dob = new DateTime(1972, 4, 25),
			        Nationality = "Irish",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH007",
			        AuthorImage = "image7.jpg",
			        FullName = "Sophia Miller",
			        Biography = "<p>Sophia Miller writes romance novels enjoyed worldwide.</p>",
			        Dob = new DateTime(1995, 2, 12),
			        Nationality = "French",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH008",
			        AuthorImage = "image8.jpg",
			        FullName = "William King",
			        Biography = "<p>William King is a prominent fantasy author.</p>",
			        Dob = new DateTime(1983, 9, 18),
			        Nationality = "Scottish",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH009",
			        AuthorImage = "image9.jpg",
			        FullName = "Elizabeth Hall",
			        Biography = "<p>Elizabeth Hall has authored bestselling biographies.</p>",
			        Dob = new DateTime(1978, 6, 22),
			        Nationality = "New Zealander",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH010",
			        AuthorImage = "image10.jpg",
			        FullName = "David White",
			        Biography = "<p>David White writes award-winning nonfiction works.</p>",
			        Dob = new DateTime(1969, 12, 5),
			        Nationality = "South African",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    }
			};
	        
	        // Add Range
	        await _context.Authors.AddRangeAsync(authors);
	        var saveSucc = await _context.SaveChangesAsync() > 0;

	        if (saveSucc) _logger.Information("Seed authors successfully.");
        }
        
		//	Summary:
		//		Seeding Employee
		private async Task SeedEmployeeAsync()
		{
			// Get librarian job role
			var librarianJobRole = await _context.SystemRoles.FirstOrDefaultAsync(x => 
				x.EnglishName == Role.Librarian.ToString());

			// Check for role existence
			if(librarianJobRole == null)
			{
				_logger.Error("Not found any librarian role to seed Employee");
				return;
			}

			// Initialize employees
			List<Employee> employees = new()
			{
				new()
				{
					EmployeeCode = "EM270925",
					Email = "librian@gmail.com",
					// Password: @Employee123
					PasswordHash = "$2a$13$2bD1T7g/kstNMw0LWEksKuUaGhuAXCXkftsfIURf7yJ6Hr20I2Aae",
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
					RoleId = librarianJobRole.RoleId
				}
			};
			
			for (int i = 1; i <= 10; i++)
            {
                employees.Add(new Employee
                {
                    EmployeeCode = $"EM27092{i}",
                    Email = $"employee{i}@gmail.com",
                    PasswordHash = string.Empty,
                    FirstName = $"First{i}",
                    LastName = $"Last{i}",
                    Dob = new DateTime(1990 + i, 01, i + 1),
                    Phone = $"07771557{i:00}",
                    Gender = i % 2 == 0 ? Gender.Male.ToString() : Gender.Female.ToString(), 
                    HireDate = DateTime.UtcNow.AddDays(-i * 10),
                    IsActive = false,
                    CreateDate = DateTime.UtcNow,
                    TwoFactorEnabled = false,
                    PhoneNumberConfirmed = false,
                    EmailConfirmed = false,
                    RoleId = librarianJobRole.RoleId
                });
            }


			// Add Range
			await _context.Employees.AddRangeAsync(employees);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed employees successfully.");
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
