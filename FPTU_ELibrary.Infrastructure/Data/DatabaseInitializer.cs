using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
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
				
				// [Categories]
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
				
				// [LibraryItemConditions]
				if (!await _context.LibraryItemConditions.AnyAsync()) await SeedLibraryItemConditionAsync();
				else _logger.Information("Already seed data for table {0}", "LibraryItemCondition");
				
				// [LibraryItems]
				// if (!await _context.LibraryItems.AnyAsync()) await SeedLibraryItemAsync();
				// else _logger.Information("Already seed data for table {0}", "LibraryItem");
				
				// [LibraryItemReviews]
				// if (!await _context.LibraryItemReviews.AnyAsync()) await SeedLibraryItemReviewsAsync();
				// else _logger.Information("Already seed data for table {0}", "LibraryItemReview");
				
				// [LibraryCards]
				if (!await _context.LibraryCards.AnyAsync()) await SeedLibraryCardAsync();
				else _logger.Information("Already seed data for table {0}", "LibraryCard");
				
				// [Suppliers]
				if (!await _context.Suppliers.AnyAsync()) await SeedSupplierAsync();
				else _logger.Information("Already seed data for table {0}", "Supplier");
            	
				// [FinePolicies]
				if (!await _context.FinePolicies.AnyAsync()) await SeedFinePoliciesAsync();
				else _logger.Information("Already seed data for table {0}", "FinePolicies");
				
				// [Fines]
				// if (!await _context.Fines.AnyAsync()) await SeedFinesAsync();
				// else _logger.Information("Already seed data for table {0}", "Fines");
            
				// [PaymentMethods]
				if (!await _context.PaymentMethods.AnyAsync()) await SeedPaymentMethodsAsync();
				else _logger.Information("Already seed data for table {0}", "PaymentMethods");
				
				// [Transactions]
				if (!await _context.Transactions.AnyAsync()) await SeedTransactionsAsync();
				else _logger.Information("Already seed data for table {0}", "Transactions");
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
					EnglishName = nameof(Role.LibraryPatron),
					VietnameseName = Role.LibraryPatron.GetDescription(),
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
					EnglishName = nameof(Role.Librarian),
					VietnameseName = Role.Librarian.GetDescription(),
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
				},
				new()
				{
					Email = "huynvqse171850@fpt.edu.vn",
					// @Admin123
					PasswordHash = "$2a$13$qUsCGtDD.dTou8YyhK.1YuKNjS7IM25cl/D0vd8EPaV40uvoG/l9u",
					FirstName = "Huy",
					LastName = "NVQ",
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

		private async Task SeedFinePoliciesAsync()
		{
			List<FinePolicy> finePolicies = new()
			{
				new()
				{
					ConditionType = FinePolicyConditionType.Damage,
					Description = "Rách bìa sách nhẹ (dưới 20% hư hỏng)",
					FixedFineAmount = 8000,
					FineAmountPerDay = 10000,
					FinePolicyTitle = "Rách bìa sách nhẹ (dưới 20%)"
				},
				new()
				{
					ConditionType = FinePolicyConditionType.Damage,
					Description = "Rách bìa sách nặng (trên 20% hư hỏng)",
					FixedFineAmount = 20000,
					FineAmountPerDay = 22000,
					FinePolicyTitle = "Rách bìa sách nặng (trên 20%)"
				},
				new()
				{
					ConditionType = FinePolicyConditionType.Damage,
					Description = "Mất trang sách nhẹ (dưới 50% trang mất)",
					FixedFineAmount = 12000,
					FineAmountPerDay = 14000,
					FinePolicyTitle = "Mất trang sách nhẹ (dưới 50%)"
				},
				new()
				{
					ConditionType = FinePolicyConditionType.Damage,
					Description = "Mất trang sách nặng (trên 50% trang mất)",
					FixedFineAmount = 25000,
					FineAmountPerDay = 30000,
					FinePolicyTitle = "Mất trang sách nặng (trên 50%)"
				},
				new()
				{
					ConditionType = FinePolicyConditionType.Damage,
					Description = "Vẽ bậy lên sách",
					FixedFineAmount = 15000,
					FineAmountPerDay = 20000,
					FinePolicyTitle = "Vẽ bậy lên sách"
				},
				new()
				{
					ConditionType = FinePolicyConditionType.Damage,
					Description = "Sách bị ướt do nước",
					FixedFineAmount = 18000,
					FineAmountPerDay = 20000,
					FinePolicyTitle = "Sách bị ướt"
				},
				new()
				{
					ConditionType = FinePolicyConditionType.Lost,
					Description = "Mất sách",
					FineAmountPerDay = 0,
					FinePolicyTitle = "Mất sách"
				},
				new()
				{
					ConditionType = FinePolicyConditionType.OverDue,
					Description = "Trả quá hạn",
					FixedFineAmount = 5000,
					FineAmountPerDay = 10000,
					FinePolicyTitle = "Trả quá hạn"
				}
			};
			
			await _context.FinePolicies.AddRangeAsync(finePolicies);	
			
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed fine policies successfully.");
		}

		private async Task SeedPaymentMethodsAsync()
		{
			List<PaymentMethod> paymentMethods = new()
			{
				new()
				{
					MethodName = PaymentType.PayOS.ToString()
				}
			};
			await _context.PaymentMethods.AddRangeAsync(paymentMethods);	
			
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed fine paymentmethods successfully.");
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
					VietnameseName = "Quản lí tài liệu"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.WarehouseTrackingManagement),
					VietnameseName = "Quản lí kho"
				},
				new ()
				{
					EnglishName = nameof(SystemFeatureEnum.BorrowManagement),
					VietnameseName = "Quản lí mượn trả tài liệu"
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
			// Initialize item category entities
            List<Category> categories = new()
            {
                new()
                {
	                Prefix = "SD",
                    EnglishName = nameof(LibraryItemCategory.SingleBook),
                    VietnameseName = LibraryItemCategory.SingleBook.GetDescription(),
					IsAllowAITraining = true,
					TotalBorrowDays = 30
                },
                new()
                {
	                Prefix = "SB",
	                EnglishName = nameof(LibraryItemCategory.BookSeries),
	                VietnameseName = LibraryItemCategory.BookSeries.GetDescription(),
					IsAllowAITraining = true,
					TotalBorrowDays = 30
                },
                new()
                {
	                Prefix = "STK",
	                EnglishName = nameof(LibraryItemCategory.ReferenceBook),
	                VietnameseName = LibraryItemCategory.ReferenceBook.GetDescription(),
					IsAllowAITraining = false,
					TotalBorrowDays = 90
                },
                new()
                {
                    Prefix = "TC",
                    EnglishName = nameof(LibraryItemCategory.Magazine),
                    VietnameseName = LibraryItemCategory.Magazine.GetDescription(),
					IsAllowAITraining = false,
					TotalBorrowDays = 30
                },
                new()
                {
	                Prefix = "BC",
	                EnglishName = nameof(LibraryItemCategory.Newspaper),
	                VietnameseName = LibraryItemCategory.Newspaper.GetDescription(),
					IsAllowAITraining = false,
					TotalBorrowDays = 20
                },
                new()
                {
	                Prefix = "STN",
	                EnglishName = nameof(LibraryItemCategory.ChildrenBook),
	                VietnameseName = LibraryItemCategory.ChildrenBook.GetDescription(),
	                IsAllowAITraining = true,
	                TotalBorrowDays = 60
                },
                new()
                {
	                Prefix = "SK",
	                EnglishName = nameof(LibraryItemCategory.Other),
	                VietnameseName = LibraryItemCategory.Other.GetDescription(),
	                IsAllowAITraining = false,
	                TotalBorrowDays = 30
                }
			};
        
			// Add range
			await _context.Categories.AddRangeAsync(categories);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed library item category success.");
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
			        AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/f/fb/To_Hoai.jpg",
			        FullName = "Tô Hoài",
			        Biography = "<p><strong>Nguyễn Sen</strong>, thường được biết đến với b&uacute;t danh <strong>T&ocirc; Ho&agrave;i</strong> (27 th&aacute;ng 9 năm 1920 &ndash; 6 th&aacute;ng 7 năm 2014),<a href=\"\\&quot;https://vi.wikipedia.org/wiki/T%C3%B4_Ho%C3%A0i#cite_note-1\\&quot;\" target=\"\\&quot;_blank\\&quot;\" rel=\"\\&quot;noopener\"><span class=\"\\&quot;cite-bracket\\&quot;\"><sup id=\"\\&quot;cite_ref-1\\&quot;\" class=\"\\&quot;reference\\&quot;\">[</sup></span><sup id=\"\\&quot;cite_ref-1\\&quot;\" class=\"\\&quot;reference\\&quot;\">1</sup><span class=\"\\&quot;cite-bracket\\&quot;\"><sup id=\"\\&quot;cite_ref-1\\&quot;\" class=\"\\&quot;reference\\&quot;\">]</sup></span></a> l&agrave; một nh&agrave; văn người <a title=\"\\&quot;Việt\" href=\"\\&quot;https://vi.wikipedia.org/wiki/Vi%E1%BB%87t_Nam\\&quot;\" target=\"\\&quot;_blank\\&quot;\" rel=\"\\&quot;noopener\">Việt Nam</a>.</p>\n<p>&nbsp;</p>\n<p>&Ocirc;ng được nh&agrave; nước Việt Nam trao tặng <a title=\"\\&quot;Giải\" href=\"\\&quot;https://vi.wikipedia.org/wiki/Gi%E1%BA%A3i_th%C6%B0%E1%BB%9Fng_H%E1%BB%93_Ch%C3%AD_Minh\\&quot;\" target=\"\\&quot;_blank\\&quot;\" rel=\"\\&quot;noopener\">Giải thưởng Hồ Ch&iacute; Minh</a> về Văn học &ndash; Nghệ thuật đợt 1 (1996) cho c&aacute;c t&aacute;c phẩm: <em>X&oacute;m giếng</em>, <em>Nh&agrave; ngh&egrave;o</em>, <em>O chuột</em>, <a class=\"\\&quot;mw-redirect\\&quot;\" title=\"\\&quot;Dế\" href=\"\\&quot;https://vi.wikipedia.org/wiki/D%E1%BA%BF_m%C3%A8n_phi%C3%AAu_l%C6%B0u_k%C3%BD\\&quot;\" target=\"\\&quot;_blank\\&quot;\" rel=\"\\&quot;noopener\"><em>Dế m&egrave;n phi&ecirc;u lưu k&yacute;</em></a>, <em>N&uacute;i Cứu quốc</em>, <em>Truyện T&acirc;y Bắc</em>, <em>Mười năm</em>, <em>Xuống l&agrave;ng</em>, <em>Vỡ tỉnh</em>, <em>T&agrave;o lường</em>, <em>Họ Gi&agrave;ng ở Ph&igrave;n Sa</em>, <em>Miền T&acirc;y</em>, <em>Vợ chồng A Phủ</em>, <em>Tuổi trẻ Ho&agrave;ng Văn Thụ</em>. Một số t&aacute;c phẩm đề t&agrave;i thiếu nhi của &ocirc;ng được dịch ra nhiều ngoại ngữ kh&aacute;c nhau.</p>",
			        Dob = new DateTime(1920, 9, 27),
			        DateOfDeath = new DateTime(2014, 7, 6),
			        Nationality = "Việt Nam",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH00002",
			        AuthorImage = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR8lNksAT9hFj7YFbvwb2yIDUXSC5C9P-F80w&s",
			        FullName = "Đoàn Giỏi",
			        Biography = "<p><strong>Đo&agrave;n Giỏi</strong> (17 th&aacute;ng 5 năm 1925 &ndash; 2 th&aacute;ng 4 năm 1989) l&agrave; một nh&agrave; văn Việt Nam nổi tiếng với c&aacute;c s&aacute;ng t&aacute;c về cuộc sống, thi&ecirc;n nhi&ecirc;n v&agrave; con người Nam Bộ; trong đ&oacute; ti&ecirc;u biểu nhất l&agrave; t&aacute;c phẩm Đất rừng phương Nam được tr&iacute;ch đoạn trong s&aacute;ch gi&aacute;o khoa Ngữ văn lớp 6 v&agrave; lớp 10. &Ocirc;ng được truy tặng Giải thưởng Nh&agrave; nước về Văn học Nghệ thuật đợt 1 năm 2001.</p>",
			        Dob = new DateTime(1925, 7, 17),
			        DateOfDeath = new DateTime(1989, 4,2),
			        Nationality = "Việt Nam",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH00003",
			        AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/60/Carlo_Collodi.jpg/640px-Carlo_Collodi.jpg",
			        FullName = "Carlo Collodi",
			        Biography = "<p><strong>Carlo Lorenzini</strong>&nbsp;(24 th&aacute;ng 11 năm 1826 - 26 th&aacute;ng 10 năm 1890), được biết nhiều hơn với&nbsp;<a title=\"B&uacute;t danh\" href=\"https://vi.wikipedia.org/wiki/B%C3%BAt_danh\">b&uacute;t danh</a>&nbsp;Carlo Collodi, l&agrave; một nh&agrave; văn &Yacute; của trẻ em nổi tiếng với cuốn tiểu thuyết cổ t&iacute;ch nổi tiếng thế giới&nbsp;<em><a title=\"Những cuộc phi&ecirc;u lưu của Pinocchio\" href=\"https://vi.wikipedia.org/wiki/Nh%E1%BB%AFng_cu%E1%BB%99c_phi%C3%AAu_l%C6%B0u_c%E1%BB%A7a_Pinocchio\">Những cuộc phi&ecirc;u lưu của Pinocchio</a></em>. Collodi sinh ra tại&nbsp;<a title=\"Firenze\" href=\"https://vi.wikipedia.org/wiki/Firenze\">Firenze</a>. &Ocirc;ng c&ograve;n l&agrave; một nh&agrave; b&aacute;o, vừa l&agrave; một nh&acirc;n vi&ecirc;n cao cấp trong Ch&iacute;nh phủ, đ&atilde; từng được thưởng Qu&acirc;n c&ocirc;ng bội tinh. Trong c&aacute;c cuộc chiến tranh độc lập năm 1848 v&agrave; 1860 Collodi đ&atilde; l&agrave;m một t&igrave;nh nguyện vi&ecirc;n qu&acirc;n đội Tuscan</p>",
			        Dob = new DateTime(1826, 11, 24),
			        DateOfDeath = new DateTime(1890, 10,26),
			        Nationality = "Ý",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH00004",
			        AuthorImage = "https://m.media-amazon.com/images/M/MV5BNzQ0YWMxNzYtOWM1Ni00MDM0LWI4ZDMtOTZjNzc2OThmMGY1XkEyXkFqcGc@._V1_.jpg",
			        FullName = "Antoine de Saint-Exupéry",
			        Biography = "<p><strong>Antoine Marie Jean-Baptiste Roger de Saint-Exup&eacute;ry</strong>, thường được biết tới với t&ecirc;n&nbsp;<strong>Antoine de Saint-Exup&eacute;ry</strong>&nbsp;hay gọi tắt l&agrave;&nbsp;<strong>Saint-Ex</strong>&nbsp;(sinh ng&agrave;y&nbsp;<a title=\"29 th&aacute;ng 6\" href=\"https://vi.wikipedia.org/wiki/29_th%C3%A1ng_6\">29 th&aacute;ng 6</a>&nbsp;năm&nbsp;<a title=\"1900\" href=\"https://vi.wikipedia.org/wiki/1900\">1900</a>&nbsp;- mất t&iacute;ch ng&agrave;y&nbsp;<a title=\"31 th&aacute;ng 7\" href=\"https://vi.wikipedia.org/wiki/31_th%C3%A1ng_7\">31 th&aacute;ng 7</a>&nbsp;năm&nbsp;<a title=\"1944\" href=\"https://vi.wikipedia.org/wiki/1944\">1944</a>) l&agrave; một&nbsp;<a title=\"Nh&agrave; văn\" href=\"https://vi.wikipedia.org/wiki/Nh%C3%A0_v%C4%83n\">nh&agrave; văn</a>&nbsp;v&agrave;&nbsp;<a title=\"Phi c&ocirc;ng\" href=\"https://vi.wikipedia.org/wiki/Phi_c%C3%B4ng\">phi c&ocirc;ng</a>&nbsp;<a title=\"Ph&aacute;p\" href=\"https://vi.wikipedia.org/wiki/Ph%C3%A1p\">Ph&aacute;p</a>&nbsp;nổi tiếng. Saint-Exup&eacute;ry được biết tới nhiều nhất với kiệt t&aacute;c văn học&nbsp;<a class=\"mw-redirect\" title=\"Ho&agrave;ng Tử B&eacute;\" href=\"https://vi.wikipedia.org/wiki/Ho%C3%A0ng_T%E1%BB%AD_B%C3%A9\">Ho&agrave;ng tử b&eacute;</a>&nbsp;(<em>Le Petit Prince</em>).</p>",
			        Dob = new DateTime(1900, 6, 29),
			        DateOfDeath = new DateTime(1944, 7,31),
			        Nationality = "Pháp",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = true
			    },
			    new()
			    {
			        AuthorCode = "AUTH00005",
			        AuthorImage = "https://img.giaoduc.net.vn/w1000/Uploaded/2025/edxwpcqdh/2022_07_01/gdvn-thay-nam-3039.jpg",
			        FullName = "PGS. TS. Nguyễn Văn Nam",
			        Biography = "<p>L&agrave; một trong những sinh vi&ecirc;n xuất sắc của sinh vi&ecirc;n chuy&ecirc;n ng&agrave;nh Xử l&yacute; th&ocirc;ng tin&nbsp;kinh tế kh&oacute;a 1 (tương ứng với kh&oacute;a 14 của Trường), <strong>GS.TS Nguyễn Văn Nam</strong> được&nbsp;c&aacute;c thầy, c&ocirc; gi&aacute;o v&agrave; c&aacute;c bạn sinh vi&ecirc;n nhớ đến với h&igrave;nh ảnh m&aacute;i t&oacute;c bồng bềnh l&atilde;ng&nbsp;tử nhưng cũng rất &ldquo;si&ecirc;u&rdquo; trong học tập.</p>\n<p>Sau khi tốt nghiệp chuy&ecirc;n Xử l&yacute; th&ocirc;ng tin kinh tế v&agrave; được giữ lại l&agrave;m giảng vi&ecirc;n ở Khoa Ng&acirc;n h&agrave;ng (nay l&agrave; Viện Ng&acirc;n h&agrave;ng &ndash; T&agrave;i ch&iacute;nh), với tố chất của một người học to&aacute;n &Ocirc;ng đ&atilde; dấn th&acirc;n v&agrave;o ng&agrave;nh Ng&acirc;n h&agrave;ng v&agrave; Thị trường t&agrave;i ch&iacute;nh. Đ&oacute; l&agrave; ng&agrave;nh học mới mẻ ở Việt Nam v&agrave;o những năm 80 của thập kỷ trước. &Ocirc;ng đ&atilde; bảo vệ th&agrave;nh c&ocirc;ng luận &aacute;n Tiến sỹ một c&aacute;ch xuất sắc ở trường Đại học tổng hợp Humboldt &ndash; Berlin, sau đ&oacute; l&agrave;m thực tập sinh khoa học tại Thị trường chứng kho&aacute;n Franfurt/ Main, Deutsche Bank (Đức).</p>",
			        Nationality = "Việt Nam",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH00006",
			        AuthorImage = "https://staff.hnue.edu.vn/Portals/0/Images/20fa5cd3-788b-4fd4-9d97-383b95edf53a.jpg",
			        FullName = "PGS. TS. Lê Minh Hoàng",
			        Nationality = "Việt Nam",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new()
			    {
			        AuthorCode = "AUTH00007",
			        FullName = "PGS. TS. Trần Văn Lượng",
			        Nationality = "Việt Nam",
			        CreateDate = DateTime.UtcNow,
			        IsDeleted = false
			    },
			    new ()
			    {
				    AuthorCode = "AUTH00008",
                    AuthorImage = "https://www.jkrowling.com/wp-content/uploads/2022/05/J.K.-Rowling-2021-Photography-Debra-Hurford-Brown-scaled.jpg",
                    FullName = "Rowling, J. K.",
                    Biography = "Joanne Rowling CH OBE FRSL, được biết đến với bút danh J. K. Rowling, là một tác giả và nhà từ thiện người Anh. Bà đã viết Harry Potter, một bộ truyện giả tưởng gồm bảy tập được xuất bản từ năm 1997 đến năm 2007.",
                    Dob = new DateTime(1965, 7, 31),
                    Nationality = "Gloucestershire, Anh",
                    CreateDate = DateTime.UtcNow,
                    IsDeleted = false
			    },
			    new ()
			    {
				    AuthorCode = "AUTH00009",
				    AuthorImage = "https://media.baoquangninh.vn/upload/image/202407/medium/2231419_6dea9ae532595ef6080e0db2bdb446a6.jpg",
				    FullName = "Aoyama Gosho",
				    Biography = "Gosho Aoyama là một họa sĩ truyện tranh nổi tiếng, được biết đến nhiều nhất với việc tạo ra loạt truyện trinh thám 'Thám tử lừng danh Conan'. Bộ truyện, được ra mắt vào năm 1994, đã trở thành một trong những series truyện tranh bán chạy nhất từ trước đến nay.",
				    Dob = new DateTime(1963, 6, 21),
				    Nationality = "Nhật Bản",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
			    },
			    new ()
			    {
				    AuthorCode =  "AUTH00010",
				    AuthorImage =  "https://example.com/path/to/barbieayumi.jpg",
				    FullName =  "Barbie Ayumi",
				    Biography =  "Barbie Ayumi là dịch giả nổi tiếng trong lĩnh vực manga tại Việt Nam. Cô đã đảm nhận vai trò chuyển ngữ cho nhiều tựa manga đình đám như 'Shin – Cậu Bé Bút Chì', 'One-Punch Man' và 'Dragon Ball Super'. Với phong cách dịch trung thực và sáng tạo, Barbie Ayumi giúp độc giả Việt Nam tiếp cận văn hóa Nhật Bản một cách mượt mà và đầy cảm xúc.",
				    Dob = new DateTime(1990, 01, 01),
				    Nationality =  "Nhật Bản",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted =  false
			    },
			    // Hector Malot
	            new Author
	            {
	                AuthorCode = "AUTH00011",
	                FullName = "Hector Malot",
	                Biography = "<p>Hector-Henri Malot (20 tháng 5 năm 1830 – 17 tháng 7 năm 1907) là một nhà văn người Pháp, nổi tiếng với tiểu thuyết <i>Sans Famille</i> (Cậu bé không gia đình).</p>",
	                Dob = new DateTime(1830, 5, 20),
	                DateOfDeath = new DateTime(1907, 7, 17),
	                Nationality = "Pháp",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Edmondo De Amicis
	            new Author
	            {
	                AuthorCode = "AUTH00012",
	                FullName = "Edmondo De Amicis",
	                Biography = "<p>Edmondo De Amicis (21 tháng 10 năm 1846 – 11 tháng 3 năm 1908) là một nhà tiểu thuyết gia, nhà báo, nhà thơ và tác giả truyện ngắn người Ý, được biết đến nhiều nhất với tiểu thuyết thiếu nhi <i>Cuore</i> (Trái tim).</p>",
	                Dob = new DateTime(1846, 10, 21),
	                DateOfDeath = new DateTime(1908, 3, 11),
	                Nationality = "Ý",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Alexander Grin (Aleksandr Stepanovich Grinevsky)
	            new Author
	            {
	                AuthorCode = "AUTH00013",
	                FullName = "Alexander Grin",
	                Biography = "<p>Aleksandr Stepanovich Grinevsky (23 tháng 8 năm 1880 – 8 tháng 7 năm 1932), nổi tiếng với bút danh Alexander Grin, là một nhà văn người Nga, được biết đến với các tiểu thuyết và truyện ngắn lãng mạn.</p>",
	                Dob = new DateTime(1880, 8, 23),
	                DateOfDeath = new DateTime(1932, 7, 8),
	                Nationality = "Nga",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Tetsuko Kuroyanagi
	            new Author
	            {
	                AuthorCode = "AUTH00014",
	                FullName = "Tetsuko Kuroyanagi",
	                Biography = "<p>Tetsuko Kuroyanagi (sinh ngày 9 tháng 8 năm 1933) là một nữ diễn viên, diễn viên lồng tiếng và tác giả bán chạy người Nhật, nổi tiếng với cuốn sách <i>Totto-chan, the Little Girl at the Window</i> (Totto-chan, cô bé bên khung cửa sổ).</p>",
	                Dob = new DateTime(1933, 8, 9),
	                Nationality = "Nhật",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Frances Hodgson Burnett
	            new Author
	            {
	                AuthorCode = "AUTH00015",
	                FullName = "Frances Hodgson Burnett",
	                Biography = "<p>Frances Hodgson Burnett (24 tháng 11 năm 1849 – 29 tháng 10 năm 1924) là một nhà tiểu thuyết gia và nhà viết kịch người Anh-Mỹ, nổi tiếng với các tác phẩm <i>The Secret Garden</i> (Vườn bí mật), <i>A Little Princess</i> (Công chúa nhí) và <i>Little Lord Fauntleroy</i> (Chàng bé quý tộc).</p>",
	                Dob = new DateTime(1849, 11, 24),
	                DateOfDeath = new DateTime(1924, 10, 29),
	                Nationality = "Anh-Mỹ",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Lewis Carroll
	            new Author
	            {
	                AuthorCode = "AUTH00016",
	                FullName = "Lewis Carroll",
	                Biography = "<p>Lewis Carroll là bút danh của Charles Lutwidge Dodgson (27 tháng 1 năm 1832 – 14 tháng 1 năm 1898), một nhà văn người Anh nổi tiếng với tác phẩm thiếu nhi thế giới như <i>Alice's Adventures in Wonderland</i> (Cuộc phiêu lưu của Alice ở xứ sở diệu kì).</p>",
	                Dob = new DateTime(1832, 1, 27),
	                DateOfDeath = new DateTime(1898, 1, 14),
	                Nationality = "Anh",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },

	            // Laura Ingalls Wilder
	            new Author
	            {
	                AuthorCode = "AUTH00017",
	                FullName = "Laura Ingalls Wilder",
	                Biography = "<p>Laura Ingalls Wilder (7 tháng 2 năm 1867 – 10 tháng 2 năm 1957) là một nhà văn người Mỹ, được biết đến qua loạt sách thiếu nhi <i>Little House on the Prairie</i> (Ngôi nhà nhỏ trên thảo nguyên).</p>",
	                Dob = new DateTime(1867, 2, 7),
	                DateOfDeath = new DateTime(1957, 2, 10),
	                Nationality = "Mỹ",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Ernst Hans Josef Gombrich
	            new Author
	            {
	                AuthorCode = "AUTH00018",
	                FullName = "Ernst Hans Josef Gombrich",
	                Biography = "<p>Ernst Hans Josef Gombrich (30 tháng 3 năm 1909 – 3 tháng 11 năm 2001) là một nhà sử học nghệ thuật người Áo-Anh và tác giả của <i>The Story of Art</i> (Câu chuyện nghệ thuật).</p>",
	                Dob = new DateTime(1909, 3, 30),
	                DateOfDeath = new DateTime(2001, 11, 3),
	                Nationality = "Áo-Anh",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Lê Hải Oanh
	            new Author
	            {
	                AuthorCode = "AUTH00019",
	                FullName = "Lê Hải Oanh",
	                Biography = null,
	                Dob = null,
	                DateOfDeath = null,
	                Nationality = null,
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Luis Sepúlveda
	            new Author
	            {
	                AuthorCode = "AUTH00020",
	                FullName = "Luis Sepúlveda",
	                Biography = "<p>Luis Sepúlveda (4 tháng 10 năm 1949 – 16 tháng 4 năm 2020) là một nhà văn và nhà báo người Chile, nổi tiếng với các tác phẩm như <i>The Old Man Who Read Love Stories</i> (Ông lão đọc những câu chuyện tình yêu).</p>",
	                Dob = new DateTime(1949, 10, 4),
	                DateOfDeath = new DateTime(2020, 4, 16),
	                Nationality = "Chile",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Trần Đăng Khoa
	            new Author
	            {
	                AuthorCode = "AUTH00021",
	                FullName = "Trần Đăng Khoa",
	                Biography = "<p>Trần Đăng Khoa (sinh ngày 26 tháng 4 năm 1958) là một nhà thơ và nhân vật văn hóa người Việt, được biết đến rộng rãi qua những bài thơ viết từ thuở nhỏ.</p>",
	                Dob = new DateTime(1958, 4, 26),
	                Nationality = "Việt Nam",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Võ Quảng
	            new Author
	            {
	                AuthorCode = "AUTH00022",
	                FullName = "Võ Quảng",
	                Biography = "<p>Võ Quảng (1 tháng 3 năm 1920 – 15 tháng 2 năm 2007) là một nhà văn người Việt chuyên viết văn học thiếu nhi, nổi tiếng với tác phẩm <i>Dế Mèn Phiêu Lưu Ký</i> và các tác phẩm khác.</p>",
	                Dob = new DateTime(1920, 3, 1),
	                DateOfDeath = new DateTime(2007, 2, 15),
	                Nationality = "Việt Nam",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Phạm Hổ
	            new Author
	            {
	                AuthorCode = "AUTH00023",
	                FullName = "Phạm Hổ",
	                Biography = "<p>Phạm Hổ (28 tháng 4 năm 1926 – 2 tháng 5 năm 2007) là một nhà thơ và tác giả người Việt, được biết đến đặc biệt qua văn học thiếu nhi.</p>",
	                Dob = new DateTime(1926, 4, 28),
	                DateOfDeath = new DateTime(2007, 5, 2),
	                Nationality = "Việt Nam",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Sơn Tùng
	            new Author
	            {
	                AuthorCode = "AUTH00024",
	                FullName = "Sơn Tùng",
	                Biography = "<p>Sơn Tùng (8 tháng 8 năm 1928 – 22 tháng 7 năm 2021) là một nhà văn người Việt, được biết đến với tác phẩm tiểu sử có ảnh hưởng <i>Búp sen xanh</i> về Chủ tịch Hồ Chí Minh.</p>",
	                Dob = new DateTime(1928, 8, 8),
	                DateOfDeath = new DateTime(2021, 7, 22),
	                Nationality = "Việt Nam",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Phùng Quán
	            new Author
	            {
	                AuthorCode = "AUTH00025",
	                FullName = "Phùng Quán",
	                Biography = "<p>Phùng Quán (10 tháng 1 năm 1932 – 22 tháng 1 năm 1995) là một nhà thơ và tiểu thuyết gia người Việt, nổi bật với các tác phẩm trong phong trào Nhân Văn–Giai Phẩm.</p>",
	                Dob = new DateTime(1932, 1, 10),
	                DateOfDeath = new DateTime(1995, 1, 22),
	                Nationality = "Việt Nam",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Nguyễn Nhật Ánh
	            new Author
	            {
	                AuthorCode = "AUTH00026",
	                FullName = "Nguyễn Nhật Ánh",
	                Biography = "<p>Nguyễn Nhật Ánh (sinh ngày 7 tháng 5 năm 1955) là một tác giả văn học thiếu nhi người Việt, nổi tiếng với tiểu thuyết <i>Mắt biếc</i> và nhiều tác phẩm được yêu thích khác.</p>",
	                Dob = new DateTime(1955, 5, 7),
	                Nationality = "Việt Nam",
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Nguyễn Hoàng Trung
	            new Author
	            {
	                AuthorCode = "AUTH00027",
	                FullName = "Nguyễn Hoàng Trung",
	                Biography = null,
	                Dob = null,
	                DateOfDeath = null,
	                Nationality = null,
	                AuthorImage = null,
	                CreateDate = DateTime.Now,
	                UpdateDate = null,
	                IsDeleted = false
	            },
	            // Tolkien J. R. R.
	            new Author
	            {
		            AuthorCode = "AUTH00028",
		            FullName = "Tolkien J. R. R.",
		            Biography = "<p>John Ronald Reuel Tolkien (3 tháng 1 năm 1892 – 2 tháng 9 năm 1973) là một tác giả người Anh, nổi tiếng với tác phẩm giả tưởng <i>The Hobbit</i> (Người Hobbit) và <i>The Lord of the Rings</i> (Chúa tể những chiếc nhẫn).</p>",
		            Dob = new DateTime(1892, 1, 3),
		            DateOfDeath = new DateTime(1973, 9, 2),
		            Nationality = "Anh",
		            AuthorImage = null,
		            CreateDate = DateTime.Now,
		            UpdateDate = null,
		            IsDeleted = false
	            },
				new()
				{
				    AuthorCode = "AUTH00029",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/2/2d/Paulo_Coelho_2014.jpg",
				    FullName = "Paulo Coelho",
				    Biography = "Paulo Coelho là một nhà văn Brazil nổi tiếng nhất với tiểu thuyết 'The Alchemist' và được biết đến nhờ những tác phẩm truyền cảm hứng.",
				    Dob = new DateTime(1947, 8, 24),
				    Nationality = "Brazil",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00030",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/1b/Mario_Puzo_%28photo%29.jpg/220px-Mario_Puzo_%28photo%29.jpg",
				    FullName = "Mario Puzo",
				    Biography = "Mario Puzo là một tác giả người Mỹ, nổi tiếng với tiểu thuyết 'The Godfather', tác phẩm đã tạo cảm hứng cho một trong những loạt phim kinh điển.",
				    Dob = new DateTime(1920, 10, 15),
				    Nationality = "Mỹ",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00031",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0c/Ernest_Hemingway_in_1950.jpg/220px-Ernest_Hemingway_in_1950.jpg",
				    FullName = "Ernest Hemingway",
				    Biography = "Ernest Hemingway là một tác giả người Mỹ, nổi tiếng với phong cách viết ngắn gọn và cuộc sống phiêu lưu.",
				    Dob = new DateTime(1899, 7, 21),
				    Nationality = "Mỹ",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00032",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/VuTrongPhuong.jpg/220px-VuTrongPhuong.jpg",
				    FullName = "Vũ Trọng Phụng",
				    Biography = "Vũ Trọng Phụng là một nhà văn lỗi lạc của Việt Nam, nổi tiếng với tác phẩm châm biếm xã hội 'Số đỏ'.",
				    Dob = new DateTime(1912, 1, 1),
				    Nationality = "Việt Nam",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00033",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/7f/Hector_Malot.jpeg/220px-Hector_Malot.jpeg",
				    FullName = "Hector Malot",
				    Biography = "Hector Malot là một tác giả người Pháp, được biết đến với tác phẩm 'Sans Famille' kể về hành trình của một đứa trẻ mồ côi.",
				    Dob = new DateTime(1830, 4, 20),
				    Nationality = "Pháp",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00034",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/5f/Victor_Hugo_by%2C_1851.jpg/220px-Victor_Hugo_by%2C_1851.jpg",
				    FullName = "Victor Hugo",
				    Biography = "Victor Hugo là một trong những nhà văn, nhà thơ và nhà viết kịch lớn của phong trào Lãng mạn của Pháp, nổi tiếng với 'Les Misérables' và 'The Hunchback of Notre-Dame'.",
				    Dob = new DateTime(1802, 2, 26),
				    Nationality = "Pháp",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00035",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/en/thumb/9/92/Margaret_Mitchell_1949.jpeg/220px-Margaret_Mitchell_1949.jpeg",
				    FullName = "Margaret Mitchell",
				    Biography = "Margaret Mitchell là một tác giả người Mỹ, được biết đến nhiều nhất qua tác phẩm 'Gone with the Wind' đoạt giải Pulitzer năm 1936.",
				    Dob = new DateTime(1900, 11, 8),
				    Nationality = "Mỹ",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00036",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0c/Ernest_Hemingway_in_1950.jpg/220px-Ernest_Hemingway_in_1950.jpg",
				    FullName = "Ernest Hemingway",
				    Biography = "Ernest Hemingway là một tác giả người Mỹ, nổi tiếng với phong cách viết ngắn gọn, các tác phẩm có sức ảnh hưởng lớn cũng như lối sống phiêu lưu.",
				    Dob = new DateTime(1899, 7, 21),
				    Nationality = "Mỹ",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00037",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/34/Emily_Bront%C3%AB_by_Portrait_of_a_Young_Lady%2C_in_a_Carved_Headframe.jpg/220px-Emily_Bront%C3%AB_by_Portrait_of_a_Young_Lady%2C_in_a_Carved_Headframe.jpg",
				    FullName = "Emily Brontë",
				    Biography = "Emily Brontë là một tác giả và nhà thơ người Anh, nổi tiếng với tiểu thuyết duy nhất 'Wuthering Heights' đã trở thành một tác phẩm kinh điển.",
				    Dob = new DateTime(1818, 7, 30),
				    Nationality = "Anh",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00038",
				    FullName = "Nguyễn Thị Hồng",
				    Nationality = "Việt Nam",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00039",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6c/Gabriel_Garc%C3%ADa_M%C3%A1rquez_2014.jpg/220px-Gabriel_Garc%C3%ADa_M%C3%A1rquez_2014.jpg",
				    FullName = "Gabriel García Márquez",
				    Biography = "Gabriel García Márquez là một tác giả và nhà báo người Colombia, với những tác phẩm nổi bật như 'One Hundred Years of Solitude' tiêu biểu cho thể loại hiện thực huyền ảo.",
				    Dob = new DateTime(1927, 3, 6),
				    Nationality = "Colombia",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
				    AuthorCode = "AUTH00040",
				    AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d3/Jerome_k_jerome.jpg/220px-Jerome_k_jerome.jpg",
				    FullName = "Jerome K. Jerome",
				    Biography = "Jerome K. Jerome là một tác giả và nhà hài hước người Anh, nổi tiếng với tác phẩm 'Three Men in a Boat' mang tính chất du lịch hài hước.",
				    Dob = new DateTime(1859, 5, 2),
				    Nationality = "Anh",
				    CreateDate = DateTime.UtcNow,
				    IsDeleted = false
				},
				new()
				{
					AuthorCode = "AUTH00041",
					AuthorImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/7f/Harper_Lee_%28cropped%29.jpg/220px-Harper_Lee_%28cropped%29.jpg",
					FullName = "Harper Lee",
					Biography = "Harper Lee là một tác giả người Mỹ, nổi tiếng với tiểu thuyết 'To Kill a Mockingbird' về bất công chủng tộc và lòng nhân ái ở miền Nam nước Mỹ. Tác phẩm này đã giành giải Pulitzer năm 1961.",
					Dob = new DateTime(1926, 4, 28),
					Nationality = "Mỹ",
					CreateDate = DateTime.UtcNow,
					IsDeleted = false
				},
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
					EmailConfirmed = true,
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
                    EmailConfirmed = true,
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
					FloorNumber = 1, 
					CreateDate = DateTime.Now
				},
				new()
				{
					FloorNumber = 2, 
					CreateDate = DateTime.Now
				},
				new()
				{
					FloorNumber = 3, 
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
			// Retrieve all current floor
			var floors = await _context.LibraryFloors.ToListAsync();
			if (!floors.Any())
			{
				_logger.Warning("Not found any library floors to process seeding library zone");
				return;
			}
			
			// Retrieve floors
			var firstFloor = floors.FirstOrDefault(f => f.FloorNumber == 1) ?? floors.First();
			var secondFloor = floors.FirstOrDefault(f => f.FloorNumber == 2) ?? floors.First();
			var thirdFloor = floors.FirstOrDefault(f => f.FloorNumber == 3) ?? floors.First();
			
			List<LibraryZone> zones = new()
			{
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.Lobby)),
			        VieZoneName = LibraryLocation.Zones.Lobby,
			        EngDescription = "Located near the main entrance, connecting to the checkout area and leading to various sections of the library.",
			        VieDescription = "Nằm gần lối vào chính, kết nối với khu vực quầy thanh toán và dẫn đến các khu vực khác của thư viện.",
			        TotalCount = 1,
			        CreateDate = DateTime.Now
			    },
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.LibraryStacks)),
			        VieZoneName = LibraryLocation.Zones.LibraryStacks,
			        EngDescription = "Positioned centrally, surrounded by reading spaces and computer zones, it houses various book collections.",
			        VieDescription = "Nằm ở trung tâm, được bao quanh bởi khu đọc sách và khu máy tính, chứa nhiều bộ sưu tập sách.",
			        TotalCount = 3,
			        CreateDate = DateTime.Now,
			        LibrarySections = new List<LibrarySection>()
			        {
				        // 000-099: Computer science, information & general works
				        LibraryLocation.Sections.ComputerScienceAndGeneralWorks,
				        // 800-899: Literature
				        LibraryLocation.Sections.Literature,
				        // 000-999: Children
				        LibraryLocation.Sections.Children,
			        }
			    },
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.ReadingSpace)),
			        VieZoneName = LibraryLocation.Zones.ReadingSpace,
			        EngDescription = "Positioned on the left side of the floor, near the book area, providing a quiet space for reading.",
			        VieDescription = "Nằm ở phía bên trái tầng, gần khu sách, cung cấp không gian yên tĩnh để đọc sách.",
			        TotalCount = 1,
			        CreateDate = DateTime.Now
			    },
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.RestRoom)),
			        VieZoneName = LibraryLocation.Zones.RestRoom,
			        EngDescription = "Situated in the lower-left corner, separate from other areas to provide a quiet resting space.",
			        VieDescription = "Nằm ở góc dưới bên trái, tách biệt với các khu vực khác để tạo không gian nghỉ ngơi yên tĩnh.",
			        TotalCount = 2,
			        CreateDate = DateTime.Now
			    },
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.MeetingRoom)),
			        VieZoneName = LibraryLocation.Zones.MeetingRoom,
			        EngDescription = "Positioned in the upper-left corner of the library, adjacent to the reading space and computer zone.",
			        VieDescription = "Nằm ở góc trên bên trái của thư viện, bên cạnh khu đọc sách và khu máy tính.",
			        TotalCount = 2,
			        CreateDate = DateTime.Now
			    },
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.ComputerZone)),
			        VieZoneName = LibraryLocation.Zones.ComputerZone,
			        EngDescription = "Located along the left side of the library, providing access to public computers for research and study.",
			        VieDescription = "Nằm dọc theo phía bên trái của thư viện, cung cấp máy tính công cộng cho nghiên cứu và học tập.",
			        TotalCount = 2,
			        CreateDate = DateTime.Now
			    },
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.SelfCheckoutStation)),
			        VieZoneName = LibraryLocation.Zones.SelfCheckoutStation,
			        EngDescription = "Positioned near the parking entrance, allowing users to return books conveniently.",
			        VieDescription = "Nằm gần lối vào bãi đỗ xe, giúp người dùng trả sách một cách thuận tiện.",
			        TotalCount = 1,
			        CreateDate = DateTime.Now
			    },
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.Auditorium)),
			        VieZoneName = LibraryLocation.Zones.Auditorium,
			        EngDescription = "Situated in the upper-right section of the library, near the gallery and restrooms.",
			        VieDescription = "Nằm ở khu vực phía trên bên phải của thư viện, gần phòng trưng bày và nhà vệ sinh.",
			        TotalCount = 1,
			        CreateDate = DateTime.Now
			    },
			    new()
			    {
			        FloorId = firstFloor.FloorId,
			        EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.Gallery)),
			        VieZoneName = LibraryLocation.Zones.Gallery,
			        EngDescription = "Located next to the auditorium and near the restrooms, providing a space for exhibitions and displays.",
			        VieDescription = "Nằm cạnh thính phòng và gần nhà vệ sinh, cung cấp không gian cho các cuộc triển lãm và trưng bày.",
			        TotalCount = 1,
			        CreateDate = DateTime.Now
			    },
			    new()
			    {
				    FloorId = firstFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.CheckoutCounter)),
				    VieZoneName = LibraryLocation.Zones.CheckoutCounter,
				    EngDescription = "Located centrally in the lobby, the checkout counter serves as the main point for borrowing and returning books.",
				    VieDescription = "Nằm ở trung tâm sảnh chính, quầy thanh toán là điểm chính để mượn và trả sách.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
			    },
			    new()
				{
				    FloorId = secondFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.LibraryStacks)),
				    VieZoneName = LibraryLocation.Zones.LibraryStacks,
				    EngDescription = "Located in the left section of the floor, adjacent to the reading space and near the entrance corridor.",
				    VieDescription = "Nằm ở khu vực bên trái của tầng, liền kề với không gian đọc và gần hành lang lối vào.",
				    TotalCount = 8,
				    CreateDate = DateTime.Now,
				    LibrarySections = new List<LibrarySection>()
				    {
					    // 100-199: Philosophy & Psychology
					    LibraryLocation.Sections.PhilosophyAndPsychology,
					    // 300-399: Social sciences
					    LibraryLocation.Sections.SocialSciences,
					    // 400-499: Language
					    LibraryLocation.Sections.Language,
					    // 500-599: Natural sciences and mathematics
					    LibraryLocation.Sections.NaturalSciencesAndMathematics,
					    // 600-699: Technology
					    LibraryLocation.Sections.Technology,
					    // 700-799: Arts & Recreation
					    LibraryLocation.Sections.ArtsAndRecreation,
					    // Magazines & News
					    LibraryLocation.Sections.MagazinesAndNews,
					    // Reference
					    LibraryLocation.Sections.Reference,
				    }
				},
				new()
				{
				    FloorId = secondFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.ReadingSpace)),
				    VieZoneName = LibraryLocation.Zones.ReadingSpace,
				    EngDescription = "Positioned at the top-right corner of the floor, connected to the checkout counter.",
				    VieDescription = "Nằm ở góc trên bên phải của tầng, kết nối với quầy thủ thư.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = secondFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.CheckoutCounter)),
				    VieZoneName = LibraryLocation.Zones.CheckoutCounter,
				    EngDescription = "Located near the entrance of the reading space, providing access to book checkouts.",
				    VieDescription = "Nằm gần lối vào của khu vực đọc sách, tạo thuận lợi cho việc mượn sách.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = secondFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.TrusteesRoom)),
				    VieZoneName = LibraryLocation.Zones.TrusteesRoom,
				    EngDescription = "Located in the lower-right section of the floor, adjacent to the admin office.",
				    VieDescription = "Nằm ở phần dưới bên phải của tầng, liền kề với văn phòng hành chính.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = secondFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.AdminOffice)),
				    VieZoneName = LibraryLocation.Zones.AdminOffice,
				    EngDescription = "Positioned at the bottom-right of the floor, next to the trustees room and near the toilets.",
				    VieDescription = "Nằm ở phía dưới bên phải của tầng, cạnh phòng hội đồng quản trị và gần khu vệ sinh.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = secondFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.MeetingRoom)),
				    VieZoneName = LibraryLocation.Zones.MeetingRoom,
				    EngDescription = "Located near the central corridor, next to the toilets and across from the trustees room.",
				    VieDescription = "Nằm gần hành lang trung tâm, bên cạnh nhà vệ sinh và đối diện phòng hội đồng quản trị.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = secondFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.Toilet)),
				    VieZoneName = LibraryLocation.Zones.Toilet,
				    EngDescription = "Positioned centrally on the floor, separated into male and female sections, near the meeting room.",
				    VieDescription = "Nằm ở trung tâm của tầng, chia thành khu vực nam và nữ, gần phòng họp.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = thirdFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.LibraryStacks)),
				    VieZoneName = LibraryLocation.Zones.LibraryStacks,
				    EngDescription = "Located at the upper left section of the floor, adjacent to the checkout corner.",
				    VieDescription = "Nằm ở khu vực phía trên bên trái của tầng, liền kề với quầy thủ thư.",
				    TotalCount = 2,
				    CreateDate = DateTime.Now,
				    LibrarySections = new List<LibrarySection>()
				    {
					    // 200-299: Religion
					    LibraryLocation.Sections.Religion,
					    // 900-999: History & Geography
					    LibraryLocation.Sections.HistoryAndGeography
				    }
				},
				new()
				{
				    FloorId = thirdFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.CheckoutCounter)),
				    VieZoneName = LibraryLocation.Zones.CheckoutCounter,
				    EngDescription = "Positioned at the top-right corner of the floor, next to the book stacks.",
				    VieDescription = "Nằm ở góc trên bên phải của tầng, cạnh khu vực sách.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = thirdFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.StudyArea)),
				    VieZoneName = LibraryLocation.Zones.StudyArea,
				    EngDescription = "Occupies the lower left section of the floor, providing ample space for studying, near the printer station.",
				    VieDescription = "Chiếm khu vực phía dưới bên trái của tầng, cung cấp không gian rộng rãi để học tập, gần trạm in.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = thirdFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.MeetingRoom)),
				    VieZoneName = LibraryLocation.Zones.MeetingRoom,
				    EngDescription = "Located in the lower center of the floor, next to the study area.",
				    VieDescription = "Nằm ở khu vực trung tâm phía dưới của tầng, bên cạnh khu học tập.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				},
				new()
				{
				    FloorId = thirdFloor.FloorId,
				    EngZoneName = DatabaseInitializerExtensions.AddWhitespaceToString(nameof(LibraryLocation.Zones.Printer)),
				    VieZoneName = "Máy in",
				    EngDescription = "Situated at the left side of the study area, easily accessible for students.",
				    VieDescription = "Nằm ở phía bên trái của khu học tập, thuận tiện cho sinh viên sử dụng.",
				    TotalCount = 1,
				    CreateDate = DateTime.Now
				}
			};

			
			// Add Range
			await _context.LibraryZones.AddRangeAsync(zones);
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed library zones successfully.");
		}
		
		//	Summary:
		//		Seeding Library card
		private async Task SeedLibraryCardAsync()
		{
			// Get all admin 
			var admins = await _context.Users
				.Where(u => u.Role.EnglishName == nameof(Role.Administration))
				.ToListAsync();

			var rnd = new Random();
			// Add card for each admin
			foreach (var admin in admins)
			{
				admin.LibraryCard = new()
				{
					FullName = $"{admin.FirstName} {admin.LastName}",
					Avatar = "https://img.freepik.com/free-photo/serious-young-african-man-standing-isolated_171337-9633.jpg",
					Barcode = DatabaseInitializerExtensions.GenerateBarcode("EC"),
					IssuanceMethod = LibraryCardIssuanceMethod.Online,
					Status = LibraryCardStatus.Active,
					IsExtended = false,
					IsReminderSent = true,
					ExtensionCount = 0,
					IssueDate = DateTime.Now,
					ExpiryDate = DateTime.Now.AddYears(2),
					TransactionCode = "CODE" + $"{rnd.Next(100, 200)}"
				};
				
			}
			
			// Update range  
			_context.Users.UpdateRange(admins);
			
			// Save DB
			var saveSucc = await _context.SaveChangesAsync() > 0;
			if(saveSucc) _logger.Information("Seed library cards successfully.");
		}
		
		//	Summary:
		//		Seeding Library item condition
		private async Task SeedLibraryItemConditionAsync()
		{
			List<LibraryItemCondition> conditions = new()
			{
				new()
				{
					EnglishName = nameof(LibraryItemConditionStatus.Good),
					VietnameseName = LibraryItemConditionStatus.Good.GetDescription(),
					CreatedAt = DateTime.Now
				},
				new()
				{
					EnglishName = nameof(LibraryItemConditionStatus.Damaged),
					VietnameseName = LibraryItemConditionStatus.Damaged.GetDescription(),
					CreatedAt = DateTime.Now
				},
				new()
				{
					EnglishName = nameof(LibraryItemConditionStatus.Worn),
					VietnameseName = LibraryItemConditionStatus.Worn.GetDescription(),
					CreatedAt = DateTime.Now
				},
				new()
				{
					EnglishName = nameof(LibraryItemConditionStatus.Lost),
					VietnameseName = LibraryItemConditionStatus.Lost.GetDescription(),
					CreatedAt = DateTime.Now
				}
			};
			
			// Add range 
			await _context.LibraryItemConditions.AddRangeAsync(conditions);
			// Save DB
			var saveSucc = await _context.SaveChangesAsync() > 0;
			if(saveSucc) _logger.Information("Seed library item conditions successfully");
		}
		
        //  Summary:
        //      Seeding Library item
        private async Task SeedLibraryItemAsync()
        {
			// Get librarian
			var librarian = await _context.Employees
				.Include(x => x.Role)
				.Where(l => l.Email.Contains("librarian"))
				.FirstOrDefaultAsync(e => e.Role.EnglishName == Role.Librarian.ToString());

			// Get authors
			var authors = await _context.Authors.ToListAsync();
			
			// Get item categories
			var categories = await _context.Categories.ToListAsync();

			if(librarian == null || !categories.Any() || !authors.Any())
			{
				_logger.Error("Not found any librarian, category or author to process seeding item");
				return;
			}
			
			// Get library shelves
			var libraryShelves = await _context.LibraryShelves.ToListAsync();
			if(!libraryShelves.Any())
			{
				_logger.Error("Not found any shelf to process seeding library item");
				return;
			}
			
			// Retrieve all item condition
			var goodCondition = await _context.LibraryItemConditions
				.Where(c => c.EnglishName == nameof(LibraryItemConditionStatus.Good))
				.FirstOrDefaultAsync();
			if(goodCondition == null)
			{
				_logger.Error("Not found any good item condition to process seeding library item");
				return;
			}
			
			// Initialize item group
			var itemGrp1 = new LibraryItemGroup()
			{
				// AiTrainingCode = Guid.NewGuid().ToString(),
				AiTrainingCode = "c961f808-eb91-411b-a02c-497549692c21",
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
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SingleBook)).CategoryId,
			        // TODO: Change to Draft
			        Status = LibraryItemStatus.Published,
			        CanBorrow = false,
			        IsTrained = false,
			        CreatedAt = DateTime.Now,
			        CreatedBy = librarian.Email,
			        LibraryItemInventory = new LibraryItemInventory()
			        {
				        TotalUnits = 0,
				        AvailableUnits = 0,
						BorrowedUnits = 0,
						ReservedUnits = 0,
						RequestUnits = 0,
						LostUnits = 0
			        },
                    LibraryItemAuthors = new List<LibraryItemAuthor>()
				    {
					    new()
					    {
						    AuthorId = authors.First(a => a.AuthorCode == "AUTH00001").AuthorId
					    }
				    }
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
					    ReservedUnits = 0,
						LostUnits = 0
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
								    ConditionId = goodCondition.ConditionId
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
								    ConditionId = goodCondition.ConditionId
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
								    ConditionId = goodCondition.ConditionId
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
								    ConditionId = goodCondition.ConditionId
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
								    ConditionId = goodCondition.ConditionId
							    }
						    }
					    }
				    },
				    LibraryItemAuthors = new List<LibraryItemAuthor>()
				    {
					    new()
					    {
						    AuthorId = authors.First(a => a.AuthorCode == "AUTH00008").AuthorId
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
					    ReservedUnits = 0,
						LostUnits = 0
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
								    ConditionId = goodCondition.ConditionId
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
								    ConditionId = goodCondition.ConditionId
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
								    ConditionId = goodCondition.ConditionId
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
								    ConditionId = goodCondition.ConditionId
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
								    ConditionId = goodCondition.ConditionId
							    }
						    }
					    }
				    },
				    LibraryItemAuthors = new List<LibraryItemAuthor>()
				    {
					    new()
					    {
						    AuthorId = authors.First(a => a.AuthorCode == "AUTH00008").AuthorId
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
				    // TODO: Change to Draft
			        Status = LibraryItemStatus.Published,
				    CanBorrow = false,
				    IsTrained = false,
				    CreatedAt = DateTime.Now,
				    CreatedBy = librarian.Email,
					LibraryItemInventory = new LibraryItemInventory()
			        {
				        TotalUnits = 0,
				        AvailableUnits = 0,
						BorrowedUnits = 0,
						ReservedUnits = 0,
						RequestUnits = 0,
						LostUnits = 0
			        },
			        LibraryItemAuthors = new List<LibraryItemAuthor>()
			        {
				        new()
				        {
					        AuthorId = authors.First(a => a.AuthorCode == "AUTH00008").AuthorId
				        }
			        }
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
				    Isbn = "9787127440582",
				    Ean = "8934567890123",
				    EstimatedPrice = 200000M,
				    PageCount = 320,
				    PhysicalDetails = "Bìa mềm, in màu",
				    Dimensions = "15 x 23 cm",
				    Genres = "Kinh tế học, Tâm lý học ứng dụng",
				    GeneralNote = "Sách được viết bởi hai tác giả của 'Freakonomics'.",
				    TopicalTerms = "Kinh tế, Tư duy sáng tạo, Giải quyết vấn đề",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SingleBook)).CategoryId,
				    // TODO: Change to Draft
			        Status = LibraryItemStatus.Published,
				    CanBorrow = false,
				    IsTrained = false,
				    CreatedAt = DateTime.Now,
				    CreatedBy = librarian.Email,
					LibraryItemInventory = new LibraryItemInventory()
			        {
				        TotalUnits = 0,
				        AvailableUnits = 0,
						BorrowedUnits = 0,
						ReservedUnits = 0,
						RequestUnits = 0,
						LostUnits = 0
			        },
			        LibraryItemAuthors = new List<LibraryItemAuthor>()
			        {
				        new()
				        {
					        AuthorId = authors.First(a => a.AuthorCode == "AUTH00002").AuthorId
				        }
			        }
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
				    Isbn = "9788703856117",
				    Ean = "8937890123456",
				    EstimatedPrice = 150000M,
				    PageCount = 400,
				    PhysicalDetails = "Bìa mềm, in màu",
				    Dimensions = "16 x 24 cm",
				    Genres = "Kỹ năng sống, Tâm lý học ứng dụng",
				    GeneralNote = "Sách nổi tiếng và được dịch ra nhiều ngôn ngữ trên thế giới.",
				    TopicalTerms = "Giao tiếp, Kỹ năng sống, Tâm lý học",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.SingleBook)).CategoryId,
				    // TODO: Change to Draft
			        Status = LibraryItemStatus.Published,
				    CanBorrow = false,
				    IsTrained = false,
				    CreatedAt = DateTime.Now,
				    CreatedBy = librarian.Email,
					LibraryItemInventory = new LibraryItemInventory()
			        {
				        TotalUnits = 4,
				        AvailableUnits = 4,
						BorrowedUnits = 0,
						ReservedUnits = 0,
						RequestUnits = 0,
						LostUnits = 0
			        },
			        LibraryItemAuthors = new List<LibraryItemAuthor>()
			        {
				        new()
				        {
					        AuthorId = authors.First(a => a.AuthorCode == "AUTH00003").AuthorId
				        }
			        },
					LibraryItemInstances = new List<LibraryItemInstance>()
					{
						new()
						{
							Barcode = "SD00011",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
							LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
							{
								new ()
								{
									ConditionId = goodCondition.ConditionId
								}
							}
						},
						new()
						{
							Barcode = "SD00012",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
							LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
							{
								new ()
								{
									ConditionId = goodCondition.ConditionId
								}
							}
						},
						new()
						{
							Barcode = "SD00013",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
							LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
							{
								new ()
								{
									ConditionId = goodCondition.ConditionId
								}
							}
						},
						new()
						{
							Barcode = "SD00014",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
							LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
							{
								new ()
								{
									ConditionId = goodCondition.ConditionId
								}
							}
						},
						new()
						{
							Barcode = "SD00015",
							Status = nameof(LibraryItemInstanceStatus.InShelf),
							LibraryItemConditionHistories = new List<LibraryItemConditionHistory>()
							{
								new ()
								{
									ConditionId = goodCondition.ConditionId
								}
							}
						}
					}
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
			        // TODO: Change to Draft
			        Status = LibraryItemStatus.Published,
			        CanBorrow = false,
			        IsTrained = false,
			        CreatedAt = DateTime.Now,
			        CreatedBy = librarian.Email,
					LibraryItemInventory = new LibraryItemInventory()
			        {
				        TotalUnits = 0,
				        AvailableUnits = 0,
						BorrowedUnits = 0,
						ReservedUnits = 0,
						RequestUnits = 0,
						LostUnits = 0
			        }
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
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.Newspaper)).CategoryId,
			        // TODO: Change to Draft
			        Status = LibraryItemStatus.Published,
			        CanBorrow = false,
			        IsTrained = false,
			        CreatedAt = DateTime.Now,
			        CreatedBy = librarian.Email,
					LibraryItemInventory = new LibraryItemInventory()
			        {
				        TotalUnits = 0,
				        AvailableUnits = 0,
						BorrowedUnits = 0,
						ReservedUnits = 0,
						RequestUnits = 0,
						LostUnits = 0
			        }
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
			        Isbn = "9785862430813",
			        Ean = "8934567890678",
			        EstimatedPrice = 300000M,
			        PageCount = 1000,
			        PhysicalDetails = "3 tập, bìa cứng",
			        Dimensions = "18 x 25 cm",
			        Genres = "Văn học, Kinh điển",
			        GeneralNote = "Gồm 3 tập với các tác phẩm từ thế kỷ 18 đến thế kỷ 20.",
			        TopicalTerms = "Văn học Việt Nam, Tác phẩm kinh điển",
			        CategoryId = categories.First(x => x.EnglishName == nameof(LibraryItemCategory.ReferenceBook)).CategoryId,
			        // TODO: Change to Draft
			        Status = LibraryItemStatus.Published,
			        CanBorrow = false,
			        IsTrained = false,
			        CreatedAt = DateTime.Now,
			        CreatedBy = librarian.Email,
					LibraryItemInventory = new LibraryItemInventory()
			        {
				        TotalUnits = 0,
				        AvailableUnits = 0,
						BorrowedUnits = 0,
						ReservedUnits = 0,
						RequestUnits = 0,
						LostUnits = 0
			        },
			        LibraryItemAuthors = new List<LibraryItemAuthor>()
			        {
				        new()
				        {
					        AuthorId = authors.First(a => a.AuthorCode == "AUTH00005").AuthorId
				        }
			        }
			    }
			};

			// Add range items
			await _context.LibraryItems.AddRangeAsync(items);
			// Save change
			var saveSucc = await _context.SaveChangesAsync() > 0;

			if (saveSucc) _logger.Information("Seed library items successfully.");
		}
    
        //	Summary:
        //		Seeding Library item review
        private async Task SeedLibraryItemReviewsAsync()
        {
	        // Initialize random 
	        var rnd = new Random();
	        
	        // Retrieve all library item
	        var libraryItems = await _context.LibraryItems.ToListAsync();
	        
	        // Retrieve users (maximum: 20 users)
	        var users = await _context.Users.Take(20).ToListAsync();
	        
	        var possibleRatings = new List<double> { 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0 };
	        for (int i = 0; i < libraryItems.Count; i++)
	        {
		        var lItem = libraryItems[i];
				
		        // Initialize item review collection
		        var itemReviews = new List<LibraryItemReview>();
		        var indexThreshold = rnd.Next(1, 10);
		        for (int j = 0; j < indexThreshold; j++)
		        {
			        itemReviews.Add(new LibraryItemReview()
			        {
				        LibraryItemId = lItem.LibraryItemId,
				        UserId = users[rnd.Next(users.Count)].UserId,
				        RatingValue = possibleRatings[rnd.Next(possibleRatings.Count)],
				        CreateDate = DateTime.Now.Subtract(new TimeSpan(rnd.Next(0,100), 0,0,0))
			        });
		        }
		        
		        // Add reviews
		        lItem.LibraryItemReviews = itemReviews;
	        }
	        
	        // Save DB
	        var saveSucc = await _context.SaveChangesAsync() > 0;
	        if(saveSucc) _logger.Information("Seed library item reviews successfully.");
        }
        
        //	Summary:
        //		Seed supplier
        private async Task SeedSupplierAsync()
		{
		    List<Supplier> suppliers = new()
		    {
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Trẻ",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Nguyễn Văn A",
		            ContactEmail = "contact@xuatbantre.vn",
		            ContactPhone = "0123456789",
		            Address = "123 Đường Lê Lợi, Quận 1",
		            Country = "Việt Nam",
		            City = "TP.HCM",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Kim Đồng",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Trần Thị B",
		            ContactEmail = "info@kimdong.com",
		            ContactPhone = "0987654321",
		            Address = "456 Phố Huế, Hà Nội",
		            Country = "Việt Nam",
		            City = "Hà Nội",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Giáo Dục",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Lê Văn C",
		            ContactEmail = "contact@giaoduc.vn",
		            ContactPhone = "0912345678",
		            Address = "789 Đường Cách Mạng, Hà Nội",
		            Country = "Việt Nam",
		            City = "Hà Nội",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Văn Học",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Phạm Thị D",
		            ContactEmail = "info@vanhoc.vn",
		            ContactPhone = "0934567890",
		            Address = "321 Đường Lê Duẩn, TP.HCM",
		            Country = "Việt Nam",
		            City = "TP.HCM",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Tổng Hợp",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Hoàng Văn E",
		            ContactEmail = "contact@tonghop.vn",
		            ContactPhone = "0976543210",
		            Address = "654 Đường Nguyễn Trãi, TP.HCM",
		            Country = "Việt Nam",
		            City = "TP.HCM",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Khoa Học",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Đỗ Thị F",
		            ContactEmail = "info@khoahoc.vn",
		            ContactPhone = "0901234567",
		            Address = "987 Đường Hai Bà Trưng, Hà Nội",
		            Country = "Việt Nam",
		            City = "Hà Nội",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Văn Nghệ",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Vũ Văn G",
		            ContactEmail = "contact@vannghe.vn",
		            ContactPhone = "0945678901",
		            Address = "135 Đường Trần Hưng Đạo, Đà Nẵng",
		            Country = "Việt Nam",
		            City = "Đà Nẵng",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Sách Bách Khoa",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Ngô Thị H",
		            ContactEmail = "info@sachbachkhoa.vn",
		            ContactPhone = "0967890123",
		            Address = "246 Đường Lý Thường Kiệt, Hà Nội",
		            Country = "Việt Nam",
		            City = "Hà Nội",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Văn Hóa",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Bùi Văn I",
		            ContactEmail = "contact@vanhoa.vn",
		            ContactPhone = "0923456789",
		            Address = "357 Đường Phan Đình Phùng, TP.HCM",
		            Country = "Việt Nam",
		            City = "TP.HCM",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        },
		        new Supplier
		        {
		            SupplierName = "Nhà Xuất Bản Cổ Truyền",
		            SupplierType = SupplierType.Publisher,
		            ContactPerson = "Trương Thị J",
		            ContactEmail = "info@cotruyen.vn",
		            ContactPhone = "0911122233",
		            Address = "468 Đường Nguyễn Huệ, TP.HCM",
		            Country = "Việt Nam",
		            City = "TP.HCM",
		            IsDeleted = false,
		            IsActive = true,
		            CreatedAt = DateTime.Now
		        }
		    };

		    // Add range 
		    await _context.Suppliers.AddRangeAsync(suppliers);
		    // Save DB
		    var saveSucc = await _context.SaveChangesAsync() > 0;
		    if (saveSucc) _logger.Information("Seed supplier successfully.");
		}
		
        //	Summary:
        //		Seed transactions
        private async Task SeedTransactionsAsync()
        {
	        // Retrieve all existing users
	        var users = await _context.Users
		        .Where(u => u.Role.EnglishName != nameof(Role.Administration))
		        .ToListAsync();
	        if (users.Any())
	        {
		        var rnd = new Random();
		        var transactions = new List<Transaction>();
		        
		        // Define start and end dates for transaction generation
		        var startDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
		        var endDate = DateTime.Today;
		        var totalDays = (endDate - startDate).Days;

				// Generate transaction code
		        string GenerateTransactionCode(int minLength = 10, int maxLength = 12)
		        {
			        int codeLength = rnd.Next(minLength, maxLength + 1);
			        var code = new char[codeLength];
			        for (int i = 0; i < codeLength; i++)
			        {
				        code[i] = (char)('0' + rnd.Next(10));
			        }
			        return new string(code);
		        }

		        for (int i = 0; i < 500; i++)
		        {
			        var randomDays = rnd.Next(totalDays + 1);
			        var transactionDate = startDate.AddDays(randomDays);

			        transactions.Add(new Transaction
			        {
				        TransactionDate = transactionDate,
				        Amount = rnd.Next(5000, 500000),
				        TransactionType = (i % 2 == 0) ? TransactionType.Fine : TransactionType.DigitalBorrow,
				        CreatedAt = transactionDate.AddHours(rnd.Next(0, 24)).AddMinutes(rnd.Next(0, 60)),
				        TransactionCode = GenerateTransactionCode(), 
				        UserId = users[rnd.Next(users.Count)].UserId,
				        TransactionStatus = TransactionStatus.Paid,
						TransactionMethod = (i % 2 == 0) ? TransactionMethod.DigitalPayment : TransactionMethod.Cash
			        });
		        }
		        
		        await _context.Transactions.AddRangeAsync(transactions);
		        // Save DB
		        var saveSucc = await _context.SaveChangesAsync() > 0;
		        if (saveSucc) _logger.Information("Seed transactions successfully.");
	        }
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
		
		// Barcode generator
		public static string GenerateBarcode(string prefix)
		{
			return $"{prefix}-{Guid.NewGuid().ToString("N").Substring(20).ToUpper()}";
		}
		
		public static string AddWhitespaceToString(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			// Use a regex to identify boundaries between lowercase and uppercase letters
			string result = Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");

			return result;
		}
	}
}
