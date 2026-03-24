using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskStorm.Migrations
{
    /// <inheritdoc />
    public partial class Labels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "labels",
                schema: "public",
                table: "issues",
                type: "integer[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "labels",
                schema: "public",
                table: "issues");
        }
    }
}
