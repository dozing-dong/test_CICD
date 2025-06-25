using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmGear_Application.Migrations
{
    /// <inheritdoc />
    public partial class AddExplicitAverageRatingDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "AverageRating",
                table: "Equipment",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "AverageRating",
                table: "Equipment",
                type: "decimal(3,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,2)",
                oldDefaultValue: 0.0m);
        }
    }
}
