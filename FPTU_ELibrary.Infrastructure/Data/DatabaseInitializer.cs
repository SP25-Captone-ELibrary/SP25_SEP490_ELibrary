using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Reflection;
using Serilog;
using SystemFeature = FPTU_ELibrary.Domain.Entities.SystemFeature;
using SystemFeatureEnum = FPTU_ELibrary.Domain.Common.Enums.SystemFeature;

namespace FPTU_ELibrary.Infrastructure.Data
{
	//	Summary:
	//		This class is to initialize database and seeding default data for the application
	public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly ElibraryDbContext _context;
        private readonly ILogger _logger;

        public DatabaseInitializer(ILogger logger,
            ElibraryDbContext context)
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
				if (!await _context.Categories.AnyAsync()) await SeedCategoryAsync();
				else _logger.Information("Already seed data for table {0}", "Book_Category");

				// [Employees]
				if (!await _context.Employees.AnyAsync()) await SeedEmployeeAsync();
				else _logger.Information("Already seed data for table {0}", "Employee");

				// [Users]
				if (!await _context.Users.AnyAsync()) await SeedUserAsync();
				else _logger.Information("Already seed data for table {0}", "User");
				
				// [Authors]
				if (!await _context.Authors.AnyAsync()) await SeedAuthorAsync();
				else _logger.Information("Already seed data for table {0}", "Author");
				
				// [LibraryFloors]
				if (!await _context.LibraryFloors.AnyAsync()) await SeedLibraryFloorAsync();
				else _logger.Information("Already seed data for table {0}", "LibraryFloor");
				
				// [LibraryZones]
				if (!await _context.LibraryZones.AnyAsync()) await SeedLibraryZoneAsync();
				else _logger.Information("Already seed data for table {0}", "LibraryZone");
				
				// [LibrarySections]
				if (!await _context.LibrarySections.AnyAsync()) await SeedLibrarySectionAsync();
				else _logger.Information("Already seed data for table {0}", "LibrarySection");
				
				// [LibraryShelves]
				if (!await _context.LibraryShelves.AnyAsync()) await SeedLibraryShelvesAsync();
				else _logger.Information("Already seed data for table {0}", "LibraryShelf");
				
				// [LibraryItems]
				if (!await _context.LibraryItems.AnyAsync()) await SeedLibraryItemAsync();
				else _logger.Information("Already seed data for table {0}", "LibraryItem");
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
			// Get admin role
			var adminRole = await _context.SystemRoles.FirstOrDefaultAsync(x => 
				x.EnglishName == Role.Administration.ToString());
			
			// Check for role existence
			if(adminRole == null)
			{
				_logger.Error("Not found any admin role to seed User");
				return;
			}
			
			List<User> users = new()
			{
				new()
				{
					Email = "doanvietthanhhs@gmail.com",
					// @Admin123
					PasswordHash = "$2a$13$qUsCGtDD.dTou8YyhK.1YuKNjS7IM25cl/D0vd8EPaV40uvoG/l9u",
					FirstName = "Chube",
					LastName = "Thanh",
					Dob = new DateTime(1995, 02, 10),
					Phone = "099999999",
					Gender = Gender.Male.ToString(),
					IsActive = true,
					CreateDate = DateTime.UtcNow,
					TwoFactorEnabled = false,
					PhoneNumberConfirmed = false,
					EmailConfirmed = true,
					RoleId = adminRole.RoleId 
				},
				new()
				{
					Email = "thanhdvse171867@fpt.edu.vn",
					// @Admin123
					PasswordHash = "$2a$13$qUsCGtDD.dTou8YyhK.1YuKNjS7IM25cl/D0vd8EPaV40uvoG/l9u",
					FirstName = "Chube",
					LastName = "Thanh",
					Dob = new DateTime(1995, 02, 10),
					Phone = "099999999",
					Gender = Gender.Male.ToString(),
					IsActive = true,
					CreateDate = DateTime.UtcNow,
					TwoFactorEnabled = false,
					PhoneNumberConfirmed = false,
					EmailConfirmed = true,
					RoleId = adminRole.RoleId 
				},
				new()
				{
					Email = "kingchenobama711@gmail.com",
					// @Admin123
					PasswordHash = "$2a$13$qUsCGtDD.dTou8YyhK.1YuKNjS7IM25cl/D0vd8EPaV40uvoG/l9u",
					FirstName = "King",
					LastName = "Chen",
					Dob = new DateTime(1995, 02, 10),
					Phone = "099999999",
					Gender = Gender.Male.ToString(),
					IsActive = true,
					CreateDate = DateTime.UtcNow,
					TwoFactorEnabled = false,
					PhoneNumberConfirmed = false,
					EmailConfirmed = true,
					RoleId = adminRole.RoleId 
				},
				new()
				{
					Email = "phuoclxse171957@fpt.edu.vn",
					// @Admin123
					PasswordHash = "$2a$13$qUsCGtDD.dTou8YyhK.1YuKNjS7IM25cl/D0vd8EPaV40uvoG/l9u",
					FirstName = "Xuan",
					LastName = "Phuoc",
					Dob = new DateTime(1995, 02, 10),
					Phone = "099999999",
					Gender = Gender.Male.ToString(),
					IsActive = true,
					CreateDate = DateTime.UtcNow,
					TwoFactorEnabled = false,
					PhoneNumberConfirmed = false,
					EmailConfirmed = true,
					RoleId = adminRole.RoleId 
				}
			};
			
			// Add range employee roles
			await _context.Users.AddRangeAsync(users);	
			
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed users successfully.");
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
					EnglishName = nameof(SystemFeatureEnum.LibraryItemManagement),
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
		private async Task SeedCategoryAsync()
        {
			// Initialize book category entities
            List<Category> categories = new()
            {
                new()
                {
	                Prefix = "SD",
                    EnglishName = nameof(LibraryItemCategory.SingleBook),
                    VietnameseName = LibraryItemCategory.SingleBook.GetDescription()
                },
                new()
                {
	                Prefix = "SB",
	                EnglishName = nameof(LibraryItemCategory.BookSeries),
	                VietnameseName = LibraryItemCategory.BookSeries.GetDescription()
                },
                new()
                {
	                Prefix = "SCN",
	                EnglishName = nameof(LibraryItemCategory.SpecializedBook),
	                VietnameseName = LibraryItemCategory.SpecializedBook.GetDescription()
                },
                new()
                {
	                Prefix = "STK",
	                EnglishName = nameof(LibraryItemCategory.ReferenceBook),
	                VietnameseName = LibraryItemCategory.ReferenceBook.GetDescription()
                },
                new()
                {
	                Prefix = "SNV",
	                EnglishName = nameof(LibraryItemCategory.ProfessionalBook),
	                VietnameseName = LibraryItemCategory.ProfessionalBook.GetDescription()
                },
                new()
                {
	                Prefix = "SVH",
	                EnglishName = nameof(LibraryItemCategory.Literature),
	                VietnameseName = LibraryItemCategory.Literature.GetDescription()
                },
                new()
                {
	                Prefix = "SMV",
	                EnglishName = nameof(LibraryItemCategory.Multimedia),
	                VietnameseName = LibraryItemCategory.Multimedia.GetDescription()
                },
                new()
                {
                    Prefix = "TC",
                    EnglishName = nameof(LibraryItemCategory.Journal),
                    VietnameseName = LibraryItemCategory.Journal.GetDescription()
                },
                new()
                {
	                Prefix = "NC",
	                EnglishName = nameof(LibraryItemCategory.ResearchPaper),
	                VietnameseName = LibraryItemCategory.ResearchPaper.GetDescription()
                },
                new()
                {
	                Prefix = "BC",
	                EnglishName = nameof(LibraryItemCategory.Newspaper),
	                VietnameseName = LibraryItemCategory.Newspaper.GetDescription()
                },
                new()
                {
	                Prefix = "HT",
	                EnglishName = nameof(LibraryItemCategory.LearningSupportMaterial),
	                VietnameseName = LibraryItemCategory.LearningSupportMaterial.GetDescription()
                },
                new()
                {
	                Prefix = "LA",
	                EnglishName = nameof(LibraryItemCategory.AcademicThesis),
	                VietnameseName = LibraryItemCategory.AcademicThesis.GetDescription()
                },
			};
        
			// Add range
			await _context.Categories.AddRangeAsync(categories);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed book category success.");
		}

        //  Summary:
        //      Seeding Library Item
        private async Task SeedLibraryItemAsync()
        {
			// Get librarian
			var librarian = await _context.Employees
				.Include(x => x.Role)
				.FirstOrDefaultAsync(e => e.Role.EnglishName == Role.Librarian.ToString());

			// Get authors
			var authors = await _context.Authors.ToListAsync();
			
			// Get item categories
			var categories = await _context.Categories.ToListAsync();

			if(librarian == null || !categories.Any() || !authors.Any())
			{
				_logger.Error("Not found any librarian, category or author to process seeding book");
				return;
			}
			
			// Get library shelves
			var libraryShelves = await _context.LibraryShelves.ToListAsync();

			if(!libraryShelves.Any())
			{
				_logger.Error("Not found any shelf to process seeding library item");
				return;
			}
			
			// Initialize item group
			var itemGrp1 = new LibraryItemGroup()
			{
				AiTrainingCode = Guid.NewGuid().ToString(),
				ClassificationNumber = "823",
				CutterNumber = "H109P",
				Title = "Harry Potter và phòng chứa bí mật",
				SubTitle = "Harry Potter and the Chamber of Secrets",
				Author = "J. K. Rowling",
				TopicalTerms = "Văn học thiếu nhi, Phép thuật, Phiêu lưu"
			};
			// Add group 
			await _context.LibraryItemGroups.AddAsync(itemGrp1);
			await _context.SaveChangesAsync();
			
			// Random 
			var rnd = new Random();
			// Initialize items
			List<LibraryItem> items = new()
			{
			    new LibraryItem
			    {
			        Title = "Lập Trình C# Cơ Bản",
			        Responsibility = "Nguyễn Văn A",
			        Edition = "Tái bản lần thứ nhất",
			        EditionNumber = 1,
			        Language = "vie",
			        OriginLanguage = "vie",
			        Summary = "Cuốn sách này cung cấp kiến thức nền tảng về lập trình C#, phù hợp cho người mới bắt đầu.",
			        CoverImage = "https://images.nxbbachkhoa.vn/Picture/2024/5/8/image-20240508180323597.jpg",
			        PublicationYear = 2022,
			        Publisher = "Nhà Xuất Bản Khoa Học và Kỹ Thuật",
			        PublicationPlace = "Hà Nội",
			        ClassificationNumber = "005.133",
			        CutterNumber = "NVA",
			        Isbn = "9786041123456",
			        Ean = "8934567890123",
			        EstimatedPrice = 150000M,
			        PageCount = 350,
			        PhysicalDetails = "Bìa mềm, in màu",
			        Dimensions = "16 x 24 cm",
			        Genres = "Lập trình, Công nghệ thông tin",
			        GeneralNote = "Sách có các bài tập thực hành cuối mỗi chương.",
			        BibliographicalNote = "Danh mục tài liệu tham khảo ở cuối sách.",
			        TopicalTerms = "Lập trình, Ngôn ngữ C#, Phát triển phần mềm",
			        AdditionalAuthors = "Trần Thị B",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SpecializedBook)).CategoryId,
			        Status = LibraryItemStatus.Published,
			        CanBorrow = true,
			        IsTrained = false,
			        CreatedAt = DateTime.Now,
			        CreatedBy = librarian.Email
			    },
			    new LibraryItem
			    {
				    Title = "Harry Potter và phòng chứa bí mật",
				    SubTitle = "Harry Potter and the Chamber of Secrets",
				    Responsibility = "J. K. Rowling ; Lý Lan dịch",
				    EditionNumber = 1,
				    Language = "vie",
				    OriginLanguage = "eng",
				    Summary = "Câu chuyện phiêu lưu kỳ thú của Harry Potter tại trường Hogwarts khi khám phá bí mật về căn phòng bí mật.",
				    CoverImage = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTrEKYtbqkEEP9yuSBRp_YL0BtqYo6ptzh2mg&s",
				    PublicationYear = 2024,
				    Publisher = "Nhà Xuất Bản Trẻ",
				    PublicationPlace = "Tp. Hồ Chí Minh",
				    ClassificationNumber = "823",
				    CutterNumber = "H109P",
				    Isbn = "9780395453100",
				    Ean = null,
				    EstimatedPrice = 170000M,
				    PageCount = 429,
				    PhysicalDetails = "In lần thứ 56, bìa cứng",
				    Dimensions = "20 cm",
				    Genres = "Văn học thiếu nhi, Tiểu thuyết",
				    GeneralNote = "Tập 2 trong loạt sách Harry Potter nổi tiếng.",
				    TopicalTerms = "Văn học thiếu nhi, Phép thuật, Phiêu lưu",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SingleBook)).CategoryId,
				    ShelfId = libraryShelves[rnd.Next(libraryShelves.Count)].ShelfId,
			        GroupId = 1,
			        Status = LibraryItemStatus.Published,
				    CanBorrow = true,
				    IsTrained = false,
				    CreatedAt = DateTime.Now,
				    CreatedBy = librarian.Email,
				    LibraryItemInventory = new LibraryItemInventory()
				    {
					    TotalUnits = 5,
					    AvailableUnits = 5,
					    BorrowedUnits = 0,
					    RequestUnits = 0,
					    ReservedUnits = 0
				    },
				    LibraryItemInstances = new List<LibraryItemInstance>()
				    {
					    new()
					    {
						    Barcode = "SD00001",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    },
					    new()
					    {
						    Barcode = "SD00002",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    },
					    new()
					    {
						    Barcode = "SD00003",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    },
					    new()
					    {
						    Barcode = "SD00004",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    },
					    new()
					    {
						    Barcode = "SD00005",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    }
				    },
				    LibraryItemAuthors = new List<LibraryItemAuthor>()
				    {
					    new()
					    {
						    AuthorId = authors.First(a => a.AuthorCode == "AUTH00011").AuthorId
					    }
				    }
			    },
			    new LibraryItem
			    {
				    Title = "Harry Potter và phòng chứa bí mật",
				    SubTitle = "Harry Potter and the Chamber of Secrets",
				    Responsibility = "J. K. Rowling ; Lý Lan dịch",
				    EditionNumber = 2,
				    Language = "vie",
				    OriginLanguage = "eng",
				    Summary = "Câu chuyện phiêu lưu kỳ thú của Harry Potter tại trường Hogwarts khi khám phá bí mật về căn phòng bí mật.",
				    CoverImage = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTrEKYtbqkEEP9yuSBRp_YL0BtqYo6ptzh2mg&s",
				    PublicationYear = 2025,
				    Publisher = "Nhà Xuất Bản Trẻ",
				    PublicationPlace = "Tp. Hồ Chí Minh",
				    ClassificationNumber = "823",
				    CutterNumber = "H109P",
				    Isbn = "9786041243958",
				    Ean = null,
				    EstimatedPrice = 170000M,
				    PageCount = 509,
				    PhysicalDetails = "In lần thứ 56, bìa cứng",
				    Dimensions = "20 cm",
				    Genres = "Văn học thiếu nhi, Tiểu thuyết",
				    GeneralNote = "Tập 2 trong loạt sách Harry Potter nổi tiếng.",
				    TopicalTerms = "Văn học thiếu nhi, Phép thuật, Phiêu lưu",
				    CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SingleBook)).CategoryId,
				    ShelfId = libraryShelves[rnd.Next(libraryShelves.Count)].ShelfId,
				    GroupId = 1,
				    Status = LibraryItemStatus.Published,
				    CanBorrow = true,
				    IsTrained = false,
				    CreatedAt = DateTime.Now,
				    CreatedBy = librarian.Email,
				    LibraryItemInventory = new LibraryItemInventory()
				    {
					    TotalUnits = 5,
					    AvailableUnits = 4,
					    BorrowedUnits = 0,
					    RequestUnits = 0,
					    ReservedUnits = 0
				    },
				    LibraryItemInstances = new List<LibraryItemInstance>()
				    {
					    new()
					    {
						    Barcode = "SD00006",
							Status = nameof(LibraryItemInstanceStatus.OutOfShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    },
					    new()
					    {
						    Barcode = "SD00007",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    },
					    new()
					    {
						    Barcode = "SD00008",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    },
					    new()
					    {
						    Barcode = "SD00009",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    },
					    new()
					    {
						    Barcode = "SD00010",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
						    {
							    new ()
							    {
								    Condition = nameof(LibraryItemConditionStatus.Good)
							    }
						    }
					    }
				    },
				    LibraryItemAuthors = new List<LibraryItemAuthor>()
				    {
					    new()
					    {
						    AuthorId = authors.First(a => a.AuthorCode == "AUTH00011").AuthorId
					    }
				    }
			    },
			    new LibraryItem
			    {
				    Title = "Harry Potter và tên tù nhân ngục Azkaban",
				    SubTitle = "Harry Potter and the Prisoner of Azkaban",
				    Responsibility = "J. K. Rowling ; Lý Lan dịch",
				    Language = "vie",
				    OriginLanguage = "eng",
				    Summary = "Harry Potter tiếp tục những chuyến phiêu lưu và phải đối mặt với Sirius Black, một tù nhân trốn thoát khỏi Azkaban.",
				    CoverImage = "https://images-na.ssl-images-amazon.com/images/S/compressed.photo.goodreads.com/books/1630547330i/5.jpg",
				    PublicationYear = 2025,
				    Publisher = "Nhà Xuất Bản Trẻ",
				    PublicationPlace = "Tp. Hồ Chí Minh",
				    ClassificationNumber = "823",
				    CutterNumber = "H109P",
				    Isbn = "9786041243965",
				    Ean = null,
				    EstimatedPrice = 170000M,
				    PageCount = 433,
				    PhysicalDetails = "In lần thứ 45, bìa mềm",
				    Dimensions = "20 cm",
				    Genres = "Văn học thiếu nhi, Tiểu thuyết",
				    GeneralNote = "Tập 3 trong loạt sách Harry Potter nổi tiếng.",
				    TopicalTerms = "Văn học thiếu nhi, Phép thuật, Phiêu lưu",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SingleBook)).CategoryId,
				    Status = LibraryItemStatus.Published,
				    CanBorrow = true,
				    IsTrained = false,
				    CreatedAt = DateTime.Now,
				    CreatedBy = librarian.Email
			    },
			    new LibraryItem
			    {
				    Title = "Nghĩ Như Một Kẻ Lập Dị",
				    SubTitle = "Think Like a Freak",
				    Responsibility = "Steven D. Levitt và Stephen J. Dubner ; Nguyễn Văn B dịch",
				    Language = "vie",
				    OriginLanguage = "eng",
				    Summary = "Cuốn sách giúp bạn nhìn thế giới theo một cách hoàn toàn khác, sử dụng lối tư duy sáng tạo để giải quyết vấn đề.",
				    CoverImage = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTicekPNc8am2BG70bAgkzooOOBoN7zGL8wAw&s",
				    PublicationYear = 2023,
				    Publisher = "Nhà Xuất Bản Thế Giới",
				    PublicationPlace = "Hà Nội",
				    ClassificationNumber = "330",
				    CutterNumber = "L512T",
				    Isbn = "9786041567892",
				    Ean = "8934567890123",
				    EstimatedPrice = 200000M,
				    PageCount = 320,
				    PhysicalDetails = "Bìa mềm, in màu",
				    Dimensions = "15 x 23 cm",
				    Genres = "Kinh tế học, Tâm lý học ứng dụng",
				    GeneralNote = "Sách được viết bởi hai tác giả của 'Freakonomics'.",
				    TopicalTerms = "Kinh tế, Tư duy sáng tạo, Giải quyết vấn đề",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SingleBook)).CategoryId,
				    Status = LibraryItemStatus.Published,
				    CanBorrow = true,
				    IsTrained = false,
				    CreatedAt = DateTime.Now,
				    CreatedBy = librarian.Email
			    },
			    new LibraryItem
			    {
				    Title = "Đắc Nhân Tâm",
				    SubTitle = "How to Win Friends and Influence People",
				    Responsibility = "Dale Carnegie ; Phạm Văn X dịch",
				    Language = "vie",
				    OriginLanguage = "eng",
				    Summary = "Cuốn sách kinh điển về nghệ thuật giao tiếp và gây ảnh hưởng, giúp bạn đạt được thành công trong cuộc sống.",
				    CoverImage = "https://nxbhcm.com.vn/Image/Biasach/dacnhantam86.jpg",
				    PublicationYear = 2020,
				    Publisher = "Nhà Xuất Bản Văn Hóa",
				    PublicationPlace = "Hà Nội",
				    ClassificationNumber = "158",
				    CutterNumber = "C512D",
				    Isbn = "9786041789456",
				    Ean = "8937890123456",
				    EstimatedPrice = 150000M,
				    PageCount = 400,
				    PhysicalDetails = "Bìa mềm, in màu",
				    Dimensions = "16 x 24 cm",
				    Genres = "Kỹ năng sống, Tâm lý học ứng dụng",
				    GeneralNote = "Sách nổi tiếng và được dịch ra nhiều ngôn ngữ trên thế giới.",
				    TopicalTerms = "Giao tiếp, Kỹ năng sống, Tâm lý học",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SingleBook)).CategoryId,
				    Status = LibraryItemStatus.Published,
				    CanBorrow = true,
				    IsTrained = false,
				    CreatedAt = DateTime.Now,
				    CreatedBy = librarian.Email
			    },
			    new LibraryItem
			    {
			        Title = "Công Nghệ Mới",
			        SubTitle = "Số tháng 01/2023",
			        Responsibility = "Ban Biên Tập",
			        Language = "vie",
			        OriginLanguage = "vie",
			        Summary = "Tạp chí chuyên về những xu hướng công nghệ mới nhất tại Việt Nam và trên thế giới.",
			        CoverImage = "https://media.baotintuc.vn/Upload/XmrgEWAN1PzjhSWqVO54A/files/2024/06/1806/avamegabaochi(1).jpg",
			        PublicationYear = 2023,
			        Publisher = "Nhà Xuất Bản Công Nghệ",
			        PublicationPlace = "TP. Hồ Chí Minh",
			        ClassificationNumber = "006.789",
			        CutterNumber = "BBT",
			        Isbn = null,
			        Ean = "8937890123456",
			        EstimatedPrice = 50000M,
			        PageCount = 120,
			        PhysicalDetails = "Khổ lớn, in màu toàn bộ",
			        Dimensions = "20 x 28 cm",
			        Genres = "Tạp chí, Công nghệ",
			        GeneralNote = "Số chuyên đề về trí tuệ nhân tạo (AI).",
			        TopicalTerms = "AI, Công nghệ mới, Xu hướng 2023",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.Newspaper)).CategoryId,
			        Status = LibraryItemStatus.Published,
			        CanBorrow = false,
			        IsTrained = false,
			        CreatedAt = DateTime.Now,
			        CreatedBy = librarian.Email
			    },
			    new LibraryItem
			    {
			        Title = "Nghiên Cứu Về Biến Đổi Khí Hậu",
			        SubTitle = "Thực Trạng và Giải Pháp Tại Việt Nam",
			        Responsibility = "Đặng Thị C",
			        Language = "vie",
			        OriginLanguage = "vie",
			        Summary = "Nghiên cứu phân tích tác động của biến đổi khí hậu đến môi trường và kinh tế Việt Nam.",
			        CoverImage = "https://luanvanbeta.com/wp-content/uploads/2023/10/tieu-luan-bien-doi-khi-hau-o-viet-nam-06-luanvanbeta.jpg",
			        PublicationYear = 2020,
			        Publisher = "Nhà Xuất Bản Môi Trường",
			        PublicationPlace = "Đà Nẵng",
			        ClassificationNumber = "363.738",
			        CutterNumber = "DTC",
			        Isbn = "9786042123457",
			        Ean = "8935678901234",
			        EstimatedPrice = 200000M,
			        PageCount = 300,
			        PhysicalDetails = "Bìa cứng, minh họa ảnh màu",
			        Dimensions = "15 x 23 cm",
			        Genres = "Môi trường, Nghiên cứu",
			        GeneralNote = "Bao gồm dữ liệu thực tế từ năm 2010 đến 2020.",
			        TopicalTerms = "Biến đổi khí hậu, Bảo vệ môi trường",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.ResearchPaper)).CategoryId,
			        Status = LibraryItemStatus.Published,
			        CanBorrow = true,
			        IsTrained = false,
			        CreatedAt = DateTime.Now,
			        CreatedBy = librarian.Email
			    },
			    new LibraryItem
			    {
			        Title = "Bộ Sách Văn Học Việt Nam Kinh Điển Bộ Sách Văn Học Việt Nam Kinh Điển",
			        SubTitle = "Những Tác Phẩm Bất Hủ Những Tác Phẩm Bất Hủ Những Tác Phẩm Bất Hủ",
			        Responsibility = "Nhiều tác giả",
			        Language = "vie",
			        OriginLanguage = "vie",
			        Summary = "Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ. Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ. Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ. Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ. Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ. Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ. Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ. Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ. Tuyển tập những tác phẩm văn học kinh điển của Việt Nam qua nhiều thế kỷ.",
			        CoverImage = "https://nxbhcm.com.vn/Image/Biasach/sach-van-hoc-viet-nam.jpg",
			        PublicationYear = 2019,
			        Publisher = "Nhà Xuất Bản Văn Học",
			        PublicationPlace = "Hà Nội",
			        ClassificationNumber = "895.922",
			        CutterNumber = "NTA",
			        Isbn = "9786043123458",
			        Ean = "8934567890678",
			        EstimatedPrice = 300000M,
			        PageCount = 1000,
			        PhysicalDetails = "3 tập, bìa cứng",
			        Dimensions = "18 x 25 cm",
			        Genres = "Văn học, Kinh điển",
			        GeneralNote = "Gồm 3 tập với các tác phẩm từ thế kỷ 18 đến thế kỷ 20.",
			        TopicalTerms = "Văn học Việt Nam, Tác phẩm kinh điển",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.Literature)).CategoryId,
			        Status = LibraryItemStatus.Published,
			        CanBorrow = true,
			        IsTrained = false,
			        CreatedAt = DateTime.Now,
			        CreatedBy = librarian.Email
			    }
			};

			// Add range items
			await _context.LibraryItems.AddRangeAsync(items);
			// Save change
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed library items successfully.");
		}
    
        //	Summary:
        //		Seeding Author
        private async Task SeedAuthorAsync()
        {
	        List<Author> authors = new()
			{
			    new()
			    {
			        AuthorCode = "AUTH00001",
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
			        AuthorCode = "AUTH00002",
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
			        AuthorCode = "AUTH00003",
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
			        AuthorCode = "AUTH00004",
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
			        AuthorCode = "AUTH00005",
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
			        AuthorCode = "AUTH00006",
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
			        AuthorCode = "AUTH00007",
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
			        AuthorCode = "AUTH00008",
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
			        AuthorCode = "AUTH00009",
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
			        AuthorCode = "AUTH00010",
			        AuthorImage = "image10.jpg",
			        FullName = "David White",
			        Biography = "<p>David White writes award-winning nonfiction works.</p>",
			        Dob = new DateTime(1969, 12, 5),
			        Nationality = "South African",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new ()
			    {
				    AuthorCode = "AUTH00011",
                    AuthorImage = "https://www.jkrowling.com/wp-content/uploads/2022/05/J.K.-Rowling-2021-Photography-Debra-Hurford-Brown-scaled.jpg",
                    FullName = "Rowling, J. K.",
                    Biography = "Joanne Rowling CH OBE FRSL, known by her pen name J. K. Rowling, is a British author and philanthropist. She wrote Harry Potter, a seven-volume fantasy series published from 1997 to 2007.",
                    Dob = new DateTime(1965, 7, 31),
                    Nationality = "British",
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
					Email = "librarian@gmail.com",
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
		
		//	Summary:
		//		Seeding library floor
		private async Task SeedLibraryFloorAsync()
		{
			List<LibraryFloor> floors = new()
			{
				new()
				{
					FloorNumber = "Floor 1", 
					CreateDate = DateTime.Now
				},
				new()
				{
					FloorNumber = "Floor 2", 
					CreateDate = DateTime.Now
				}
			};
			
			// Add Range
			await _context.LibraryFloors.AddRangeAsync(floors);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed library floors successfully.");
		}
		
		//	Summary:
		//		Seeding Library zone
		private async Task SeedLibraryZoneAsync()
		{
			// Initialize random 
			var rnd = new Random();
			
			// Retrieve all current floor
			var floors = await _context.LibraryFloors.ToListAsync();
			
			List<LibraryZone> zones = new()
			{
				new()
                {
                    FloorId = floors[rnd.Next(floors.Count)].FloorId,
                    ZoneName = "Lounge",
                    XCoordinate = 10.5,
                    YCoordinate = 20.3,
                    CreateDate = DateTime.Now,
                    UpdateDate = null,
                    IsDeleted = false
                },
                new()
                {
                    FloorId = floors[rnd.Next(floors.Count)].FloorId,
                    ZoneName = "Reading Room",
                    XCoordinate = 15.2,
                    YCoordinate = 25.8,
                    CreateDate = DateTime.Now,
                    UpdateDate = null,
                    IsDeleted = false
                },
                new()
                {
                    FloorId = floors[rnd.Next(floors.Count)].FloorId,
                    ZoneName = "Study Area",
                    XCoordinate = 5.0,
                    YCoordinate = 10.0,
                    CreateDate = DateTime.Now,
                    UpdateDate = null,
                    IsDeleted = false
                },
                new()
                {
                    FloorId = floors[rnd.Next(floors.Count)].FloorId,
                    ZoneName = "Computer Lab",
                    XCoordinate = 30.7,
                    YCoordinate = 40.2,
                    CreateDate = DateTime.Now,
                    UpdateDate = null,
                    IsDeleted = false
                },
                new()
                {
                    FloorId = floors[rnd.Next(floors.Count)].FloorId,
                    ZoneName = "Rest Room",
                    XCoordinate = 12.4,
                    YCoordinate = 18.9,
                    CreateDate = DateTime.Now,
                    UpdateDate = null,
                    IsDeleted = false
                }			
			};
			
			// Add Range
			await _context.LibraryZones.AddRangeAsync(zones);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed library zones successfully.");
		}
		
		//	Summary:
		//		Seeding Library section
		private async Task SeedLibrarySectionAsync()
		{
			// Initialize random 
			var rnd = new Random();
			
			// Retrieve all zones
			var zones = await _context.LibraryZones.ToListAsync();
			
			List<LibrarySection> sections = new()
			{
				new()
				{
					ZoneId = zones[rnd.Next(zones.Count)].ZoneId,
					SectionName = "Fiction",
					CreateDate = DateTime.Now,
					UpdateDate = null,
					IsDeleted = false
				},
				new()
				{
					ZoneId = zones[rnd.Next(zones.Count)].ZoneId,
					SectionName = "Non-Fiction",
					CreateDate = DateTime.Now,
					UpdateDate = null,
					IsDeleted = false
				},
				new()
				{
					ZoneId = zones[rnd.Next(zones.Count)].ZoneId,
					SectionName = "Science",
					CreateDate = DateTime.Now,
					UpdateDate = null,
					IsDeleted = false
				},
				new()
				{
					ZoneId = zones[rnd.Next(zones.Count)].ZoneId,
					SectionName = "History",
					CreateDate = DateTime.Now,
					UpdateDate = null,
					IsDeleted = false
				},
				new()
				{
					ZoneId = zones[rnd.Next(zones.Count)].ZoneId,
					SectionName = "Novel",
					CreateDate = DateTime.Now,
					UpdateDate = null,
					IsDeleted = false
				}
			};
			
			// Add Range
			await _context.LibrarySections.AddRangeAsync(sections);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed library sections successfully.");
		}
		
		//	Summary:
		//		Seeding Library shelf
		private async Task SeedLibraryShelvesAsync()
		{
			// Initialize random 
			var rnd = new Random();
			
			// Retrieve all existing sections
			var sections = await _context.LibrarySections.ToListAsync();

			List<LibraryShelf> shelves = new();
			
			// Generate shelves
			for (int i = 0; i < 20; i++) // Example: Create 20 shelves
			{
				// Generate random shelf number
				string shelfNumber = $"{(char)('A' + rnd.Next(0, 26))}-{rnd.Next(1, 100):D2}";

				// Create a new shelf
				shelves.Add(new LibraryShelf
				{
					SectionId = sections[rnd.Next(sections.Count)].SectionId, // Random section
					ShelfNumber = shelfNumber,
					CreateDate = DateTime.Now,
					UpdateDate = null,
					IsDeleted = false
				});
			}
			
			// Add Range
			await _context.LibraryShelves.AddRangeAsync(shelves);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed library shelves successfully.");
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
