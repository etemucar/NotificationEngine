using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NotificationEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification_engine");

            migrationBuilder.CreateTable(
                name: "language",
                schema: "notification_engine",
                columns: table => new
                {
                    language_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    language_cd = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_language", x => x.language_id);
                });

            migrationBuilder.CreateTable(
                name: "localizable_fields",
                schema: "notification_engine",
                columns: table => new
                {
                    localizable_fields_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_field = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localizable_fields", x => x.localizable_fields_id);
                });

            migrationBuilder.CreateTable(
                name: "localization",
                schema: "notification_engine",
                columns: table => new
                {
                    localization_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<int>(type: "integer", nullable: false),
                    entity_field = table.Column<string>(type: "text", nullable: false),
                    language_cd = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localization", x => x.localization_id);
                });

            migrationBuilder.CreateTable(
                name: "party",
                schema: "notification_engine",
                columns: table => new
                {
                    party_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party", x => x.party_id);
                });

            migrationBuilder.CreateTable(
                name: "related_party_type",
                schema: "notification_engine",
                columns: table => new
                {
                    related_party_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_related_party_type", x => x.related_party_type_id);
                });

            migrationBuilder.CreateTable(
                name: "individual",
                schema: "notification_engine",
                columns: table => new
                {
                    individual_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_id = table.Column<int>(type: "integer", nullable: false),
                    given_name = table.Column<string>(type: "text", nullable: false),
                    family_name = table.Column<string>(type: "text", nullable: false),
                    middle_name = table.Column<string>(type: "text", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: true),
                    nationality = table.Column<string>(type: "text", nullable: true),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    death_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    place_of_birth = table.Column<string>(type: "text", nullable: true),
                    country_of_birth = table.Column<string>(type: "text", nullable: true),
                    marital_status = table.Column<string>(type: "text", nullable: true),
                    valid_for_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_for_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual", x => x.individual_id);
                    table.ForeignKey(
                        name: "FK_individual_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "notification_engine",
                        principalTable: "party",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization",
                schema: "notification_engine",
                columns: table => new
                {
                    organization_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tax_office = table.Column<string>(type: "text", nullable: true),
                    tax_number = table.Column<long>(type: "bigint", nullable: false),
                    identity_number = table.Column<long>(type: "bigint", nullable: false),
                    trade_name = table.Column<string>(type: "text", nullable: true),
                    trade_register_number = table.Column<long>(type: "bigint", nullable: false),
                    mersis_no = table.Column<long>(type: "bigint", nullable: false),
                    valid_for_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_for_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization", x => x.organization_id);
                    table.ForeignKey(
                        name: "FK_organization_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "notification_engine",
                        principalTable: "party",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "related_party",
                schema: "notification_engine",
                columns: table => new
                {
                    related_party_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_id = table.Column<int>(type: "integer", nullable: false),
                    related_to_party_id = table.Column<int>(type: "integer", nullable: false),
                    related_party_type_id = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_related_party", x => x.related_party_id);
                    table.ForeignKey(
                        name: "FK_related_party_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "notification_engine",
                        principalTable: "party",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_related_party_party_related_to_party_id",
                        column: x => x.related_to_party_id,
                        principalSchema: "notification_engine",
                        principalTable: "party",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_related_party_related_party_type_related_party_type_id",
                        column: x => x.related_party_type_id,
                        principalSchema: "notification_engine",
                        principalTable: "related_party_type",
                        principalColumn: "related_party_type_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "message_template",
                schema: "notification_engine",
                columns: table => new
                {
                    message_template_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<int>(type: "integer", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_template", x => x.message_template_id);
                    table.ForeignKey(
                        name: "FK_message_template_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "notification_engine",
                        principalTable: "organization",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_language_rel",
                schema: "notification_engine",
                columns: table => new
                {
                    organization_language_rel_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<int>(type: "integer", nullable: false),
                    language_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_language_rel", x => x.organization_language_rel_id);
                    table.ForeignKey(
                        name: "FK_organization_language_rel_language_language_id",
                        column: x => x.language_id,
                        principalSchema: "notification_engine",
                        principalTable: "language",
                        principalColumn: "language_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_language_rel_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "notification_engine",
                        principalTable: "organization",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: "notification_engine",
                columns: table => new
                {
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<int>(type: "integer", nullable: false),
                    template_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    delivery_status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message", x => x.outbox_message_id);
                    table.ForeignKey(
                        name: "FK_outbox_message_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "notification_engine",
                        principalTable: "organization",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "party_role_type",
                schema: "notification_engine",
                columns: table => new
                {
                    party_role_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<int>(type: "integer", nullable: true),
                    party_role_type_cd = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_role_type", x => x.party_role_type_id);
                    table.ForeignKey(
                        name: "FK_party_role_type_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "notification_engine",
                        principalTable: "organization",
                        principalColumn: "organization_id");
                });

            migrationBuilder.CreateTable(
                name: "delivery_log",
                schema: "notification_engine",
                columns: table => new
                {
                    delivery_log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @event = table.Column<int>(name: "event", type: "integer", nullable: false),
                    detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_log", x => x.delivery_log_id);
                    table.ForeignKey(
                        name: "FK_delivery_log_outbox_message_message_id",
                        column: x => x.message_id,
                        principalSchema: "notification_engine",
                        principalTable: "outbox_message",
                        principalColumn: "outbox_message_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "party_role",
                schema: "notification_engine",
                columns: table => new
                {
                    party_role_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_id = table.Column<int>(type: "integer", nullable: false),
                    party_role_type_id = table.Column<int>(type: "integer", nullable: false),
                    valid_for_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_for_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    organization_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_role", x => x.party_role_id);
                    table.ForeignKey(
                        name: "FK_party_role_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "notification_engine",
                        principalTable: "organization",
                        principalColumn: "organization_id");
                    table.ForeignKey(
                        name: "FK_party_role_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "notification_engine",
                        principalTable: "party",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_party_role_party_role_type_party_role_type_id",
                        column: x => x.party_role_type_id,
                        principalSchema: "notification_engine",
                        principalTable: "party_role_type",
                        principalColumn: "party_role_type_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer",
                schema: "notification_engine",
                columns: table => new
                {
                    customer_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_role_id = table.Column<int>(type: "integer", nullable: false),
                    customer_number = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer", x => x.customer_id);
                    table.ForeignKey(
                        name: "FK_customer_party_role_party_role_id",
                        column: x => x.party_role_id,
                        principalSchema: "notification_engine",
                        principalTable: "party_role",
                        principalColumn: "party_role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_identity",
                schema: "notification_engine",
                columns: table => new
                {
                    digital_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nickname = table.Column<string>(type: "text", nullable: true),
                    digital_identity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    party_role_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_identity", x => x.digital_identity_id);
                    table.ForeignKey(
                        name: "FK_digital_identity_party_role_party_role_id",
                        column: x => x.party_role_id,
                        principalSchema: "notification_engine",
                        principalTable: "party_role",
                        principalColumn: "party_role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "party_role_account",
                schema: "notification_engine",
                columns: table => new
                {
                    party_role_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_role_id = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_role_account", x => x.party_role_account_id);
                    table.ForeignKey(
                        name: "FK_party_role_account_customer_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "notification_engine",
                        principalTable: "customer",
                        principalColumn: "customer_id");
                    table.ForeignKey(
                        name: "FK_party_role_account_party_role_party_role_id",
                        column: x => x.party_role_id,
                        principalSchema: "notification_engine",
                        principalTable: "party_role",
                        principalColumn: "party_role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "application_user",
                schema: "notification_engine",
                columns: table => new
                {
                    application_user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_user_id = table.Column<string>(type: "text", nullable: true),
                    language_id = table.Column<int>(type: "integer", nullable: false),
                    digital_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_user", x => x.application_user_id);
                    table.ForeignKey(
                        name: "FK_application_user_digital_identity_digital_identity_id",
                        column: x => x.digital_identity_id,
                        principalSchema: "notification_engine",
                        principalTable: "digital_identity",
                        principalColumn: "digital_identity_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_application_user_language_language_id",
                        column: x => x.language_id,
                        principalSchema: "notification_engine",
                        principalTable: "language",
                        principalColumn: "language_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credential",
                schema: "notification_engine",
                columns: table => new
                {
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type = table.Column<int>(type: "integer", nullable: false),
                    trust_level = table.Column<int>(type: "integer", nullable: true),
                    digital_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credential", x => x.credential_id);
                    table.ForeignKey(
                        name: "FK_credential_digital_identity_digital_identity_id",
                        column: x => x.digital_identity_id,
                        principalSchema: "notification_engine",
                        principalTable: "digital_identity",
                        principalColumn: "digital_identity_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                schema: "notification_engine",
                columns: table => new
                {
                    refresh_token_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_ip = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    application_user_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_token", x => x.refresh_token_id);
                    table.ForeignKey(
                        name: "FK_refresh_token_application_user_application_user_id",
                        column: x => x.application_user_id,
                        principalSchema: "notification_engine",
                        principalTable: "application_user",
                        principalColumn: "application_user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contact_medium",
                schema: "notification_engine",
                columns: table => new
                {
                    contact_medium_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_id = table.Column<int>(type: "integer", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    medium_type = table.Column<int>(type: "integer", nullable: false),
                    is_preferred = table.Column<bool>(type: "boolean", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    address_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_medium", x => x.contact_medium_id);
                    table.ForeignKey(
                        name: "FK_contact_medium_credential_credential_id",
                        column: x => x.credential_id,
                        principalSchema: "notification_engine",
                        principalTable: "credential",
                        principalColumn: "credential_id");
                    table.ForeignKey(
                        name: "FK_contact_medium_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "notification_engine",
                        principalTable: "party",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credential_characteristic",
                schema: "notification_engine",
                columns: table => new
                {
                    credential_characteristic_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credential_characteristic", x => x.credential_characteristic_id);
                    table.ForeignKey(
                        name: "FK_credential_characteristic_credential_credential_id",
                        column: x => x.credential_id,
                        principalSchema: "notification_engine",
                        principalTable: "credential",
                        principalColumn: "credential_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "notification_engine",
                table: "language",
                columns: new[] { "language_id", "create_date", "created_by", "is_deleted", "language_cd", "name", "status", "update_date", "updated_by" },
                values: new object[,]
                {
                    { 1, null, null, 0, "tr", "Türkçe", 0, null, null },
                    { 2, null, null, 0, "en", "English", 0, null, null },
                    { 3, null, null, 0, "ru", "Русский", 0, null, null }
                });

            migrationBuilder.InsertData(
                schema: "notification_engine",
                table: "localizable_fields",
                columns: new[] { "localizable_fields_id", "create_date", "created_by", "entity_field", "entity_type", "is_deleted", "status", "update_date", "updated_by" },
                values: new object[,]
                {
                    { 1, null, null, "Name", "Status", 0, 0, null, null },
                    { 2, null, null, "Description", "Status", 0, 0, null, null }
                });

            migrationBuilder.InsertData(
                schema: "notification_engine",
                table: "party_role_type",
                columns: new[] { "party_role_type_id", "create_date", "created_by", "is_deleted", "name", "organization_id", "party_role_type_cd", "status", "update_date", "updated_by" },
                values: new object[,]
                {
                    { 1, null, null, 0, "Site Yöneticisi", null, "SITE_ADMIN", 0, null, null },
                    { 2, null, null, 0, "Uygulama Kullanıcısı", null, "USER", 0, null, null },
                    { 3, null, null, 0, "Müşteri", null, "CUSTOMER", 0, null, null },
                    { 4, null, null, 0, "Fatura Hesabı", null, "BILL_ACCOUNT", 0, null, null },
                    { 5, null, null, 0, "Dış Servis", null, "EXTERNAL_SERVICE", 0, null, null }
                });

            migrationBuilder.InsertData(
                schema: "notification_engine",
                table: "related_party_type",
                columns: new[] { "related_party_type_id", "code", "create_date", "created_by", "is_deleted", "name", "status", "update_date", "updated_by" },
                values: new object[,]
                {
                    { 1, "ORGANIZATION_MEMBER", null, null, 0, "Organizasyon Üyesi", 0, null, null },
                    { 2, "DEPARTMENT_MEMBER", null, null, 0, "Departman Üyesi", 0, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_user_digital_identity_id",
                schema: "notification_engine",
                table: "application_user",
                column: "digital_identity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_application_user_language_id",
                schema: "notification_engine",
                table: "application_user",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "IX_contact_medium_credential_id",
                schema: "notification_engine",
                table: "contact_medium",
                column: "credential_id");

            migrationBuilder.CreateIndex(
                name: "IX_contact_medium_party_id",
                schema: "notification_engine",
                table: "contact_medium",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "IX_credential_digital_identity_id",
                schema: "notification_engine",
                table: "credential",
                column: "digital_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_credential_characteristic_credential_id",
                schema: "notification_engine",
                table: "credential_characteristic",
                column: "credential_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_party_role_id",
                schema: "notification_engine",
                table: "customer",
                column: "party_role_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_log_message_id",
                schema: "notification_engine",
                table: "delivery_log",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "IX_digital_identity_party_role_id",
                schema: "notification_engine",
                table: "digital_identity",
                column: "party_role_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_individual_party_id",
                schema: "notification_engine",
                table: "individual",
                column: "party_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_language_language_cd",
                schema: "notification_engine",
                table: "language",
                column: "language_cd",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_message_template_organization_id_code_channel_culture",
                schema: "notification_engine",
                table: "message_template",
                columns: new[] { "organization_id", "code", "channel", "culture" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_organization_party_id",
                schema: "notification_engine",
                table: "organization",
                column: "party_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_language_rel_language_id",
                schema: "notification_engine",
                table: "organization_language_rel",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_language_rel_organization_id_language_id",
                schema: "notification_engine",
                table: "organization_language_rel",
                columns: new[] { "organization_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_delivery_status_next_attempt_at",
                schema: "notification_engine",
                table: "outbox_message",
                columns: new[] { "delivery_status", "next_attempt_at" },
                filter: "delivery_status IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_organization_id_idempotency_key",
                schema: "notification_engine",
                table: "outbox_message",
                columns: new[] { "organization_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_party_role_organization_id",
                schema: "notification_engine",
                table: "party_role",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_role_party_id",
                schema: "notification_engine",
                table: "party_role",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_role_party_role_type_id",
                schema: "notification_engine",
                table: "party_role",
                column: "party_role_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_role_account_customer_id",
                schema: "notification_engine",
                table: "party_role_account",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_role_account_party_role_id",
                schema: "notification_engine",
                table: "party_role_account",
                column: "party_role_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_party_role_type_organization_id",
                schema: "notification_engine",
                table: "party_role_type",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_application_user_id",
                schema: "notification_engine",
                table: "refresh_token",
                column: "application_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_token",
                schema: "notification_engine",
                table: "refresh_token",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_related_party_related_party_type_id",
                schema: "notification_engine",
                table: "related_party",
                column: "related_party_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_related_party_related_to_party_id",
                schema: "notification_engine",
                table: "related_party",
                column: "related_to_party_id");

            migrationBuilder.CreateIndex(
                name: "ux_related_party_org_member",
                schema: "notification_engine",
                table: "related_party",
                column: "party_id",
                unique: true,
                filter: "related_party_type_id = 1 AND valid_to IS NULL AND is_deleted <> 1");

            migrationBuilder.CreateIndex(
                name: "IX_related_party_type_code",
                schema: "notification_engine",
                table: "related_party_type",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_medium",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "credential_characteristic",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "delivery_log",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "individual",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "localizable_fields",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "localization",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "message_template",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "organization_language_rel",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "party_role_account",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "refresh_token",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "related_party",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "credential",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "customer",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "application_user",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "related_party_type",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "digital_identity",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "language",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "party_role",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "party_role_type",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "organization",
                schema: "notification_engine");

            migrationBuilder.DropTable(
                name: "party",
                schema: "notification_engine");
        }
    }
}
