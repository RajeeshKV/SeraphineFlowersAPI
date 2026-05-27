using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraphineFlowers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOtpAndRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to customers table
            migrationBuilder.AddColumn<bool>(
                name: "IsOtpVerified",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            // Create customer_refresh_tokens table
            migrationBuilder.CreateTable(
                name: "customer_refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_refresh_tokens_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indices
            migrationBuilder.CreateIndex(
                name: "IX_customer_refresh_tokens_CustomerId",
                table: "customer_refresh_tokens",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refresh_tokens_TokenHash",
                table: "customer_refresh_tokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop customer_refresh_tokens table
            migrationBuilder.DropTable(
                name: "customer_refresh_tokens");

            // Remove columns from customers table
            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "IsOtpVerified",
                table: "customers");
        }
    }
}
