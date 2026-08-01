using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoBuyer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreMonitoringState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_monitoring_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    LastHttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastSuccessAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextAllowedAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_monitoring_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_monitoring_states_Host",
                table: "store_monitoring_states",
                column: "Host",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_monitoring_states_NextAllowedAttemptAt",
                table: "store_monitoring_states",
                column: "NextAllowedAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_store_monitoring_states_Status",
                table: "store_monitoring_states",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_monitoring_states");
        }
    }
}
