using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskStorm.Migrations
{
    /// <inheritdoc />
    public partial class ActivityGenerics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "comment_id",
                schema: "public",
                table: "activities",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "comment_id",
                schema: "public",
                table: "activities");
        }
    }
}
