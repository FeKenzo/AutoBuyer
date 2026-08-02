using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoBuyer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramMessageId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AdvertisedPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ResolvedUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Coupon = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Conditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OriginalMessage = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductTargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_candidates_product_targets_ProductTargetId",
                        column: x => x.ProductTargetId,
                        principalTable: "product_targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_promotion_candidates_stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_candidates_ProductTargetId",
                table: "promotion_candidates",
                column: "ProductTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_candidates_ReceivedAt",
                table: "promotion_candidates",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_candidates_Status",
                table: "promotion_candidates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_candidates_StoreId",
                table: "promotion_candidates",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_candidates_TelegramChatId_TelegramMessageId",
                table: "promotion_candidates",
                columns: new[] { "TelegramChatId", "TelegramMessageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_candidates");
        }
    }
}
