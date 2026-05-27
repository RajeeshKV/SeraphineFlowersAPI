using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraphineFlowers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerFirebaseUid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirebaseUid",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_FirebaseUid",
                table: "customers",
                column: "FirebaseUid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customers_FirebaseUid",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "FirebaseUid",
                table: "customers");
        }
    }
}
