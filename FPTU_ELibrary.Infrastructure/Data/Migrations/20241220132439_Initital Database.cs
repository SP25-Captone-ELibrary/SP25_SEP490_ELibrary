using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPTU_ELibrary.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InititalDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Author",
                columns: table => new
                {
                    author_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    author_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    author_image = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    biography = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    dob = table.Column<DateTime>(type: "datetime", nullable: true),
                    date_of_death = table.Column<DateTime>(type: "datetime", nullable: true),
                    nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Author_AuthorId", x => x.author_id);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    english_name = table.Column<string>(type: "nvarchar(155)", maxLength: 155, nullable: false),
                    vietnamese_name = table.Column<string>(type: "nvarchar(155)", maxLength: 155, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category_CategoryId", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "Fine_Policy",
                columns: table => new
                {
                    fine_policy_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    condition_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    fine_amount_per_day = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    fixed_fine_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinePolicy_FinePolicyId", x => x.fine_policy_id);
                });

            migrationBuilder.CreateTable(
                name: "Library_Floor",
                columns: table => new
                {
                    floor_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    floor_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryFloor_FloorId", x => x.floor_id);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_public = table.Column<bool>(type: "bit", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notification_type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification_NotificationId", x => x.notification_id);
                });

            migrationBuilder.CreateTable(
                name: "System_Feature",
                columns: table => new
                {
                    feature_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vietnamese_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    english_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemFeature_FeatureId", x => x.feature_id);
                });

            migrationBuilder.CreateTable(
                name: "System_Message",
                columns: table => new
                {
                    msg_id = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    msg_content = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    VI = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    EN = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    RU = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    JA = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    KO = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    modified_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemMessage_MsgId", x => x.msg_id);
                });

            migrationBuilder.CreateTable(
                name: "System_Permission",
                columns: table => new
                {
                    permission_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    permission_level = table.Column<int>(type: "int", nullable: false),
                    vietnamese_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    english_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemPermission_PermissionId", x => x.permission_id);
                });

            migrationBuilder.CreateTable(
                name: "System_Role",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vietnamese_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    english_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    role_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRole_RoleId", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "Library_Zone",
                columns: table => new
                {
                    zone_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    floor_id = table.Column<int>(type: "int", nullable: false),
                    zone_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    x_coordinate = table.Column<double>(type: "float", nullable: false),
                    y_coordinate = table.Column<double>(type: "float", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryZone_ZoneId", x => x.zone_id);
                    table.ForeignKey(
                        name: "FK_LibraryZone_FloorId",
                        column: x => x.floor_id,
                        principalTable: "Library_Floor",
                        principalColumn: "floor_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    employee_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    hire_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    termination_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    avatar = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    gender = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    dob = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    modified_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    modified_by = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    two_factor_secret_key = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    two_factor_backup_codes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    phone_verification_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    email_verification_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    phone_verification_expiry = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee_EmployeeId", x => x.employee_id);
                    table.ForeignKey(
                        name: "FK_Employee_RoleId",
                        column: x => x.role_id,
                        principalTable: "System_Role",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Role_Permission",
                columns: table => new
                {
                    role_permission_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    feature_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission_RolePermissionId", x => x.role_permission_id);
                    table.ForeignKey(
                        name: "FK_RolePermission_FeatureId",
                        column: x => x.feature_id,
                        principalTable: "System_Feature",
                        principalColumn: "feature_id");
                    table.ForeignKey(
                        name: "FK_RolePermission_PermissionId",
                        column: x => x.permission_id,
                        principalTable: "System_Permission",
                        principalColumn: "permission_id");
                    table.ForeignKey(
                        name: "FK_RolePermission_RoleId",
                        column: x => x.role_id,
                        principalTable: "System_Role",
                        principalColumn: "role_id");
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    user_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    avatar = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    gender = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    dob = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    modified_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    modified_by = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    two_factor_secret_key = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    two_factor_backup_codes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    phone_verification_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    email_verification_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    phone_verification_expiry = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_UserId", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_SystemRole_RoleId",
                        column: x => x.role_id,
                        principalTable: "System_Role",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Library_Path",
                columns: table => new
                {
                    path_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    from_zone_id = table.Column<int>(type: "int", nullable: false),
                    to_zone_id = table.Column<int>(type: "int", nullable: false),
                    distance = table.Column<double>(type: "float", nullable: false),
                    path_description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryPath_PathId", x => x.path_id);
                    table.ForeignKey(
                        name: "FK_LibraryPath_FromZoneId",
                        column: x => x.from_zone_id,
                        principalTable: "Library_Zone",
                        principalColumn: "zone_id");
                    table.ForeignKey(
                        name: "FK_LibraryPath_ToZoneId",
                        column: x => x.to_zone_id,
                        principalTable: "Library_Zone",
                        principalColumn: "zone_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Section",
                columns: table => new
                {
                    section_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    zone_id = table.Column<int>(type: "int", nullable: false),
                    section_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibrarySection_SectionId", x => x.section_id);
                    table.ForeignKey(
                        name: "FK_LibrarySection_ZoneId",
                        column: x => x.zone_id,
                        principalTable: "Library_Zone",
                        principalColumn: "zone_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Book",
                columns: table => new
                {
                    book_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    sub_title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    is_draft = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    create_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Book_BookId", x => x.book_id);
                    table.ForeignKey(
                        name: "FK_Book_CreateBy",
                        column: x => x.create_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_Book_UpdateBy",
                        column: x => x.updated_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "Notification_Recipient",
                columns: table => new
                {
                    notification_recipient_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    notification_id = table.Column<int>(type: "int", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_read = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipient_NotificationRecipientId", x => x.notification_recipient_id);
                    table.ForeignKey(
                        name: "FK_NotificationRecipient_NotificationId",
                        column: x => x.notification_id,
                        principalTable: "Notification",
                        principalColumn: "notification_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationRecipient_UserId",
                        column: x => x.recipient_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Refresh_Token",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refresh_token_id = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    token_id = table.Column<string>(type: "nvarchar(36)", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    employee_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefreshCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Pk_RefreshToken_Id", x => x.id);
                    table.CheckConstraint("CK_RefreshToken_UserOrEmployee", "(user_id IS NOT NULL AND employee_id IS NULL) OR (user_id IS NULL AND employee_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_RefreshToken_EmployeeId",
                        column: x => x.employee_id,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_RefreshToken_UserId",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Shelf",
                columns: table => new
                {
                    shelf_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section_id = table.Column<int>(type: "int", nullable: false),
                    shelf_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryShelf_ShelfId", x => x.shelf_id);
                    table.ForeignKey(
                        name: "FK_LibraryShelf_SectionId",
                        column: x => x.section_id,
                        principalTable: "Library_Section",
                        principalColumn: "section_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Book_Category",
                columns: table => new
                {
                    book_category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCategory_BookCategoryId", x => x.book_category_id);
                    table.ForeignKey(
                        name: "FK_BookCategory_BookId",
                        column: x => x.book_id,
                        principalTable: "Book",
                        principalColumn: "book_id");
                    table.ForeignKey(
                        name: "FK_BookCategory_CategoryId",
                        column: x => x.category_id,
                        principalTable: "Category",
                        principalColumn: "category_id");
                });

            migrationBuilder.CreateTable(
                name: "Book_Edition",
                columns: table => new
                {
                    book_edition_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_id = table.Column<int>(type: "int", nullable: false),
                    edition_title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    edition_number = table.Column<int>(type: "int", nullable: false),
                    page_count = table.Column<int>(type: "int", nullable: false),
                    language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    publication_year = table.Column<int>(type: "int", nullable: false),
                    edition_summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    cover_image = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    format = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    publisher = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    isbn = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    can_borrow = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    create_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookEdition_BookEditionId", x => x.book_edition_id);
                    table.ForeignKey(
                        name: "FK_BookEdition_Book",
                        column: x => x.book_id,
                        principalTable: "Book",
                        principalColumn: "book_id");
                    table.ForeignKey(
                        name: "FK_BookEdition_CreateBy",
                        column: x => x.create_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "Learning_Material",
                columns: table => new
                {
                    learning_material_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    material_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    condition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    shelf_id = table.Column<int>(type: "int", nullable: true),
                    total_quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    available_quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    manufacturer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    warranty_period = table.Column<DateOnly>(type: "date", nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    create_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByNavigationEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningMaterial_LearningMaterialId", x => x.learning_material_id);
                    table.ForeignKey(
                        name: "FK_LearningMaterial_CreateBy",
                        column: x => x.create_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_LearningMaterial_ShelfId",
                        column: x => x.shelf_id,
                        principalTable: "Library_Shelf",
                        principalColumn: "shelf_id");
                    table.ForeignKey(
                        name: "FK_Learning_Material_Employee_UpdatedByNavigationEmployeeId",
                        column: x => x.UpdatedByNavigationEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "Book_Edition_Author",
                columns: table => new
                {
                    book_edition_author_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_edition_id = table.Column<int>(type: "int", nullable: false),
                    author_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookAuthorEdition_BookEditionAuthorId", x => x.book_edition_author_id);
                    table.ForeignKey(
                        name: "FK_BookEditionAuthor_AuthorId",
                        column: x => x.author_id,
                        principalTable: "Author",
                        principalColumn: "author_id");
                    table.ForeignKey(
                        name: "FK_BookEditionAuthor_BookId",
                        column: x => x.book_edition_id,
                        principalTable: "Book_Edition",
                        principalColumn: "book_edition_id");
                });

            migrationBuilder.CreateTable(
                name: "Book_Edition_Copy",
                columns: table => new
                {
                    book_edition_copy_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_edition_id = table.Column<int>(type: "int", nullable: false),
                    shelf_id = table.Column<int>(type: "int", nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookEditionCopy_BookEditionCopyId", x => x.book_edition_copy_id);
                    table.ForeignKey(
                        name: "FK_BookEditionCopy_BookEditionId",
                        column: x => x.book_edition_id,
                        principalTable: "Book_Edition",
                        principalColumn: "book_edition_id");
                    table.ForeignKey(
                        name: "FK_BookEditionCopy_ShelfId",
                        column: x => x.shelf_id,
                        principalTable: "Library_Shelf",
                        principalColumn: "shelf_id");
                });

            migrationBuilder.CreateTable(
                name: "Book_Edition_Inventory",
                columns: table => new
                {
                    book_edition_id = table.Column<int>(type: "int", nullable: false),
                    total_copies = table.Column<int>(type: "int", nullable: false),
                    available_copies = table.Column<int>(type: "int", nullable: false),
                    request_copies = table.Column<int>(type: "int", nullable: false),
                    reserved_copies = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookEditionInventory_BookEditionId", x => x.book_edition_id);
                    table.ForeignKey(
                        name: "FK_BookEditionInventory_BookEditionId",
                        column: x => x.book_edition_id,
                        principalTable: "Book_Edition",
                        principalColumn: "book_edition_id");
                });

            migrationBuilder.CreateTable(
                name: "Book_Resource",
                columns: table => new
                {
                    resource_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_edition_id = table.Column<int>(type: "int", nullable: false),
                    resource_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    resource_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    resource_size = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    file_format = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    provider_public_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    provider_metadata = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookResource_BookResourceId", x => x.resource_id);
                    table.ForeignKey(
                        name: "FK_BookResource_BookEditionId",
                        column: x => x.book_edition_id,
                        principalTable: "Book_Edition",
                        principalColumn: "book_edition_id");
                    table.ForeignKey(
                        name: "FK_BookResource_CreatedBy",
                        column: x => x.created_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "Book_Review",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_edition_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rating_value = table.Column<int>(type: "int", nullable: false),
                    review_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookReview_ReviewId", x => x.review_id);
                    table.ForeignKey(
                        name: "FK_BookReview_BookEditionId",
                        column: x => x.book_edition_id,
                        principalTable: "Book_Edition",
                        principalColumn: "book_edition_id");
                    table.ForeignKey(
                        name: "FK_BookReview_UserId",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Reservation_Queue",
                columns: table => new
                {
                    queue_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_edition_id = table.Column<int>(type: "int", nullable: false),
                    expected_available_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    reserved_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reservation_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    deposit_expiration_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    deposit_fee = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    deposit_paid = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    queue_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationQueue_QueueId", x => x.queue_id);
                    table.ForeignKey(
                        name: "FK_ReservationQueue_BookEditionId",
                        column: x => x.book_edition_id,
                        principalTable: "Book_Edition",
                        principalColumn: "book_edition_id");
                    table.ForeignKey(
                        name: "FK_ReservationQueue_ReservedBy",
                        column: x => x.reserved_by,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "User_Favorites",
                columns: table => new
                {
                    favorite_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    book_edition_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorites_FavoriteId", x => x.favorite_id);
                    table.ForeignKey(
                        name: "FK_UserFavorites_BookEditionId",
                        column: x => x.book_edition_id,
                        principalTable: "Book_Edition",
                        principalColumn: "book_edition_id");
                    table.ForeignKey(
                        name: "FK_UserFavorites_UserId",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Borrow_Request",
                columns: table => new
                {
                    borrow_request_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_edition_id = table.Column<int>(type: "int", nullable: true),
                    book_edition_copy_id = table.Column<int>(type: "int", nullable: true),
                    learning_material_id = table.Column<int>(type: "int", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    borrow_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    deposit_fee = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    deposit_paid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRequest_BorrowRequestId", x => x.borrow_request_id);
                    table.CheckConstraint("CK_BorrowRequest_BookOrMaterial", "(book_edition_id IS NOT NULL AND learning_material_id IS NULL) OR (book_edition_id IS NULL AND learning_material_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BorrowRequest_BookEditionCopyId",
                        column: x => x.book_edition_copy_id,
                        principalTable: "Book_Edition_Copy",
                        principalColumn: "book_edition_copy_id");
                    table.ForeignKey(
                        name: "FK_BorrowRequest_BookEditionId",
                        column: x => x.book_edition_id,
                        principalTable: "Book_Edition",
                        principalColumn: "book_edition_id");
                    table.ForeignKey(
                        name: "FK_BorrowRequest_LearningMaterialId",
                        column: x => x.learning_material_id,
                        principalTable: "Learning_Material",
                        principalColumn: "learning_material_id");
                    table.ForeignKey(
                        name: "FK_BorrowRequest_UserId",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Copy_Condition_History",
                columns: table => new
                {
                    condition_history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_edition_copy_id = table.Column<int>(type: "int", nullable: false),
                    condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    change_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    changed_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Book_Condition_History", x => x.condition_history_id);
                    table.ForeignKey(
                        name: "FK_ConditionHistory_BookEditionCopyId",
                        column: x => x.book_edition_copy_id,
                        principalTable: "Book_Edition_Copy",
                        principalColumn: "book_edition_copy_id");
                    table.ForeignKey(
                        name: "FK_ConditionHistory_ChangedBy",
                        column: x => x.changed_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "Borrow_Record",
                columns: table => new
                {
                    borrow_record_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_edition_copy_id = table.Column<int>(type: "int", nullable: true),
                    learning_material_id = table.Column<int>(type: "int", nullable: true),
                    borrower_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    borrow_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    borrow_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    due_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    return_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    extension_limit = table.Column<int>(type: "int", nullable: false),
                    borrow_condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    return_condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    condition_check_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    request_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    processed_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    proceesed_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    deposit_fee = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    deposit_refunded = table.Column<bool>(type: "bit", nullable: true),
                    refund_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    BorrowRequestId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRecord_BorrowRecordId", x => x.borrow_record_id);
                    table.CheckConstraint("CK_BorrowRecord_BookOrMaterial", "(book_edition_copy_id IS NOT NULL AND learning_material_id IS NULL) OR (book_edition_copy_id IS NULL AND learning_material_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BorrowRecord_BookEditionCopyId",
                        column: x => x.book_edition_copy_id,
                        principalTable: "Book_Edition_Copy",
                        principalColumn: "book_edition_copy_id");
                    table.ForeignKey(
                        name: "FK_BorrowRecord_BorrowerId",
                        column: x => x.borrower_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_BorrowRecord_LearningMaterialId",
                        column: x => x.learning_material_id,
                        principalTable: "Learning_Material",
                        principalColumn: "learning_material_id");
                    table.ForeignKey(
                        name: "FK_BorrowRecord_ProcessedBy",
                        column: x => x.proceesed_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Borrow_Record_Borrow_Request_BorrowRequestId",
                        column: x => x.BorrowRequestId,
                        principalTable: "Borrow_Request",
                        principalColumn: "borrow_request_id");
                });

            migrationBuilder.CreateTable(
                name: "Fine",
                columns: table => new
                {
                    fine_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    borrow_record_id = table.Column<int>(type: "int", nullable: false),
                    fine_policy_id = table.Column<int>(type: "int", nullable: false),
                    fine_note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    paid_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    payment_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    create_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    compensation_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    compensation_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    compensate_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    compensate_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    compensation_note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fine_FineId", x => x.fine_id);
                    table.ForeignKey(
                        name: "FK_Fine_BorrowRecordId",
                        column: x => x.borrow_record_id,
                        principalTable: "Borrow_Record",
                        principalColumn: "borrow_record_id");
                    table.ForeignKey(
                        name: "FK_Fine_CreateBY",
                        column: x => x.create_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_Fine_FindPolicyId",
                        column: x => x.fine_policy_id,
                        principalTable: "Fine_Policy",
                        principalColumn: "fine_policy_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Book_create_by",
                table: "Book",
                column: "create_by");

            migrationBuilder.CreateIndex(
                name: "IX_Book_updated_by",
                table: "Book",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Category_book_id",
                table: "Book_Category",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Category_category_id",
                table: "Book_Category",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Edition_book_id",
                table: "Book_Edition",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Edition_create_by",
                table: "Book_Edition",
                column: "create_by");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Edition_Author_author_id",
                table: "Book_Edition_Author",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Edition_Author_book_edition_id",
                table: "Book_Edition_Author",
                column: "book_edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Edition_Copy_book_edition_id",
                table: "Book_Edition_Copy",
                column: "book_edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Edition_Copy_shelf_id",
                table: "Book_Edition_Copy",
                column: "shelf_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Resource_book_edition_id",
                table: "Book_Resource",
                column: "book_edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Resource_created_by",
                table: "Book_Resource",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Review_book_edition_id",
                table: "Book_Review",
                column: "book_edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Review_user_id",
                table: "Book_Review",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_book_edition_copy_id",
                table: "Borrow_Record",
                column: "book_edition_copy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_borrower_id",
                table: "Borrow_Record",
                column: "borrower_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_BorrowRequestId",
                table: "Borrow_Record",
                column: "BorrowRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_learning_material_id",
                table: "Borrow_Record",
                column: "learning_material_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_proceesed_by",
                table: "Borrow_Record",
                column: "proceesed_by");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_book_edition_copy_id",
                table: "Borrow_Request",
                column: "book_edition_copy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_book_edition_id",
                table: "Borrow_Request",
                column: "book_edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_learning_material_id",
                table: "Borrow_Request",
                column: "learning_material_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_user_id",
                table: "Borrow_Request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Copy_Condition_History_book_edition_copy_id",
                table: "Copy_Condition_History",
                column: "book_edition_copy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Copy_Condition_History_changed_by",
                table: "Copy_Condition_History",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_role_id",
                table: "Employee",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_Fine_borrow_record_id",
                table: "Fine",
                column: "borrow_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_Fine_create_by",
                table: "Fine",
                column: "create_by");

            migrationBuilder.CreateIndex(
                name: "IX_Fine_fine_policy_id",
                table: "Fine",
                column: "fine_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Learning_Material_create_by",
                table: "Learning_Material",
                column: "create_by");

            migrationBuilder.CreateIndex(
                name: "IX_Learning_Material_shelf_id",
                table: "Learning_Material",
                column: "shelf_id");

            migrationBuilder.CreateIndex(
                name: "IX_Learning_Material_UpdatedByNavigationEmployeeId",
                table: "Learning_Material",
                column: "UpdatedByNavigationEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Path_from_zone_id",
                table: "Library_Path",
                column: "from_zone_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Path_to_zone_id",
                table: "Library_Path",
                column: "to_zone_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Section_zone_id",
                table: "Library_Section",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Shelf_section_id",
                table: "Library_Shelf",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Zone_floor_id",
                table: "Library_Zone",
                column: "floor_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Recipient_notification_id",
                table: "Notification_Recipient",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Recipient_recipient_id",
                table: "Notification_Recipient",
                column: "recipient_id");

            migrationBuilder.CreateIndex(
                name: "IX_Refresh_Token_employee_id",
                table: "Refresh_Token",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_Refresh_Token_user_id",
                table: "Refresh_Token",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Queue_book_edition_id",
                table: "Reservation_Queue",
                column: "book_edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Queue_reserved_by",
                table: "Reservation_Queue",
                column: "reserved_by");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Permission_feature_id",
                table: "Role_Permission",
                column: "feature_id");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Permission_permission_id",
                table: "Role_Permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Permission_role_id",
                table: "Role_Permission",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_role_id",
                table: "User",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Favorites_book_edition_id",
                table: "User_Favorites",
                column: "book_edition_id");

            migrationBuilder.CreateIndex(
                name: "UQ_UserFavorites",
                table: "User_Favorites",
                columns: new[] { "user_id", "book_edition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Book_Category");

            migrationBuilder.DropTable(
                name: "Book_Edition_Author");

            migrationBuilder.DropTable(
                name: "Book_Edition_Inventory");

            migrationBuilder.DropTable(
                name: "Book_Resource");

            migrationBuilder.DropTable(
                name: "Book_Review");

            migrationBuilder.DropTable(
                name: "Copy_Condition_History");

            migrationBuilder.DropTable(
                name: "Fine");

            migrationBuilder.DropTable(
                name: "Library_Path");

            migrationBuilder.DropTable(
                name: "Notification_Recipient");

            migrationBuilder.DropTable(
                name: "Refresh_Token");

            migrationBuilder.DropTable(
                name: "Reservation_Queue");

            migrationBuilder.DropTable(
                name: "Role_Permission");

            migrationBuilder.DropTable(
                name: "System_Message");

            migrationBuilder.DropTable(
                name: "User_Favorites");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Author");

            migrationBuilder.DropTable(
                name: "Borrow_Record");

            migrationBuilder.DropTable(
                name: "Fine_Policy");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "System_Feature");

            migrationBuilder.DropTable(
                name: "System_Permission");

            migrationBuilder.DropTable(
                name: "Borrow_Request");

            migrationBuilder.DropTable(
                name: "Book_Edition_Copy");

            migrationBuilder.DropTable(
                name: "Learning_Material");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Book_Edition");

            migrationBuilder.DropTable(
                name: "Library_Shelf");

            migrationBuilder.DropTable(
                name: "Book");

            migrationBuilder.DropTable(
                name: "Library_Section");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "Library_Zone");

            migrationBuilder.DropTable(
                name: "System_Role");

            migrationBuilder.DropTable(
                name: "Library_Floor");
        }
    }
}
