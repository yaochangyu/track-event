using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrackEvent.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    anonymous_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    client_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    feature_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    feature_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    feature_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    page_url = table.Column<string>(type: "text", nullable: true),
                    page_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    previous_page_url = table.Column<string>(type: "text", nullable: true),
                    previous_page_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    screen_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    previous_screen_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    device_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    os = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    os_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    browser = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    browser_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    app_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    build_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    network_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    experiments = table.Column<string>(type: "jsonb", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_events_client_session_time",
                table: "user_events",
                columns: new[] { "client_id", "session_id", "event_time" });

            migrationBuilder.CreateIndex(
                name: "idx_user_events_event_time",
                table: "user_events",
                column: "event_time");

            migrationBuilder.CreateIndex(
                name: "idx_user_events_feature_time",
                table: "user_events",
                columns: new[] { "feature_id", "event_time" });

            migrationBuilder.CreateIndex(
                name: "idx_user_events_source_time",
                table: "user_events",
                columns: new[] { "source", "event_time" });

            migrationBuilder.CreateIndex(
                name: "idx_user_events_user_time",
                table: "user_events",
                columns: new[] { "user_id", "event_time" });

            migrationBuilder.CreateIndex(
                name: "IX_user_events_event_id",
                table: "user_events",
                column: "event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_events");
        }
    }
}
