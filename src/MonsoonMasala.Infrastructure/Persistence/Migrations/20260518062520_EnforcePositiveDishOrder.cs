using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonsoonMasala.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePositiveDishOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH normalized AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "MenuId"
                            ORDER BY
                                CASE WHEN "Order" <= 0 THEN 2147483647 ELSE "Order" END,
                                "Order",
                                "CreatedAt",
                                "Name"
                        ) AS "NewOrder"
                    FROM dishes
                )
                UPDATE dishes
                SET "Order" = normalized."NewOrder",
                    "UpdatedAt" = NOW()
                FROM normalized
                WHERE dishes."Id" = normalized."Id"
                  AND dishes."Order" <> normalized."NewOrder";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_dishes_Order_Positive",
                table: "dishes",
                sql: "\"Order\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_dishes_Order_Positive",
                table: "dishes");
        }
    }
}
