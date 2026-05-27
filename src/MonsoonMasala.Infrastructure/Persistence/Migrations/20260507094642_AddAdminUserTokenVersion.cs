using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonsoonMasala.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUserTokenVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "admin_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "admin_users");
        }
    }
}
