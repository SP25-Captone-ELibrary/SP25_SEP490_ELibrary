using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPTU_ELibrary.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initialdatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AI_Training_Session",
                columns: table => new
                {
                    training_session_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    model = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    total_trained_item = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    total_trained_time = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    training_status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(250)", nullable: true),
                    training_percentage = table.Column<int>(type: "int", nullable: true),
                    train_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    train_by = table.Column<string>(type: "nvarchar(250)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AITrainingSession_TrainingSessionId", x => x.training_session_id);
                });

            migrationBuilder.CreateTable(
                name: "Audit_Trail",
                columns: table => new
                {
                    audit_trail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    entity_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entity_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    trail_type = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    date_utc = table.Column<DateTime>(type: "datetime", nullable: false),
                    old_values = table.Column<string>(type: "nvarchar(3000)", nullable: false),
                    new_values = table.Column<string>(type: "nvarchar(3000)", nullable: false),
                    changed_columns = table.Column<string>(type: "nvarchar(800)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrail_AuditTrailId", x => x.audit_trail_id);
                });

            migrationBuilder.CreateTable(
                name: "Author",
                columns: table => new
                {
                    author_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    author_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    author_image = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    biography = table.Column<string>(type: "nvarchar(3000)", nullable: true),
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
                    prefix = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    english_name = table.Column<string>(type: "nvarchar(155)", maxLength: 155, nullable: false),
                    vietnamese_name = table.Column<string>(type: "nvarchar(155)", maxLength: 155, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    is_allow_ai_training = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    total_borrow_days = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
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
                    fine_policy_title = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    condition_type = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    min_damage_pct = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    max_damage_pct = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    processing_fee = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    daily_rate = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    charge_pct = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    fine_amount_per_day = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    fixed_fine_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinePolicy_FinePolicyId", x => x.fine_policy_id);
                });

            migrationBuilder.CreateTable(
                name: "Library_Card",
                columns: table => new
                {
                    library_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    full_name = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    avatar = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: false),
                    barcode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    issuance_method = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    is_allow_borrow_more = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    max_item_once_time = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    allow_borrow_more_reason = table.Column<string>(type: "nvarchar(250)", nullable: true),
                    is_reminder_sent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    total_missed_pick_up = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_extended = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    extension_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    issue_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    suspension_end_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    suspension_reason = table.Column<string>(type: "nvarchar(250)", nullable: true),
                    reject_reason = table.Column<string>(type: "nvarchar(250)", nullable: true),
                    is_archived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    archive_reason = table.Column<string>(type: "nvarchar(250)", nullable: true),
                    previous_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    transaction_code = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryCard_LibraryCardId", x => x.library_card_id);
                });

            migrationBuilder.CreateTable(
                name: "Library_Card_Package",
                columns: table => new
                {
                    library_card_package_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    package_name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    duration_in_months = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryCardPackage_LibraryCardPackageId", x => x.library_card_package_id);
                });

            migrationBuilder.CreateTable(
                name: "Library_Closure_Day",
                columns: table => new
                {
                    closure_day_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    day = table.Column<int>(type: "int", nullable: false),
                    month = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: true),
                    vie_description = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    eng_description = table.Column<string>(type: "nvarchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryClosureDay", x => x.closure_day_id);
                });

            migrationBuilder.CreateTable(
                name: "Library_Floor",
                columns: table => new
                {
                    floor_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FloorNumber = table.Column<int>(type: "int", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryFloor_FloorId", x => x.floor_id);
                });

            migrationBuilder.CreateTable(
                name: "Library_Item_Condition",
                columns: table => new
                {
                    ConditionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    english_name = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    vietnamese_name = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItemCondition_ConditionId", x => x.ConditionId);
                });

            migrationBuilder.CreateTable(
                name: "Library_Item_Group",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ai_training_code = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    classification_number = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    cutter_number = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    sub_title = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    author = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    topical_terms = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    TrainedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItemGroup_GroupId", x => x.group_id);
                });

            migrationBuilder.CreateTable(
                name: "Library_Resource",
                columns: table => new
                {
                    resource_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    resource_title = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    resource_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    resource_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    resource_size = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    file_format = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    provider_public_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    provider_metadata = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    default_borrow_duration_days = table.Column<int>(type: "int", nullable: false),
                    borrow_price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    s3_original_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryResource_ResourceId", x => x.resource_id);
                });

            migrationBuilder.CreateTable(
                name: "Payment_Method",
                columns: table => new
                {
                    payment_method_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    method_name = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethod_PaymentMethodId", x => x.payment_method_id);
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    supplier_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    supplier_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    contact_person = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    contact_email = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    contact_phone = table.Column<string>(type: "nvarchar(12)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(300)", nullable: true),
                    country = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_SupplierId", x => x.supplier_id);
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
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    vietnamese_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    english_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    role_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRole_RoleId", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "Borrow_Request",
                columns: table => new
                {
                    borrow_request_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    library_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    cancellation_reason = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    is_reminder_sent = table.Column<bool>(type: "bit", nullable: false),
                    total_request_item = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRequest_BorrowRequestId", x => x.borrow_request_id);
                    table.ForeignKey(
                        name: "FK_BorrowRequest_LibraryCardId",
                        column: x => x.library_card_id,
                        principalTable: "Library_Card",
                        principalColumn: "library_card_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Zone",
                columns: table => new
                {
                    zone_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    floor_id = table.Column<int>(type: "int", nullable: false),
                    eng_zone_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    vie_zone_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    eng_description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    vie_description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    total_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryZone_ZoneId", x => x.zone_id);
                    table.ForeignKey(
                        name: "FK_LibraryZone_FloorId",
                        column: x => x.floor_id,
                        principalTable: "Library_Floor",
                        principalColumn: "floor_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Resource_Url",
                columns: table => new
                {
                    library_resource_url_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    part_number = table.Column<int>(type: "int", nullable: false),
                    url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    resource_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryResourceUrl_LibraryResourceUrlId", x => x.library_resource_url_id);
                    table.ForeignKey(
                        name: "FK_LibraryResourceUrl_ResourceId",
                        column: x => x.resource_id,
                        principalTable: "Library_Resource",
                        principalColumn: "resource_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Warehouse_Tracking",
                columns: table => new
                {
                    tracking_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    supplier_id = table.Column<int>(type: "int", nullable: true),
                    receipt_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    total_item = table.Column<int>(type: "int", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0.0m),
                    tracking_type = table.Column<int>(type: "int", maxLength: 30, nullable: false),
                    transfer_location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<int>(type: "int", maxLength: 30, nullable: false),
                    expected_return_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    actual_return_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    data_finalization_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    finalized_file = table.Column<string>(type: "varchar(2048)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTracking_TrackingId", x => x.tracking_id);
                    table.ForeignKey(
                        name: "FK_WarehouseTracking_SupplierId",
                        column: x => x.supplier_id,
                        principalTable: "Supplier",
                        principalColumn: "supplier_id");
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
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                    permission_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
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
                    role_id = table.Column<int>(type: "int", nullable: false),
                    library_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_employee_created = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                    modified_by = table.Column<string>(type: "nvarchar(155)", nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                        name: "FK_LibraryCard_LibraryCardId",
                        column: x => x.library_card_id,
                        principalTable: "Library_Card",
                        principalColumn: "library_card_id");
                    table.ForeignKey(
                        name: "FK_SystemRole_RoleId",
                        column: x => x.role_id,
                        principalTable: "System_Role",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Library_Section",
                columns: table => new
                {
                    section_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    zone_id = table.Column<int>(type: "int", nullable: false),
                    eng_section_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    vie_section_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    shelf_prefix = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    classification_number_range_from = table.Column<decimal>(type: "decimal(10,4)", nullable: false, defaultValue: 0m),
                    classification_number_range_to = table.Column<decimal>(type: "decimal(10,4)", nullable: false, defaultValue: 0m),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    is_children_section = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_reference_section = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_journal_section = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibrarySection_SectionId", x => x.section_id);
                    table.ForeignKey(
                        name: "FK_LibrarySection_ZoneId",
                        column: x => x.zone_id,
                        principalTable: "Library_Zone",
                        principalColumn: "zone_id");
                });

            migrationBuilder.CreateTable(
                name: "Warehouse_Tracking_Inventory",
                columns: table => new
                {
                    tracking_id = table.Column<int>(type: "int", nullable: false),
                    total_item = table.Column<int>(type: "int", nullable: false),
                    total_instance_item = table.Column<int>(type: "int", nullable: false),
                    total_cataloged_item = table.Column<int>(type: "int", nullable: false),
                    total_cataloged_instance_item = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTrackingInventory_TrackingId", x => x.tracking_id);
                    table.ForeignKey(
                        name: "FK_WarehouseTrackingInventory_TrackingId",
                        column: x => x.tracking_id,
                        principalTable: "Warehouse_Tracking",
                        principalColumn: "tracking_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Borrow_Record",
                columns: table => new
                {
                    borrow_record_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    borrow_request_id = table.Column<int>(type: "int", nullable: true),
                    library_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    borrow_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    borrow_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    self_service_borrow = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    self_service_return = table.Column<bool>(type: "bit", nullable: true),
                    total_record_item = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    proceesed_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRecord_BorrowRecordId", x => x.borrow_record_id);
                    table.ForeignKey(
                        name: "FK_BorrowRecord_BorrowRequestId",
                        column: x => x.borrow_request_id,
                        principalTable: "Borrow_Request",
                        principalColumn: "borrow_request_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowRecord_LibraryCardId",
                        column: x => x.library_card_id,
                        principalTable: "Library_Card",
                        principalColumn: "library_card_id");
                    table.ForeignKey(
                        name: "FK_BorrowRecord_ProcessedBy",
                        column: x => x.proceesed_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    message = table.Column<string>(type: "nvarchar(4000)", nullable: false),
                    is_public = table.Column<bool>(type: "bit", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    notification_type = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification_NotificationId", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_Notification_CreatedBy",
                        column: x => x.created_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "Digital_Borrow",
                columns: table => new
                {
                    digital_borrow_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    resource_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    register_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    is_extended = table.Column<bool>(type: "bit", nullable: false),
                    extension_count = table.Column<int>(type: "int", nullable: false),
                    s3_watermarked_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalBorrow_DigitalBorrowId", x => x.digital_borrow_id);
                    table.ForeignKey(
                        name: "FK_DigitalBorrow_ResourceId",
                        column: x => x.resource_id,
                        principalTable: "Library_Resource",
                        principalColumn: "resource_id");
                    table.ForeignKey(
                        name: "FK_DigitalBorrow_UserId",
                        column: x => x.user_id,
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
                    eng_shelf_name = table.Column<string>(type: "nvarchar(155)", maxLength: 155, nullable: true),
                    vie_shelf_name = table.Column<string>(type: "nvarchar(155)", maxLength: 155, nullable: true),
                    classification_number_range_from = table.Column<decimal>(type: "decimal(10,4)", nullable: false, defaultValue: 0m),
                    classification_number_range_to = table.Column<decimal>(type: "decimal(10,4)", nullable: false, defaultValue: 0m),
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
                        principalColumn: "section_id");
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
                name: "Digital_Borrow_Extension_History",
                columns: table => new
                {
                    digital_extension_history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DigitalBorrowId = table.Column<int>(type: "int", nullable: false),
                    extension_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    new_expiry_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    extension_fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    extension_number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalBorrowExtensionHistory_DigitalExtensionHistoryId", x => x.digital_extension_history_id);
                    table.ForeignKey(
                        name: "FK_DigitalBorrowExtensionHistory_DigitalBorrowId",
                        column: x => x.DigitalBorrowId,
                        principalTable: "Digital_Borrow",
                        principalColumn: "digital_borrow_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Item",
                columns: table => new
                {
                    library_item_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    sub_title = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    responsibility = table.Column<string>(type: "nvarchar(155)", nullable: true),
                    edition = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    edition_number = table.Column<int>(type: "int", nullable: true),
                    language = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    origin_language = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    summary = table.Column<string>(type: "nvarchar(3000)", nullable: true),
                    cover_image = table.Column<string>(type: "varchar(2048)", nullable: true),
                    publication_year = table.Column<int>(type: "int", nullable: false),
                    publisher = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    publication_place = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    classification_number = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    cutter_number = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    isbn = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    ean = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    estimated_price = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    page_count = table.Column<int>(type: "int", nullable: true),
                    physical_details = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    dimensions = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    accompanying_material = table.Column<string>(type: "nvarchar(155)", nullable: true),
                    genres = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    general_note = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    bibliographical_note = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    topical_terms = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    additional_authors = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    shelf_id = table.Column<int>(type: "int", nullable: true),
                    group_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    can_borrow = table.Column<bool>(type: "bit", nullable: false),
                    is_trained = table.Column<bool>(type: "bit", nullable: false),
                    trained_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItem_LibraryItemId", x => x.library_item_id);
                    table.ForeignKey(
                        name: "FK_LibraryItem_CategoryId",
                        column: x => x.category_id,
                        principalTable: "Category",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryItem_GroupId",
                        column: x => x.group_id,
                        principalTable: "Library_Item_Group",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "FK_LibraryItem_ShelfId",
                        column: x => x.shelf_id,
                        principalTable: "Library_Shelf",
                        principalColumn: "shelf_id");
                });

            migrationBuilder.CreateTable(
                name: "AI_Training_Detail",
                columns: table => new
                {
                    training_detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    training_session_id = table.Column<int>(type: "int", nullable: false),
                    library_item_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AITrainingDetail_TrainingDetailId", x => x.training_detail_id);
                    table.ForeignKey(
                        name: "FK_AITrainingDetail_LibraryItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                    table.ForeignKey(
                        name: "FK_AITrainingDetail_TrainingSessionId",
                        column: x => x.training_session_id,
                        principalTable: "AI_Training_Session",
                        principalColumn: "training_session_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Item_Author",
                columns: table => new
                {
                    library_item_author_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    library_item_id = table.Column<int>(type: "int", nullable: false),
                    author_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItemAuthor_LibraryItemAuthorId", x => x.library_item_author_id);
                    table.ForeignKey(
                        name: "FK_LibraryItemAuthor_AuthorId",
                        column: x => x.author_id,
                        principalTable: "Author",
                        principalColumn: "author_id");
                    table.ForeignKey(
                        name: "FK_LibraryItemAuthor_LibraryItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Item_Instance",
                columns: table => new
                {
                    library_item_instance_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    library_item_id = table.Column<int>(type: "int", nullable: false),
                    barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    is_circulated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItemInstance_InstanceId", x => x.library_item_instance_id);
                    table.ForeignKey(
                        name: "FK_LibraryItemInstance_ItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Item_Inventory",
                columns: table => new
                {
                    library_item_id = table.Column<int>(type: "int", nullable: false),
                    total_units = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    available_units = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    request_units = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    borrowed_units = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    reserved_units = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    lost_units = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItemInventory_LibraryItemId", x => x.library_item_id);
                    table.ForeignKey(
                        name: "FK_LibraryItemInventory_LibraryItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Library_Item_Resource",
                columns: table => new
                {
                    library_item_resource_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    library_item_id = table.Column<int>(type: "int", nullable: false),
                    resource_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItemResource", x => x.library_item_resource_id);
                    table.ForeignKey(
                        name: "FK_LibraryItemResource_LibraryItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryItemResource_ResourceId",
                        column: x => x.resource_id,
                        principalTable: "Library_Resource",
                        principalColumn: "resource_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Library_Item_Review",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    library_item_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rating_value = table.Column<double>(type: "float", nullable: false),
                    review_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItemReview_ReviewId", x => x.review_id);
                    table.ForeignKey(
                        name: "FK_LibraryItemReview_ItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                    table.ForeignKey(
                        name: "FK_LibraryItemReview_UserId",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Supplement_Request_Detail",
                columns: table => new
                {
                    supplement_request_detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    author = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    publisher = table.Column<string>(type: "nvarchar(155)", nullable: false),
                    published_date = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(3000)", nullable: true),
                    isbn = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    page_count = table.Column<int>(type: "int", nullable: false),
                    dimensions = table.Column<string>(type: "nvarchar(155)", nullable: true),
                    estimated_price = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    categories = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    average_rating = table.Column<int>(type: "int", nullable: true),
                    ratings_count = table.Column<int>(type: "int", nullable: true),
                    language = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    cover_image = table.Column<string>(type: "varchar(2048)", nullable: true),
                    preview_link = table.Column<string>(type: "varchar(2048)", nullable: true),
                    info_link = table.Column<string>(type: "varchar(2048)", nullable: true),
                    supplement_request_reason = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    related_library_item_id = table.Column<int>(type: "int", nullable: false),
                    tracking_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementRequestDetail_SupplementRequestDetailId", x => x.supplement_request_detail_id);
                    table.ForeignKey(
                        name: "FK_SupplementRequestDetail_RelatedLibraryItemId",
                        column: x => x.related_library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                    table.ForeignKey(
                        name: "FK_SupplementRequestDetail_TrackingId",
                        column: x => x.tracking_id,
                        principalTable: "Warehouse_Tracking",
                        principalColumn: "tracking_id");
                });

            migrationBuilder.CreateTable(
                name: "User_Favorite",
                columns: table => new
                {
                    favorite_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    library_item_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorite_FavoriteId", x => x.favorite_id);
                    table.ForeignKey(
                        name: "FK_UserFavorite_ItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                    table.ForeignKey(
                        name: "FK_UserFavorite_UserId",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Warehouse_Tracking_Detail",
                columns: table => new
                {
                    tracking_detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    item_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    item_total = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    isbn = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0.0m),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0.0m),
                    tracking_id = table.Column<int>(type: "int", nullable: false),
                    library_item_id = table.Column<int>(type: "int", nullable: true),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    condition_id = table.Column<int>(type: "int", nullable: false),
                    stock_transaction_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    barcode_range_from = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    barcode_range_to = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    has_glue_barcode = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    supplement_request_reason = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    borrow_success_count = table.Column<int>(type: "int", nullable: true),
                    reserve_count = table.Column<int>(type: "int", nullable: true),
                    borrow_failed_count = table.Column<int>(type: "int", nullable: true),
                    BorrowFailedRate = table.Column<double>(type: "float", nullable: true),
                    available_units = table.Column<int>(type: "int", nullable: true),
                    need_units = table.Column<int>(type: "int", nullable: true),
                    average_need_satisfaction_rate = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTrackingDetail_TrackingDetailId", x => x.tracking_detail_id);
                    table.ForeignKey(
                        name: "FK_WarehouseTrackingDetail_CategoryId",
                        column: x => x.category_id,
                        principalTable: "Category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "FK_WarehouseTrackingDetail_ConditionId",
                        column: x => x.condition_id,
                        principalTable: "Library_Item_Condition",
                        principalColumn: "ConditionId");
                    table.ForeignKey(
                        name: "FK_WarehouseTrackingDetail_LibraryItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                    table.ForeignKey(
                        name: "FK_WarehouseTrackingDetail_TrackingId",
                        column: x => x.tracking_id,
                        principalTable: "Warehouse_Tracking",
                        principalColumn: "tracking_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AI_Training_Image",
                columns: table => new
                {
                    training_image_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    training_detail_id = table.Column<int>(type: "int", nullable: false),
                    image_url = table.Column<string>(type: "varchar(2048)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AITrainingImage_TrainingImageId", x => x.training_image_id);
                    table.ForeignKey(
                        name: "FK_AITrainingImage_TrainingDetailId",
                        column: x => x.training_detail_id,
                        principalTable: "AI_Training_Detail",
                        principalColumn: "training_detail_id");
                });

            migrationBuilder.CreateTable(
                name: "Borrow_Record_Detail",
                columns: table => new
                {
                    borrow_record_detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    borrow_record_id = table.Column<int>(type: "int", nullable: false),
                    library_item_instance_id = table.Column<int>(type: "int", nullable: false),
                    image_public_ids = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    condition_id = table.Column<int>(type: "int", nullable: false),
                    return_condition_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    due_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    return_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    condition_check_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_reminder_sent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    total_extension = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    processed_return_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRecordDetail_BorrowRecordDetailId", x => x.borrow_record_detail_id);
                    table.ForeignKey(
                        name: "FK_BorrowRecordDetail_BorrowRecordId",
                        column: x => x.borrow_record_id,
                        principalTable: "Borrow_Record",
                        principalColumn: "borrow_record_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowRecordDetail_ConditionId",
                        column: x => x.condition_id,
                        principalTable: "Library_Item_Condition",
                        principalColumn: "ConditionId");
                    table.ForeignKey(
                        name: "FK_BorrowRecordDetail_ItemInstanceId",
                        column: x => x.library_item_instance_id,
                        principalTable: "Library_Item_Instance",
                        principalColumn: "library_item_instance_id");
                    table.ForeignKey(
                        name: "FK_BorrowRecordDetail_ProcessedReturnBy",
                        column: x => x.processed_return_by,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_BorrowRecordDetail_ReturnConditionId",
                        column: x => x.return_condition_id,
                        principalTable: "Library_Item_Condition",
                        principalColumn: "ConditionId");
                });

            migrationBuilder.CreateTable(
                name: "Borrow_Request_Detail",
                columns: table => new
                {
                    borrow_request_detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    borrow_request_id = table.Column<int>(type: "int", nullable: false),
                    library_item_id = table.Column<int>(type: "int", nullable: false),
                    LibraryItemInstanceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRequestDetail_BorrowRequestDetailId", x => x.borrow_request_detail_id);
                    table.ForeignKey(
                        name: "FK_BorrowRequestDetail_BorrowRequestId",
                        column: x => x.borrow_request_id,
                        principalTable: "Borrow_Request",
                        principalColumn: "borrow_request_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowRequestDetail_ItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                    table.ForeignKey(
                        name: "FK_Borrow_Request_Detail_Library_Item_Instance_LibraryItemInstanceId",
                        column: x => x.LibraryItemInstanceId,
                        principalTable: "Library_Item_Instance",
                        principalColumn: "library_item_instance_id");
                });

            migrationBuilder.CreateTable(
                name: "Library_Item_Condition_History",
                columns: table => new
                {
                    condition_history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    library_item_instance_id = table.Column<int>(type: "int", nullable: false),
                    condition_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionHistory", x => x.condition_history_id);
                    table.ForeignKey(
                        name: "FK_ConditionHistory_ConditionId",
                        column: x => x.condition_id,
                        principalTable: "Library_Item_Condition",
                        principalColumn: "ConditionId");
                    table.ForeignKey(
                        name: "FK_ConditionHistory_LibraryItemInstanceId",
                        column: x => x.library_item_instance_id,
                        principalTable: "Library_Item_Instance",
                        principalColumn: "library_item_instance_id");
                });

            migrationBuilder.CreateTable(
                name: "Reservation_Queue",
                columns: table => new
                {
                    queue_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    library_item_id = table.Column<int>(type: "int", nullable: false),
                    library_item_instance_id = table.Column<int>(type: "int", nullable: true),
                    library_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    queue_status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    borrow_request_id = table.Column<int>(type: "int", nullable: true),
                    is_reserved_after_request_failed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    expected_available_date_min = table.Column<DateTime>(type: "datetime", nullable: true),
                    expected_available_date_max = table.Column<DateTime>(type: "datetime", nullable: true),
                    reservation_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    reservation_code = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    is_applied_label = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    collected_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    assigned_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    total_extend_pickup = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_notified = table.Column<bool>(type: "bit", nullable: false),
                    cancelled_by = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    cancellation_reason = table.Column<string>(type: "nvarchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationQueue_QueueId", x => x.queue_id);
                    table.ForeignKey(
                        name: "FK_ReservationQueue_BorrowRequestId",
                        column: x => x.borrow_request_id,
                        principalTable: "Borrow_Request",
                        principalColumn: "borrow_request_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationQueue_ItemId",
                        column: x => x.library_item_id,
                        principalTable: "Library_Item",
                        principalColumn: "library_item_id");
                    table.ForeignKey(
                        name: "FK_ReservationQueue_LibraryCardId",
                        column: x => x.library_card_id,
                        principalTable: "Library_Card",
                        principalColumn: "library_card_id");
                    table.ForeignKey(
                        name: "FK_ReservationQueue_LibraryItemInstanceId",
                        column: x => x.library_item_instance_id,
                        principalTable: "Library_Item_Instance",
                        principalColumn: "library_item_instance_id");
                });

            migrationBuilder.CreateTable(
                name: "Borrow_Detail_Extension_History",
                columns: table => new
                {
                    borrow_detail_extension_history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorrowRecordDetailId = table.Column<int>(type: "int", nullable: false),
                    extension_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    new_expiry_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    extension_number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowDetailExtensionHistory_BorrowDetailExtensionHistoryId", x => x.borrow_detail_extension_history_id);
                    table.ForeignKey(
                        name: "FK_BorrowDetailExtensionHistory_BorrowRecordDetailId",
                        column: x => x.BorrowRecordDetailId,
                        principalTable: "Borrow_Record_Detail",
                        principalColumn: "borrow_record_detail_id");
                });

            migrationBuilder.CreateTable(
                name: "Fine",
                columns: table => new
                {
                    fine_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    borrow_record_detail_id = table.Column<int>(type: "int", nullable: false),
                    fine_policy_id = table.Column<int>(type: "int", nullable: false),
                    fine_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    fine_note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    damage_pct = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiry_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fine_FineId", x => x.fine_id);
                    table.ForeignKey(
                        name: "FK_Fine_BorrowRecordDetailId",
                        column: x => x.borrow_record_detail_id,
                        principalTable: "Borrow_Record_Detail",
                        principalColumn: "borrow_record_detail_id");
                    table.ForeignKey(
                        name: "FK_Fine_CreateBY",
                        column: x => x.CreatedBy,
                        principalTable: "Employee",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_Fine_FindPolicyId",
                        column: x => x.fine_policy_id,
                        principalTable: "Fine_Policy",
                        principalColumn: "fine_policy_id");
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    transaction_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    transaction_code = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    transaction_status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    expired_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    canceled_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    cancellation_reason = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    fine_id = table.Column<int>(type: "int", nullable: true),
                    resource_id = table.Column<int>(type: "int", nullable: true),
                    library_card_package_id = table.Column<int>(type: "int", nullable: true),
                    transaction_method = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    payment_method_id = table.Column<int>(type: "int", nullable: true),
                    qr_code = table.Column<string>(type: "nvarchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction_TransactionId", x => x.transaction_id);
                    table.ForeignKey(
                        name: "FK_Transaction_FineId",
                        column: x => x.fine_id,
                        principalTable: "Fine",
                        principalColumn: "fine_id");
                    table.ForeignKey(
                        name: "FK_Transaction_LibraryCardPackageId",
                        column: x => x.library_card_package_id,
                        principalTable: "Library_Card_Package",
                        principalColumn: "library_card_package_id");
                    table.ForeignKey(
                        name: "FK_Transaction_PaymentMethodId",
                        column: x => x.payment_method_id,
                        principalTable: "Payment_Method",
                        principalColumn: "payment_method_id");
                    table.ForeignKey(
                        name: "FK_Transaction_ResourceId",
                        column: x => x.resource_id,
                        principalTable: "Library_Resource",
                        principalColumn: "resource_id");
                    table.ForeignKey(
                        name: "FK_Transaction_UserId",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Borrow_Request_Resource",
                columns: table => new
                {
                    borrow_request_resource_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    borrow_request_id = table.Column<int>(type: "int", nullable: false),
                    resource_id = table.Column<int>(type: "int", nullable: false),
                    resource_title = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    borrow_price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    default_borrow_duration_days = table.Column<int>(type: "int", nullable: false),
                    transaction_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRequestResource_RequestResourceId", x => x.borrow_request_resource_id);
                    table.ForeignKey(
                        name: "FK_BorrowRequestResource_RequestId",
                        column: x => x.borrow_request_id,
                        principalTable: "Borrow_Request",
                        principalColumn: "borrow_request_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowRequestResource_ResourceId",
                        column: x => x.resource_id,
                        principalTable: "Library_Resource",
                        principalColumn: "resource_id");
                    table.ForeignKey(
                        name: "FK_BorrowRequestResource_TransactionId",
                        column: x => x.transaction_id,
                        principalTable: "Transaction",
                        principalColumn: "transaction_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AI_Training_Detail_library_item_id",
                table: "AI_Training_Detail",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_AI_Training_Detail_training_session_id",
                table: "AI_Training_Detail",
                column: "training_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_AI_Training_Image_training_detail_id",
                table: "AI_Training_Image",
                column: "training_detail_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Detail_Extension_History_BorrowRecordDetailId",
                table: "Borrow_Detail_Extension_History",
                column: "BorrowRecordDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_borrow_request_id",
                table: "Borrow_Record",
                column: "borrow_request_id",
                unique: true,
                filter: "[borrow_request_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_library_card_id",
                table: "Borrow_Record",
                column: "library_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_proceesed_by",
                table: "Borrow_Record",
                column: "proceesed_by");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_Detail_borrow_record_id",
                table: "Borrow_Record_Detail",
                column: "borrow_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_Detail_condition_id",
                table: "Borrow_Record_Detail",
                column: "condition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_Detail_library_item_instance_id",
                table: "Borrow_Record_Detail",
                column: "library_item_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_Detail_processed_return_by",
                table: "Borrow_Record_Detail",
                column: "processed_return_by");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Record_Detail_return_condition_id",
                table: "Borrow_Record_Detail",
                column: "return_condition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_library_card_id",
                table: "Borrow_Request",
                column: "library_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_Detail_borrow_request_id",
                table: "Borrow_Request_Detail",
                column: "borrow_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_Detail_library_item_id",
                table: "Borrow_Request_Detail",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_Detail_LibraryItemInstanceId",
                table: "Borrow_Request_Detail",
                column: "LibraryItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_Resource_borrow_request_id",
                table: "Borrow_Request_Resource",
                column: "borrow_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_Resource_resource_id",
                table: "Borrow_Request_Resource",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_Borrow_Request_Resource_transaction_id",
                table: "Borrow_Request_Resource",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_Digital_Borrow_resource_id",
                table: "Digital_Borrow",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_Digital_Borrow_user_id",
                table: "Digital_Borrow",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Digital_Borrow_Extension_History_DigitalBorrowId",
                table: "Digital_Borrow_Extension_History",
                column: "DigitalBorrowId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_role_id",
                table: "Employee",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_Fine_borrow_record_detail_id",
                table: "Fine",
                column: "borrow_record_detail_id");

            migrationBuilder.CreateIndex(
                name: "IX_Fine_CreatedBy",
                table: "Fine",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Fine_fine_policy_id",
                table: "Fine",
                column: "fine_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_category_id",
                table: "Library_Item",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_group_id",
                table: "Library_Item",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_shelf_id",
                table: "Library_Item",
                column: "shelf_id");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_ISBN",
                table: "Library_Item",
                column: "isbn");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_Title",
                table: "Library_Item",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Author_author_id",
                table: "Library_Item_Author",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Author_library_item_id",
                table: "Library_Item_Author",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Condition_History_condition_id",
                table: "Library_Item_Condition_History",
                column: "condition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Condition_History_library_item_instance_id",
                table: "Library_Item_Condition_History",
                column: "library_item_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Instance_library_item_id",
                table: "Library_Item_Instance",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Resource_library_item_id",
                table: "Library_Item_Resource",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Resource_resource_id",
                table: "Library_Item_Resource",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Review_library_item_id",
                table: "Library_Item_Review",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Item_Review_user_id",
                table: "Library_Item_Review",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Library_Resource_Url_resource_id",
                table: "Library_Resource_Url",
                column: "resource_id");

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
                name: "IX_Notification_created_by",
                table: "Notification",
                column: "created_by");

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
                name: "IX_Reservation_Queue_borrow_request_id",
                table: "Reservation_Queue",
                column: "borrow_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Queue_library_card_id",
                table: "Reservation_Queue",
                column: "library_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Queue_library_item_id",
                table: "Reservation_Queue",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Queue_library_item_instance_id",
                table: "Reservation_Queue",
                column: "library_item_instance_id");

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
                name: "IX_Supplement_Request_Detail_related_library_item_id",
                table: "Supplement_Request_Detail",
                column: "related_library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Supplement_Request_Detail_tracking_id",
                table: "Supplement_Request_Detail",
                column: "tracking_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_fine_id",
                table: "Transaction",
                column: "fine_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_library_card_package_id",
                table: "Transaction",
                column: "library_card_package_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_payment_method_id",
                table: "Transaction",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_resource_id",
                table: "Transaction",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_user_id",
                table: "Transaction",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_library_card_id",
                table: "User",
                column: "library_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_role_id",
                table: "User",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Favorite_library_item_id",
                table: "User_Favorite",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Favorite_user_id",
                table: "User_Favorite",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_Tracking_supplier_id",
                table: "Warehouse_Tracking",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_Tracking_Detail_category_id",
                table: "Warehouse_Tracking_Detail",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_Tracking_Detail_condition_id",
                table: "Warehouse_Tracking_Detail",
                column: "condition_id");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_Tracking_Detail_library_item_id",
                table: "Warehouse_Tracking_Detail",
                column: "library_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_Tracking_Detail_tracking_id",
                table: "Warehouse_Tracking_Detail",
                column: "tracking_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AI_Training_Image");

            migrationBuilder.DropTable(
                name: "Audit_Trail");

            migrationBuilder.DropTable(
                name: "Borrow_Detail_Extension_History");

            migrationBuilder.DropTable(
                name: "Borrow_Request_Detail");

            migrationBuilder.DropTable(
                name: "Borrow_Request_Resource");

            migrationBuilder.DropTable(
                name: "Digital_Borrow_Extension_History");

            migrationBuilder.DropTable(
                name: "Library_Closure_Day");

            migrationBuilder.DropTable(
                name: "Library_Item_Author");

            migrationBuilder.DropTable(
                name: "Library_Item_Condition_History");

            migrationBuilder.DropTable(
                name: "Library_Item_Inventory");

            migrationBuilder.DropTable(
                name: "Library_Item_Resource");

            migrationBuilder.DropTable(
                name: "Library_Item_Review");

            migrationBuilder.DropTable(
                name: "Library_Resource_Url");

            migrationBuilder.DropTable(
                name: "Notification_Recipient");

            migrationBuilder.DropTable(
                name: "Refresh_Token");

            migrationBuilder.DropTable(
                name: "Reservation_Queue");

            migrationBuilder.DropTable(
                name: "Role_Permission");

            migrationBuilder.DropTable(
                name: "Supplement_Request_Detail");

            migrationBuilder.DropTable(
                name: "System_Message");

            migrationBuilder.DropTable(
                name: "User_Favorite");

            migrationBuilder.DropTable(
                name: "Warehouse_Tracking_Detail");

            migrationBuilder.DropTable(
                name: "Warehouse_Tracking_Inventory");

            migrationBuilder.DropTable(
                name: "AI_Training_Detail");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "Digital_Borrow");

            migrationBuilder.DropTable(
                name: "Author");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "System_Feature");

            migrationBuilder.DropTable(
                name: "System_Permission");

            migrationBuilder.DropTable(
                name: "Warehouse_Tracking");

            migrationBuilder.DropTable(
                name: "AI_Training_Session");

            migrationBuilder.DropTable(
                name: "Fine");

            migrationBuilder.DropTable(
                name: "Library_Card_Package");

            migrationBuilder.DropTable(
                name: "Payment_Method");

            migrationBuilder.DropTable(
                name: "Library_Resource");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropTable(
                name: "Borrow_Record_Detail");

            migrationBuilder.DropTable(
                name: "Fine_Policy");

            migrationBuilder.DropTable(
                name: "Borrow_Record");

            migrationBuilder.DropTable(
                name: "Library_Item_Condition");

            migrationBuilder.DropTable(
                name: "Library_Item_Instance");

            migrationBuilder.DropTable(
                name: "Borrow_Request");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "Library_Item");

            migrationBuilder.DropTable(
                name: "Library_Card");

            migrationBuilder.DropTable(
                name: "System_Role");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Library_Item_Group");

            migrationBuilder.DropTable(
                name: "Library_Shelf");

            migrationBuilder.DropTable(
                name: "Library_Section");

            migrationBuilder.DropTable(
                name: "Library_Zone");

            migrationBuilder.DropTable(
                name: "Library_Floor");
        }
    }
}
