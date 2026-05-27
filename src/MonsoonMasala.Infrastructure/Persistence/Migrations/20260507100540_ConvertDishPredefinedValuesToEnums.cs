using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonsoonMasala.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertDishPredefinedValuesToEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE dishes
                SET "FoodType" = CASE LOWER("FoodType")
                    WHEN 'nonveg' THEN 'NonVeg'
                    ELSE 'Veg'
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE dish_media
                SET "MediaType" = CASE LOWER("MediaType")
                    WHEN 'video' THEN 'Video'
                    ELSE 'Image'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "FoodType",
                table: "dishes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "veg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE dishes
                SET "FoodType" = CASE "FoodType"
                    WHEN 'NonVeg' THEN 'nonveg'
                    ELSE 'veg'
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE dish_media
                SET "MediaType" = CASE "MediaType"
                    WHEN 'Video' THEN 'video'
                    ELSE 'image'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "FoodType",
                table: "dishes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "veg",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
