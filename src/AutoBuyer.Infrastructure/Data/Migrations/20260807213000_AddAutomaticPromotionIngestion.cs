using AutoBuyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoBuyer.Infrastructure.Data.Migrations;

[DbContext(typeof(AutoBuyerDbContext))]
[Migration("20260807213000_AddAutomaticPromotionIngestion")]
public partial class AddAutomaticPromotionIngestion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "product_targets",
            type: "character varying(300)",
            maxLength: 300,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(250)",
            oldMaxLength: 250);

        migrationBuilder.AlterColumn<decimal>(
            name: "TargetPrice",
            table: "product_targets",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldPrecision: 18,
            oldScale: 2);

        migrationBuilder.AddColumn<string>(
            name: "ExternalProductId",
            table: "product_targets",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "LastObservedPrice",
            table: "product_targets",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastSeenAt",
            table: "product_targets",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "product_targets",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");

        migrationBuilder.Sql(
            "ALTER TABLE product_targets " +
            "ALTER COLUMN \"UpdatedAt\" DROP DEFAULT;");

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "promotion_candidates",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReviewReason",
            table: "promotion_candidates",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_product_targets_StoreId_ExternalProductId",
            table: "product_targets",
            columns: new[] { "StoreId", "ExternalProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_stores_BaseUrl",
            table: "stores",
            column: "BaseUrl");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE product_targets SET \"TargetPrice\" = 0.01 " +
            "WHERE \"TargetPrice\" IS NULL;");

        migrationBuilder.DropIndex(
            name: "IX_product_targets_StoreId_ExternalProductId",
            table: "product_targets");

        migrationBuilder.DropIndex(
            name: "IX_stores_BaseUrl",
            table: "stores");

        migrationBuilder.DropColumn(
            name: "ExternalProductId",
            table: "product_targets");

        migrationBuilder.DropColumn(
            name: "LastObservedPrice",
            table: "product_targets");

        migrationBuilder.DropColumn(
            name: "LastSeenAt",
            table: "product_targets");

        migrationBuilder.DropColumn(
            name: "UpdatedAt",
            table: "product_targets");

        migrationBuilder.DropColumn(
            name: "UpdatedAt",
            table: "promotion_candidates");

        migrationBuilder.DropColumn(
            name: "ReviewReason",
            table: "promotion_candidates");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "product_targets",
            type: "character varying(250)",
            maxLength: 250,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(300)",
            oldMaxLength: 300);

        migrationBuilder.AlterColumn<decimal>(
            name: "TargetPrice",
            table: "product_targets",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldPrecision: 18,
            oldScale: 2,
            oldNullable: true);
    }
}
